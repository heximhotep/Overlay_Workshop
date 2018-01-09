using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SkinningUtilities;

public class SkinnedFlockingController0 : MonoBehaviour
{

    struct Agent
    {
        public Vector3 position, velocity;
        public Agent(Vector3 _pos, Vector3 _vel)
        {
            position = _pos;
            velocity = _vel;
        }
    }

    const int AGENT_NUM = 10;

    int timeStepKID;

    [SerializeField]
    Mesh agentMesh;

    [SerializeField]
    Material agentMat;

    [SerializeField]
    float maxSpeed = 8, maxForce = 2;

    [SerializeField]
    Bounds bounds;

    [SerializeField]
    float centroidThreshold = 12, headingThreshold = 4, spacingThreshold = 1.25f;
    [SerializeField]
    float centroidWeight = 1, headingWeight = 1, spacingWeight = 1;

    ComputeShader computer, skinner;

    CommandBuffer commander;

    ComputeBuffer argBuffer;
    //flocking controller buffers
    ComputeBuffer agentBuffer, transformBuffer, flockDataBuffer, 
        parAddBuffer, testBuffer, timeBuffer;
    //skinner and renderer buffers
    ComputeBuffer jointDataBuf,
        tXBuf, tYBuf, tZBuf,
        rXBuf, rYBuf, rZBuf, rWBuf,
        sXBuf, sYBuf, sZBuf,
        hierarchyBuf,
        xformBuf,
        boneIdxBuf, boneWeightBuf,
        debugBuf;

    string path = @"C:\Users\User\Documents\nu_art\tin_drum\workshops\Flocking_Workshop\Assets\Scripts\Fly_Loop.bagel";
    QAnimation animation;
    float timeFrame;
    Material modelMat;
    Matrix4x4[] xforms;
    Matrix4x4[] debugs;
    Matrix4x4[] agentMatrices, transformMatrices, transformBlock, testData;

    struct KeyFrame
    {
        public float time, value, inTangent, outTangent;
        public KeyFrame(float _time, float _value, float _inTangent, float _outTangent)
        {
            time = _time; value = _value; inTangent = _inTangent; outTangent = _outTangent;
        }
    }

    struct JointData
    {
        int tXStartIdx, tXLength;
        int tYStartIdx, tYLength;
        int tZStartIdx, tZLength;
        int rXStartIdx, rXLength;
        int rYStartIdx, rYLength;
        int rZStartIdx, rZLength;
        int rWStartIdx, rWLength;
        int sXStartIdx, sXLength;
        int sYStartIdx, sYLength;
        int sZStartIdx, sZLength;
        int numChildren;
        Matrix4x4 bindXForm, inverseBindXForm;
        public JointData(int[] trsArgs, int _numChildren, Matrix4x4 _inverseBindXForm)
        {
            numChildren = _numChildren;
            bindXForm = _inverseBindXForm.inverse;
            inverseBindXForm = _inverseBindXForm;

            tXStartIdx = trsArgs[0];
            tXLength = trsArgs[1];

            tYStartIdx = trsArgs[2];
            tYLength = trsArgs[3];

            tZStartIdx = trsArgs[4];
            tZLength = trsArgs[5];

            rXStartIdx = trsArgs[6];
            rXLength = trsArgs[7];

            rYStartIdx = trsArgs[8];
            rYLength = trsArgs[9];

            rZStartIdx = trsArgs[10];
            rZLength = trsArgs[11];

            rWStartIdx = trsArgs[12];
            rWLength = trsArgs[13];

            sXStartIdx = trsArgs[14];
            sXLength = trsArgs[15];

            sYStartIdx = trsArgs[16];
            sYLength = trsArgs[17];

            sZStartIdx = trsArgs[18];
            sZLength = trsArgs[19];
        }
    }

    JointData[] jointData;
    List<List<KeyFrame>> keyframes;
    List<int> hierarchy;
    Matrix4x4[] bindTransforms;
    int numJoints;

    int GetHierarchySize(QJoint walker)
    {
        int result = 1;
        if (walker.children.Count > 0)
        {
            result += walker.children.Count;
            foreach (QJoint child in walker.children)
            {
                result += GetHierarchySize(child);
            }
        }
        return result;
    }

