using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SkinningUtilities;
using InstancingUtilities;

public class FlockController8 : MonoBehaviour
{
    const int AGENT_NUM = 256;
    public Mesh model;
    public Bounds bounds;
    public Material modelMat;
    [Range(0f, 50f)]
    public float anchorDistance = 35;
    [Range(0f, 3f)]
    public float anchorMagnitude = 0.5f;
    public float animationSpeed = 0.1f;
    [SerializeField]
    float maxSpeed = 8, maxForce = 4;
    [SerializeField]
    float centroidThreshold = 12, headingThreshold = 4, spacingThreshold = 1.25f;
    [SerializeField]
    float centroidWeight = 1, headingWeight = 1, spacingWeight = 1;
    


    string path = @"C:\Users\User\Documents\nu_art\tin_drum\workshops\Flocking_Workshop\Assets\Scripts\kicking.bagel";
    QAnimation animation;
    float timeFrame;
    ComputeShader jointComputer, flockComputer;
    ComputeBuffer argBuffer;
    CommandBuffer commander;
    Matrix4x4[] inverseBindXforms;

    Matrix4x4[] debugs;

    Hierarchy hiero;

    ComputeBuffer jointDataBuf,
        tXBuf, tYBuf, tZBuf,
        rXBuf, rYBuf, rZBuf, rWBuf,
        sXBuf, sYBuf, sZBuf,
        timeBuf,
        hierarchyBuf, agentBuf,
        jointXBuf, instanceXBuf,
        boneIdxBuf, boneWeightBuf,
        debugBuf, flockDataBuf,
        cParAddBuf, hParAddBuf, sParAddBuf;

    void InitializeAgentBuffer()
    {
        int weightedAverageKID = flockComputer.FindKernel("CalculateWeightedAverages");
        int checkRangesKID = flockComputer.FindKernel("CheckRanges");
        debugBuf = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);
        Agent[] agents = new Agent[AGENT_NUM];
        int agentBlocks = AGENT_NUM / 2 + AGENT_NUM % 2;
        Matrix4x4[] agentMatrices = new Matrix4x4[agentBlocks];
        Matrix4x4[] transformMatrices = new Matrix4x4[AGENT_NUM];
        agentBuf = new ComputeBuffer(agentBlocks, sizeof(float) * 16);
        for (int i = 0; i < AGENT_NUM; i++)
        {
            agents[i] = new Agent(transform.position + Random.insideUnitSphere * 20f, Random.insideUnitSphere * maxSpeed / 10);
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
        agentBuf.SetData(agentMatrices);
        flockComputer.SetBuffer(weightedAverageKID, "AgentBuf", agentBuf);
        flockComputer.SetBuffer(checkRangesKID, "AgentBuf", agentBuf);
        flockComputer.SetBuffer(weightedAverageKID, "DebugBuf", debugBuf);
        flockComputer.SetBuffer(checkRangesKID, "DebugBuf", debugBuf);
    }

