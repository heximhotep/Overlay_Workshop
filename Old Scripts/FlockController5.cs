using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FlockController5 : MonoBehaviour {

    struct Agent
    {
        public Vector3 position, velocity;
        public Agent(Vector3 _pos, Vector3 _vel)
        {
            position = _pos;
            velocity = _vel;
        }
    }

    const int AGENT_NUM = 250;

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

    public SkinnedInstancerController4 carl;

    ComputeShader computer;
    CommandBuffer commander;

    ComputeBuffer argBuffer;
    ComputeBuffer agentBuffer, transformBuffer, flockDataBuffer, parAddBuffer, testBuffer;

    Matrix4x4[] agentMatrices, transformMatrices, transformBlock, testData;

    void InitializeBuffers()
    {
        carl.inXforms = new Matrix4x4[AGENT_NUM];

        Agent[] agents = new Agent[AGENT_NUM];

        int agentBlocks = AGENT_NUM / 2 + AGENT_NUM % 2;

        agentMatrices = new Matrix4x4[agentBlocks];
        transformMatrices = new Matrix4x4[AGENT_NUM];
        transformBlock = new Matrix4x4[AGENT_NUM < 1023 ? AGENT_NUM : 1023];
        testData = new Matrix4x4[AGENT_NUM];

        argBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);

        uint[] args = new uint[] { agentMesh.GetIndexCount(0), AGENT_NUM, 0, 0, 0};

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

        int kid = computer.FindKernel("TimeStep");
        computer.SetBuffer(kid, "AgentBuf", agentBuffer);
        computer.SetBuffer(kid, "ParAddBuf", parAddBuffer);
        computer.SetBuffer(kid, "FDataBuf", flockDataBuffer);
        computer.SetBuffer(kid, "XFormBuf", transformBuffer);
        computer.SetBuffer(kid, "TestBuf", testBuffer);

        agentMat.SetBuffer("xforms", transformBuffer);
        timeStepKID = computer.FindKernel("TimeStep");
        commander = new CommandBuffer();

        commander.BeginSample("TimeStep");
        commander.DispatchCompute(computer, timeStepKID, AGENT_NUM, 1, 1);
        commander.EndSample("TimeStep");
        //commander.BeginSample("DrawMeshesInstancedIndirect");
        //commander.DrawMeshInstancedIndirect(agentMesh, 0, agentMat, 0, argBuffer);
        //commander.EndSample("DrawMeshesInstancedIndirect");
        
    }

	void Awake ()
    {
        computer = (ComputeShader)Resources.Load("FlockingComputer5");
        InitializeBuffers();
	}
	
	void Update ()
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

        Graphics.ExecuteCommandBuffer(commander);

        transformBuffer.GetData(transformMatrices);
        carl.inXforms = transformMatrices;
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