    void MakeHierarchy()
    {
        QJoint walker = animation.hierarchy;
        int hLen = GetHierarchySize(walker);
        hierarchy = new List<int>(hLen);
        Queue<QJoint> remaining = new Queue<QJoint>();
        remaining.Enqueue(walker);
        List<List<int>> pieces = new List<List<int>>(numJoints);
        for (int i = 0; i < numJoints; i++)
        {
            pieces.Add(new List<int>());
        }
        while (remaining.Count > 0)
        {
            QJoint joint = remaining.Dequeue();
            pieces[joint.index].Add(joint.children.Count);
            foreach (QJoint child in joint.children)
            {
                pieces[joint.index].Add(child.index);
                remaining.Enqueue(child);
            }
        }
        for (int i = 0; i < numJoints; i++)
        {
            List<int> piece = pieces[i];
            hierarchy.AddRange(piece);
        }
    }

    int NumChildren(int jointIdx)
    {
        int result = hierarchy[0];
        int curIdx = 0;
        while (jointIdx > 0)
        {
            curIdx += 1 + result;
            result = hierarchy[curIdx];
            jointIdx--;
        }
        return result;
    }

    QJoint FindJointByName(string name)
    {
        Queue<QJoint> remaining = new Queue<QJoint>();
        remaining.Enqueue(animation.hierarchy);
        while (remaining.Count > 0)
        {
            QJoint result = remaining.Dequeue();
            if (result.name == name)
                return result;
            foreach (var child in result.children)
            {
                remaining.Enqueue(child);
            }
        }
        return null;
    }

    void MakeKeyFramesAndJointData()
    {
        var keyBlobs = animation.keyFrames;
        keyframes = new List<List<KeyFrame>>(10);
        for (int i = 0; i < 10; i++)
            keyframes.Add(new List<KeyFrame>());
        jointData = new JointData[numJoints];
        List<QJoint> indexedJoints = new List<QJoint>(numJoints);
        for (int i = 0; i < numJoints; i++)
        {
            indexedJoints.Add(null);
        }
        foreach (KeyValuePair<string, KeyBlob> kv in keyBlobs)
        {
            QJoint curJoint = FindJointByName(kv.Key);
            int curIdx = curJoint.index;
            indexedJoints[curIdx] = curJoint;
        }
        for (int i = 0; i < numJoints; i++)
        {
            QJoint joint = indexedJoints[i];
            KeyBlob blob = keyBlobs[joint.name];

            int[] jointDataArgs = new int[20];

            for (int j = 0; j < 10; j++)
            {
                string attribName = "m_LocalPosition.x";
                switch (j)
                {
                    case (1):
                        attribName = "m_LocalPosition.y";
                        break;
                    case (2):
                        attribName = "m_LocalPosition.z";
                        break;
                    case (3):
                        attribName = "m_LocalRotation.x";
                        break;
                    case (4):
                        attribName = "m_LocalRotation.y";
                        break;
                    case (5):
                        attribName = "m_LocalRotation.z";
                        break;
                    case (6):
                        attribName = "m_LocalRotation.w";
                        break;
                    case (7):
                        attribName = "m_LocalScale.x";
                        break;
                    case (8):
                        attribName = "m_LocalScale.y";
                        break;
                    case (9):
                        attribName = "m_LocalScale.z";
                        break;
                }
                SortedList<float, ScalarFrame> frames = blob.keyedAttributes[attribName].values;
                jointDataArgs[j * 2] = keyframes[j].Count;
                jointDataArgs[j * 2 + 1] = frames.Count;
                for (int k = 0; k < frames.Count; k++)
                {
                    float time = frames.Keys[k];
                    ScalarFrame frame = frames.Values[k];
                    KeyFrame newFrame = new KeyFrame(time, frame.value, frame.inTangent, frame.outTangent);
                    keyframes[j].Add(newFrame);
                }
            }
            int numChildren = NumChildren(i);
            Matrix4x4 bindPose = bindTransforms[i];//.inverse;
            jointData[i] = new JointData(jointDataArgs, numChildren, bindPose);
        }
    }