    void InitializeSkinningBuffers()
    {
        int numJoints = animation.jointNames.Count;

        AnimationData animData = hiero.MakeKeyFramesAndJointData(model);

        int kid = jointComputer.FindKernel("CalculateTransforms");

        hierarchyBuf = new ComputeBuffer(hiero.entries.Count, sizeof(int));
        hierarchyBuf.SetData(hiero.entries.ToArray());
        jointComputer.SetBuffer(kid, "hierarchyBuf", hierarchyBuf);

        jointDataBuf = new ComputeBuffer(numJoints, sizeof(int) * 21 + sizeof(float) * 32);
        jointDataBuf.SetData(animData.jointData);
        jointComputer.SetBuffer(kid, "jointBuf", jointDataBuf);

        tXBuf = new ComputeBuffer(animData.keyframes[0].Count, sizeof(float) * 2);
        tXBuf.SetData(animData.keyframes[0].ToArray());
        jointComputer.SetBuffer(kid, "tXBuf", tXBuf);

        tYBuf = new ComputeBuffer(animData.keyframes[1].Count, sizeof(float) * 2);
        tYBuf.SetData(animData.keyframes[1].ToArray());
        jointComputer.SetBuffer(kid, "tYBuf", tYBuf);

        tZBuf = new ComputeBuffer(animData.keyframes[2].Count, sizeof(float) * 2);
        tZBuf.SetData(animData.keyframes[2].ToArray());
        jointComputer.SetBuffer(kid, "tZBuf", tZBuf);

        rXBuf = new ComputeBuffer(animData.keyframes[3].Count, sizeof(float) * 2);
        rXBuf.SetData(animData.keyframes[3].ToArray());
        jointComputer.SetBuffer(kid, "rXBuf", rXBuf);

        rYBuf = new ComputeBuffer(animData.keyframes[4].Count, sizeof(float) * 2);
        rYBuf.SetData(animData.keyframes[4].ToArray());
        jointComputer.SetBuffer(kid, "rYBuf", rYBuf);

        rZBuf = new ComputeBuffer(animData.keyframes[5].Count, sizeof(float) * 2);
        rZBuf.SetData(animData.keyframes[5].ToArray());
        jointComputer.SetBuffer(kid, "rZBuf", rZBuf);

        rWBuf = new ComputeBuffer(animData.keyframes[6].Count, sizeof(float) * 2);
        rWBuf.SetData(animData.keyframes[6].ToArray());
        jointComputer.SetBuffer(kid, "rWBuf", rWBuf);

        sXBuf = new ComputeBuffer(animData.keyframes[7].Count, sizeof(float) * 2);
        sXBuf.SetData(animData.keyframes[7].ToArray());
        jointComputer.SetBuffer(kid, "sXBuf", sXBuf);

        sYBuf = new ComputeBuffer(animData.keyframes[8].Count, sizeof(float) * 2);
        sYBuf.SetData(animData.keyframes[8].ToArray());
        jointComputer.SetBuffer(kid, "sYBuf", sYBuf);

        sZBuf = new ComputeBuffer(animData.keyframes[9].Count, sizeof(float) * 2);
        sZBuf.SetData(animData.keyframes[9].ToArray());
        jointComputer.SetBuffer(kid, "sZBuf", sZBuf);

        jointXBuf = new ComputeBuffer(numJoints * AGENT_NUM, sizeof(float) * 16);
        jointComputer.SetBuffer(kid, "jointXforms", jointXBuf);

        float[] times = new float[AGENT_NUM];
        for(int i = 0; i < AGENT_NUM; i++)
        {
            times[i] = (Random.value * animation.length) % animation.length;
        }
        timeBuf = new ComputeBuffer(AGENT_NUM, sizeof(float));
        timeBuf.SetData(times);
        jointComputer.SetBuffer(kid, "times", timeBuf);

        jointComputer.SetFloat("animationLength", animation.length);

        jointComputer.SetInt("numJoints", numJoints);

        var wghts = model.boneWeights;
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
        modelMat.SetBuffer("weights", boneWeightBuf);
        modelMat.SetBuffer("boneIndices", boneIdxBuf);
        modelMat.SetBuffer("xforms", jointXBuf);
        modelMat.SetBuffer("instanceXforms", instanceXBuf);
        modelMat.SetInt("numJoints", numJoints);
    }

    void InitializeCommandBuffer()
    {
        int weightedAverageKID = flockComputer.FindKernel("CalculateWeightedAverages");
        int checkRangesKID = flockComputer.FindKernel("CheckRanges");
        int triParAddKID = flockComputer.FindKernel("TriParAdd");
        int skinKID = jointComputer.FindKernel("CalculateTransforms");
        int skinGroupCount = animation.jointNames.Count * AGENT_NUM / 13;
        commander = new CommandBuffer();
        commander.BeginSample("Flock Compute");
        commander.BeginSample("Check Ranges");
        commander.DispatchCompute(flockComputer, checkRangesKID, AGENT_NUM, 1, 1);
        commander.EndSample("Check Ranges");
        commander.BeginSample("TRI Par Add");
        commander.DispatchCompute(flockComputer, triParAddKID, AGENT_NUM, 1, 1);
        commander.EndSample("TRI Par Add");
        commander.BeginSample("Weighted Averages");
        commander.DispatchCompute(flockComputer, weightedAverageKID, 1, 1, 1);
        commander.EndSample("Weighted Averages");
        commander.EndSample("Flock Compute");
        commander.BeginSample("Skin Compute");
        commander.DispatchCompute(jointComputer, skinKID, skinGroupCount, 1, 1);
        commander.EndSample("Skin Compute");
    }

