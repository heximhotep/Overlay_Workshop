using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkinningUtilities;

public class SkinnedInstancerController3 : MonoBehaviour {

    public Mesh model;
    string path = @"C:\Users\User\Documents\nu_art\tin_drum\workshops\Flocking_Workshop\Assets\Scripts\Fly_Loop.bagel";
    QAnimation animation;
    float timeFrame;
    ComputeShader jointComputer;
    Material modelMat;
    Matrix4x4[] xforms;
    Matrix4x4[] debugs;

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
    List<int> trueAdresses;

    ComputeBuffer jointDataBuf,
        tXBuf, tYBuf, tZBuf,
        rXBuf, rYBuf, rZBuf, rWBuf,
        sXBuf, sYBuf, sZBuf,
        hierarchyBuf,
        xformBuf,
        boneIdxBuf, boneWeightBuf,
        debugBuf;

    int GetHierarchySize(QJoint walker)
    {
        int result = 1;
        if (walker.children.Count > 0)
        {
            result += walker.children.Count;
            foreach(QJoint child in walker.children)
            {
                result += GetHierarchySize(child);
            }
        }
        return result;
    }

    void MakeHierarchy()
    {
        trueAdresses = new List<int>();
        for(int i = 0; i < numJoints; i++)
        {
            trueAdresses.Add(-1);
        }
        QJoint walker = animation.hierarchy;
        int hLen = GetHierarchySize(walker);
        hierarchy = new List<int>(hLen);
        Queue<QJoint> remaining = new Queue<QJoint>();
        remaining.Enqueue(walker);
        List<List<int>> pieces = new List<List<int>>(numJoints);
        for(int i = 0; i < numJoints; i++)
        {
            pieces.Add(new List<int>());
        }
        while(remaining.Count > 0)
        {
            QJoint joint = remaining.Dequeue();
            pieces[joint.index].Add(joint.children.Count);
            foreach(QJoint child in joint.children)
            {
                pieces[joint.index].Add(child.index);
                remaining.Enqueue(child);
            }
        }
        for(int i = 0; i < numJoints; i++)
        {
            List<int> piece = pieces[i];
            trueAdresses[i] = hierarchy.Count;
            hierarchy.AddRange(piece);
        }
    }

    int NumChildren(int jointIdx)
    {
        int result = hierarchy[0];
        int curIdx = 0;
        while(jointIdx > 0)
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
        while(remaining.Count > 0)
        {
            QJoint result = remaining.Dequeue();
            if (result.name == name)
                return result;
            foreach(var child in result.children)
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
        for(int i = 0; i < numJoints; i++)
        {
            indexedJoints.Add(null);
        }
        foreach(KeyValuePair<string, KeyBlob> kv in keyBlobs)
        {
            QJoint curJoint = FindJointByName(kv.Key);
            int curIdx = curJoint.index;
            indexedJoints[curIdx] = curJoint;
        }
        for(int i = 0; i < numJoints; i++)
        {
            QJoint joint = indexedJoints[i];
            KeyBlob blob = keyBlobs[joint.name];

            int[] jointDataArgs = new int[20];

            for(int j = 0; j < 10; j++)
            {
                string attribName = "m_LocalPosition.x";
                switch(j)
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
                for(int k = 0; k < frames.Count; k++)
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

        xforms = model.bindposes;
        xformBuf = new ComputeBuffer(numJoints, sizeof(float) * 16);
        xformBuf.SetData(xforms);

        int kid = jointComputer.FindKernel("CalculateTransforms");
        jointComputer.SetBuffer(kid, "hierarchyBuf", hierarchyBuf);
        jointComputer.SetBuffer(kid, "jointBuf", jointDataBuf);
        jointComputer.SetBuffer(kid, "tXBuf", tXBuf);
        jointComputer.SetBuffer(kid, "tYBuf", tYBuf);
        jointComputer.SetBuffer(kid, "tZBuf", tZBuf);
        jointComputer.SetBuffer(kid, "rXBuf", rXBuf);
        jointComputer.SetBuffer(kid, "rYBuf", rYBuf);
        jointComputer.SetBuffer(kid, "rZBuf", rZBuf);
        jointComputer.SetBuffer(kid, "rWBuf", rWBuf);
        jointComputer.SetBuffer(kid, "sXBuf", sXBuf);
        jointComputer.SetBuffer(kid, "sYBuf", sYBuf);
        jointComputer.SetBuffer(kid, "sZBuf", sZBuf);
        jointComputer.SetBuffer(kid, "xformBuf", xformBuf);
        jointComputer.SetInt("numJoints", numJoints);

        modelMat.SetBuffer("xforms", xformBuf);

        var wghts = model.boneWeights;
        Vector4[] boneIndices = new Vector4[wghts.Length];
        Vector4[] boneWeights = new Vector4[wghts.Length];
        for(int i = 0; i < wghts.Length; i++)
        {
            BoneWeight w = wghts[i];
            boneIndices[i] = new Vector4(w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3);
            boneWeights[i] = new Vector4(w.weight0, w.weight1, w.weight2, w.weight3);
        }
        boneIdxBuf = new ComputeBuffer(wghts.Length, sizeof(float) * 4);
        boneWeightBuf = new ComputeBuffer(wghts.Length, sizeof(float) * 4);
        boneIdxBuf.SetData(boneIndices);
        boneWeightBuf.SetData(boneWeights);
        modelMat.SetBuffer("weights", boneWeightBuf);
        modelMat.SetBuffer("boneIndices", boneIdxBuf);

        jointComputer.SetInt("numJoints", numJoints);

        debugs = new Matrix4x4[numJoints];
        for (int i = 0; i < debugs.Length; i++)
        {
            debugs[i] = new Matrix4x4();
        }
        debugBuf = new ComputeBuffer(debugs.Length, sizeof(float) * 16);
        debugBuf.SetData(debugs);
        jointComputer.SetBuffer(kid, "debugBuf", debugBuf);
    }

    // Use this for initialization
    void Start () {
        jointComputer = Resources.Load<ComputeShader>("SkinningComputer0");
        modelMat = GetComponent<Renderer>().material;

        BagelLoader bagel = new BagelLoader(model);
        animation = bagel.LoadBagel(path);
        bindTransforms = model.bindposes;
        numJoints = bindTransforms.Length;
        MakeHierarchy();
        MakeKeyFramesAndJointData();
        InitializeBuffers();
    }
	
	// Update is called once per frame
	void Update ()
    {
        timeFrame += Time.deltaTime;
        if (timeFrame >= animation.length)
            timeFrame %= animation.length;

        int kid = jointComputer.FindKernel("CalculateTransforms");
        jointComputer.SetFloat("_time", timeFrame);
        int groupCount = numJoints / 16 + 1;
        jointComputer.Dispatch(kid, groupCount, 1, 1);
        debugBuf.GetData(debugs);
        xformBuf.GetData(xforms);
	}

    /*void OnDrawGizmos()
    {
        foreach(Matrix4x4 xform in xforms)
        {
            Vector3 position = new Vector3(xform.m30, xform.m31, xform.m32);
            Gizmos.DrawCube(position, Vector3.one * 0.5f);
        }
    }*/
}