    void InitializeBuffers()
    {
        hierarchyBuf = new ComputeBuffer(hierarchy.Count, sizeof(int));
        hierarchyBuf.SetData(hierarchy.ToArray());
        jointDataBuf = new ComputeBuffer(numJoints, sizeof(int) * 21 + sizeof(float) * 32);
        jointDataBuf.SetData(jointData);
        tXBuf = new ComputeBuffer(keyframes[0].Count, sizeof(float) * 4);
        tXBuf.SetData(keyframes[0].ToArray());
        tYBuf = new ComputeBuffer(keyframes[1].Count, sizeof(float) * 4);
        tYBuf.SetData(keyframes[1].ToArray());
        tZBuf = new ComputeBuffer(keyframes[2].Count, sizeof(float) * 4);
        tZBuf.SetData(keyframes[2].ToArray());
        rXBuf = new ComputeBuffer(keyframes[3].Count, sizeof(float) * 4);
        rXBuf.SetData(keyframes[3].ToArray());
        rYBuf = new ComputeBuffer(keyframes[4].Count, sizeof(float) * 4);
        rYBuf.SetData(keyframes[4].ToArray());
        rZBuf = new ComputeBuffer(keyframes[5].Count, sizeof(float) * 4);
        rZBuf.SetData(keyframes[5].ToArray());
        rWBuf = new ComputeBuffer(keyframes[6].Count, sizeof(float) * 4);
        rWBuf.SetData(keyframes[6].ToArray());
        sXBuf = new ComputeBuffer(keyframes[7].Count, sizeof(float) * 4);
        sXBuf.SetData(keyframes[7].ToArray());
        sYBuf = new ComputeBuffer(keyframes[8].Count, sizeof(float) * 4);
        sYBuf.SetData(keyframes[8].ToArray());
        sZBuf = new ComputeBuffer(keyframes[9].Count, sizeof(float) * 4);
        sZBuf.SetData(keyframes[9].ToArray());

        xforms = agentMesh.bindposes;
        xformBuf = new ComputeBuffer(numJoints, sizeof(float) * 16);
        xformBuf.SetData(xforms);

        int kid = skinner.FindKernel("CalculateTransforms");
        skinner.SetBuffer(kid, "hierarchyBuf", hierarchyBuf);
        skinner.SetBuffer(kid, "jointBuf", jointDataBuf);
        skinner.SetBuffer(kid, "tXBuf", tXBuf);
        skinner.SetBuffer(kid, "tYBuf", tYBuf);
        skinner.SetBuffer(kid, "tZBuf", tZBuf);
        skinner.SetBuffer(kid, "rXBuf", rXBuf);
        skinner.SetBuffer(kid, "rYBuf", rYBuf);
        skinner.SetBuffer(kid, "rZBuf", rZBuf);
        skinner.SetBuffer(kid, "rWBuf", rWBuf);
        skinner.SetBuffer(kid, "sXBuf", sXBuf);
        skinner.SetBuffer(kid, "sYBuf", sYBuf);
        skinner.SetBuffer(kid, "sZBuf", sZBuf);
        skinner.SetBuffer(kid, "xformBuf", xformBuf);
        skinner.SetInt("numJoints", numJoints);

        agentMat.SetBuffer("skinXforms", xformBuf);

        var wghts = agentMesh.boneWeights;
        Vector4[] boneIndices = new Vector4[wghts.Length];
        Vector4[] boneWeights = new Vector4[wghts.Length];
        for (int i = 0; i < wghts.Length; i++)
        {
            BoneWeight w = wghts[i];
            boneIndices[i] = new Vector4(w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3);
            boneWeights[i] = new Vector4(w.weight0, w.weight1, w.weight2, w.weight3);
        }
        boneIdxBuf = new ComputeBuffer(wghts.Length, sizeof(float) * 4);
        boneWeightBuf = new ComputeBuffer(wghts.Length, sizeof(float) * 4);
        boneIdxBuf.SetData(boneIndices);
        boneWeightBuf.SetData(boneWeights);
        agentMat.SetBuffer("weights", boneWeightBuf);
        agentMat.SetBuffer("boneIndices", boneIdxBuf);

        skinner.SetInt("numJoints", numJoints);

        debugs = new Matrix4x4[numJoints];
        for (int i = 0; i < debugs.Length; i++)
        {
            debugs[i] = new Matrix4x4();
        }
        debugBuf = new ComputeBuffer(debugs.Length, sizeof(float) * 16);
        debugBuf.SetData(debugs);
        skinner.SetBuffer(kid, "debugBuf", debugBuf);

        Agent[] agents = new Agent[AGENT_NUM];

        int agentBlocks = AGENT_NUM / 2 + AGENT_NUM % 2;

        agentMatrices = new Matrix4x4[agentBlocks];
        transformMatrices = new Matrix4x4[AGENT_NUM];
        transformBlock = new Matrix4x4[AGENT_NUM < 1023 ? AGENT_NUM : 1023];
        testData = new Matrix4x4[AGENT_NUM];

        argBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);

        uint[] args = new uint[] { agentMesh.GetIndexCount(0), AGENT_NUM, 0, 0, 0 };

        argBuffer.SetData(args);