    void InitializeBuffers()
    {
        debugs = new Matrix4x4[AGENT_NUM];
        int weightedAverageKID = flockComputer.FindKernel("CalculateWeightedAverages");
        int checkRangesKID = flockComputer.FindKernel("CheckRanges");
        int triParAddKID = flockComputer.FindKernel("TriParAdd");

        argBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        uint[] args = new uint[] { model.GetIndexCount(0), (uint)AGENT_NUM, 0, 0, 0 };
        argBuffer.SetData(args);

        InitializeAgentBuffer();

        cParAddBuf = new ComputeBuffer(AGENT_NUM * AGENT_NUM, sizeof(float) * 4);
        hParAddBuf = new ComputeBuffer(AGENT_NUM * AGENT_NUM, sizeof(float) * 4);
        sParAddBuf = new ComputeBuffer(AGENT_NUM * AGENT_NUM, sizeof(float) * 4);
        flockDataBuf = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);
        instanceXBuf = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);


        flockComputer.SetBuffer(triParAddKID, "CentroidParAddBuf", cParAddBuf);
        flockComputer.SetBuffer(triParAddKID, "HeadingParAddBuf", hParAddBuf);
        flockComputer.SetBuffer(triParAddKID, "SpacingParAddBuf", sParAddBuf);
        flockComputer.SetBuffer(checkRangesKID, "CentroidParAddBuf", cParAddBuf);
        flockComputer.SetBuffer(checkRangesKID, "HeadingParAddBuf", hParAddBuf);
        flockComputer.SetBuffer(checkRangesKID, "SpacingParAddBuf", sParAddBuf);
        flockComputer.SetBuffer(triParAddKID, "FDataBuf", flockDataBuf);
        flockComputer.SetBuffer(weightedAverageKID, "FDataBuf", flockDataBuf);
        flockComputer.SetBuffer(weightedAverageKID, "XFormBuf", instanceXBuf);

        InitializeSkinningBuffers();

        InitializeCommandBuffer();
    }

    void UpdateFlockParams()
    {
        flockComputer.SetFloat("deltaTime", Time.deltaTime);
        flockComputer.SetFloat("maxSpeed", maxSpeed);
        flockComputer.SetFloat("maxForce", maxForce);
        flockComputer.SetFloat("centroidThreshold", centroidThreshold);
        flockComputer.SetFloat("centroidWeight", centroidWeight);
        flockComputer.SetFloat("headingThreshold", headingThreshold);
        flockComputer.SetFloat("headingWeight", headingWeight);
        flockComputer.SetFloat("spacingThreshold", spacingThreshold);
        flockComputer.SetFloat("spacingWeight", spacingWeight);
        flockComputer.SetVector("anchorPoint", transform.position);
        flockComputer.SetFloat("anchorDistance", anchorDistance);
        flockComputer.SetFloat("anchorAttractionWeight", anchorMagnitude);
    }

    void Start()
    {
        BagelLoader bagel = new BagelLoader(model);
        animation = bagel.LoadBagel(path);
        inverseBindXforms = model.bindposes;
        flockComputer = Resources.Load<ComputeShader>("FlockingComputer7");
        jointComputer = Resources.Load<ComputeShader>("SkinningComputer1");
        hiero = new Hierarchy(animation);
        InitializeBuffers();
    }

    void Update()
    {
        jointComputer.SetFloat("deltaTime", Time.deltaTime * animationSpeed);
        UpdateFlockParams();
        Graphics.ExecuteCommandBuffer(commander);
        debugBuf.GetData(debugs);
        Graphics.DrawMeshInstancedIndirect(model, 0, modelMat, bounds, argBuffer);
    }
}