        agentBuffer = new ComputeBuffer(agentBlocks, sizeof(float) * 16);
        parAddBuffer = new ComputeBuffer(AGENT_NUM * AGENT_NUM, sizeof(float) * 4);
        flockDataBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float) * 12);
        transformBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);
        testBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);
        for (int i = 0; i < AGENT_NUM; i++)
        {
            agents[i] = new Agent(transform.position + Random.insideUnitSphere * 12f, Random.insideUnitSphere * maxSpeed / 10);
        }
        for (int i = 0; i < agentBlocks; i++)
        {
            Agent agent0 = agents[2 * i];
            Vector4 col0 = new Vector4(agent0.position.x, agent0.position.y, agent0.position.z),
                    col1 = new Vector4(agent0.velocity.x, agent0.velocity.y, agent0.velocity.z),
                    col2 = new Vector4(),
                    col3 = new Vector4();
            if (2 * i + 1 < AGENT_NUM)
            {
                Agent agent1 = agents[2 * i + 1];
                col2 = new Vector4(agent1.position.x, agent1.position.y, agent1.position.z);
                col3 = new Vector4(agent1.velocity.x, agent1.velocity.y, agent1.velocity.z);
            }
            agentMatrices[i] = new Matrix4x4(col0, col1, col2, col3);
        }
        agentBuffer.SetData(agentMatrices);

        int timeStepKid = computer.FindKernel("TimeStep");
        int skinXformsKid = skinner.FindKernel("CalculateTransforms");
        computer.SetBuffer(timeStepKid, "AgentBuf", agentBuffer);
        computer.SetBuffer(timeStepKid, "ParAddBuf", parAddBuffer);
        computer.SetBuffer(timeStepKid, "FDataBuf", flockDataBuffer);
        computer.SetBuffer(timeStepKid, "XFormBuf", transformBuffer);
        computer.SetBuffer(timeStepKid, "TestBuf", testBuffer);

        float[] startTimes = new float[AGENT_NUM];
        for(int i = 0; i < AGENT_NUM; i++)
        {
            startTimes[i] = Random.value * animation.length;
        }

        timeBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float));

        timeBuffer.SetData(startTimes);

        skinner.SetBuffer(skinXformsKid, "times", timeBuffer);


        agentMat.SetBuffer("modelXforms", transformBuffer);
        commander = new CommandBuffer();

        commander.BeginSample("TimeStep");
        commander.DispatchCompute(computer, timeStepKID, 1, 1, 1);
        commander.EndSample("TimeStep");
        commander.BeginSample("SkinStep");
        commander.DispatchCompute(skinner, skinXformsKid, 1, 1, 1);
        commander.EndSample("SkinStep");


    }

    void Awake()
    {
        computer = (ComputeShader)Resources.Load("FlockingComputer5");
        skinner = (ComputeShader)Resources.Load("SkinningInstanceComputer0");
        BagelLoader bagel = new BagelLoader(agentMesh);
        animation = bagel.LoadBagel(path);
        bindTransforms = agentMesh.bindposes;
        numJoints = bindTransforms.Length;
        MakeHierarchy();
        MakeKeyFramesAndJointData();
        InitializeBuffers();
    }

    void Update()
    {


        computer.SetFloat("deltaTime", Time.deltaTime);
        computer.SetFloat("maxSpeed", maxSpeed);
        computer.SetFloat("maxForce", maxForce);
        computer.SetFloat("centroidThreshold", centroidThreshold);
        computer.SetFloat("centroidWeight", centroidWeight);
        computer.SetFloat("headingThreshold", headingThreshold);
        computer.SetFloat("headingWeight", headingWeight);
        computer.SetFloat("spacingThreshold", spacingThreshold);
        computer.SetFloat("spacingWeight", spacingWeight);
        computer.SetVector("anchorPoint", transform.position);

        skinner.SetFloat("deltaTime", Time.deltaTime);

        timeFrame += Time.deltaTime;
        if (timeFrame >= animation.length)
            timeFrame %= animation.length;

        int groupCount = numJoints / 16 + 1;

        Graphics.ExecuteCommandBuffer(commander);

        //computer.Dispatch(kid, AGENT_NUM, 1, 1);
        //Graphics.DrawMeshInstancedIndirect(agentMesh, 0, agentMat, bounds, argBuffer);

        //int groupCount = AGENT_NUM;
        /*
        computer.Dispatch(kid, groupCount, 1, 1);

        transformBuffer.GetData(transformMatrices);
        testBuffer.GetData(testData);

        int startIdx = 0;
        do
        {
            int blockOffset = Mathf.Min(AGENT_NUM - startIdx, 1023);
            int endIdx = startIdx + blockOffset;
            System.Array.Copy(transformMatrices, startIdx, transformBlock, 0, blockOffset - 1);
            Graphics.DrawMeshInstanced(agentMesh, 0, agentMat, transformBlock);
            startIdx = endIdx;
        } while (startIdx < AGENT_NUM);
        */

    }
}