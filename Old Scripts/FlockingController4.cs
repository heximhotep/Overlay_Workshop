using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockingController4 : MonoBehaviour {

    const int AGENT_NUM = 400;
    const int HALFMAT_NUM = AGENT_NUM * (AGENT_NUM - 1) / 2;
    const int MAX_OPS = 8;

    [SerializeField]
    Mesh agentMesh;

    [SerializeField]
    Material agentMat;

    [SerializeField]
    float maxSpeed = 8, maxForce = 2;

    [SerializeField]
    float centroidThreshold = 3, headingThreshold = 2, spacingThreshold = 0.5f;
    [SerializeField]
    float centroidWeight = 1, headingWeight = 1, spacingWeight = 1;

    struct Agent
    {
        public Vector3 position, velocity;
        public Agent(Vector3 _position, Vector3 _velocity)
        {
            position = _position;
            velocity = _velocity;
        }
    }

    struct Range
    {
        public int id0, id1;
        public Range(int x, int y)
        {
            id0 = x;
            id1 = y;
        }
    }

    ComputeBuffer rangeBuf, agentBuf, agentTransformBuf, parAddBuf, flockDataBuf, testBuf;
    ComputeShader computer;

    Matrix4x4[] agentMatrices, 
                rangeMatrices, 
                agentTransformMatrices, 
                testData,
                agentTransformBlock;

    void InitializeBuffers()
    {
        Agent[] agents = new Agent[AGENT_NUM];
        Range[] ranges = new Range[HALFMAT_NUM];

        int agentBlocks = AGENT_NUM / 2 + AGENT_NUM % 2;
        int rangeBlocks = HALFMAT_NUM / 8 + (HALFMAT_NUM % 8 == 0 ? 0 : 1);

        agentMatrices = new Matrix4x4[agentBlocks];
        agentTransformMatrices = new Matrix4x4[AGENT_NUM];
        agentTransformBlock = new Matrix4x4[AGENT_NUM < 1023 ? AGENT_NUM : 1023];
        rangeMatrices = new Matrix4x4[rangeBlocks];
        testData = new Matrix4x4[AGENT_NUM];

        agentBuf = new ComputeBuffer(agentBlocks, sizeof(float) * 16);
        rangeBuf = new ComputeBuffer(rangeBlocks, sizeof(float) * 16);
        parAddBuf = new ComputeBuffer(AGENT_NUM * MAX_OPS, sizeof(float) * 4);
        flockDataBuf = new ComputeBuffer(AGENT_NUM, sizeof(float) * 12 + sizeof(int) * 3);
        agentTransformBuf = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);
        testBuf = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);

        int idx = 0;
        for(int i = 0; i < AGENT_NUM; i++)
        {
            agents[i] = new Agent(transform.position + Random.insideUnitSphere, Random.insideUnitSphere * maxSpeed / 5);
            for(int j = i + 1; j < AGENT_NUM; j++)
            {
                ranges[idx++] = new Range(i, j);
            }
        }
        for(int i = 0; i < agentBlocks; i++)
        {
            Agent agent0 = agents[2 * i];
            Vector4 col0 = new Vector4(agent0.position.x, agent0.position.y, agent0.position.z),
                    col1 = new Vector4(agent0.velocity.x, agent0.velocity.y, agent0.velocity.z),
                    col2 = new Vector4(),
                    col3 = new Vector4();
            if(2 * i + 1 < AGENT_NUM)
            {
                Agent agent1 = agents[2 * i + 1];
                col2 = new Vector4(agent1.position.x, agent1.position.y, agent1.position.z);
                col3 = new Vector4(agent1.velocity.x, agent1.velocity.y, agent1.velocity.z);
            }
            agentMatrices[i] = new Matrix4x4(col0, col1, col2, col3);
        }
        for(int i = 0; i < rangeBlocks; i++)
        {
            Vector4 col0 = new Vector4(),
                    col1 = new Vector4(),
                    col2 = new Vector4(),
                    col3 = new Vector4();
            col0.x = ranges[8 * i].id0;
            col0.y = ranges[8 * i].id1;
            if (8 * i + 1 < HALFMAT_NUM)
            {
                col0.z = ranges[8 * i + 1].id0;
                col0.w = ranges[8 * i + 1].id1;
            }
            if (8 * i + 2 < HALFMAT_NUM)
            {
                col1.x = ranges[8 * i + 2].id0;
                col1.y = ranges[8 * i + 2].id1;
            }
            if (8 * i + 3 < HALFMAT_NUM)
            {
                col1.z = ranges[8 * i + 3].id0;
                col1.w = ranges[8 * i + 3].id1;
            }
            if (8 * i + 4 < HALFMAT_NUM)
            {
                col2.x = ranges[8 * i + 4].id0;
                col2.y = ranges[8 * i + 4].id1;
            }
            if (8 * i + 5 < HALFMAT_NUM)
            {
                col2.z = ranges[8 * i + 5].id0;
                col2.w = ranges[8 * i + 5].id1;
            }
            if (8 * i + 6 < HALFMAT_NUM)
            {
                col3.x = ranges[8 * i + 6].id0;
                col3.y = ranges[8 * i + 6].id1;
            }
            if (8 * i + 7 < HALFMAT_NUM)
            {
                col3.z = ranges[8 * i + 7].id0;
                col3.w = ranges[8 * i + 7].id1;
            }
            rangeMatrices[i] = new Matrix4x4(col0, col1, col2, col3);
        }
        agentBuf.SetData(agentMatrices);
        rangeBuf.SetData(rangeMatrices);
    }

	// Use this for initialization
	void Awake () {
        computer = (ComputeShader)Resources.Load("FlockingComputer4");
        InitializeBuffers();
	}
	
	// Update is called once per frame
	void Update () {
        int kid = computer.FindKernel("TimeStep");
        computer.SetBuffer(kid, "AgentBuf", agentBuf);
        computer.SetBuffer(kid, "RangeBuf", rangeBuf);
        computer.SetBuffer(kid, "ParAddBuf", parAddBuf);
        computer.SetBuffer(kid, "FDataBuf", flockDataBuf);
        computer.SetBuffer(kid, "XFormBuf", agentTransformBuf);
        computer.SetBuffer(kid, "TestBuf", testBuf);

        computer.SetFloat("deltaTime", Time.deltaTime);
        computer.SetFloat("maxSpeed", maxSpeed);
        computer.SetFloat("maxForce", maxForce);
        computer.SetFloat("centroidThreshold", centroidThreshold);
        computer.SetFloat("centroidWeight", centroidWeight);
        computer.SetFloat("headingThreshold", headingThreshold);
        computer.SetFloat("headingWeight", headingWeight);
        computer.SetFloat("spacingThreshold", spacingThreshold);
        computer.SetFloat("spacingWeight", spacingWeight);

        int groupCount = HALFMAT_NUM / 512 + (HALFMAT_NUM % 512 == 0 ? 0 : 1);
        computer.Dispatch(kid, groupCount, 1, 1);
        agentTransformBuf.GetData(agentTransformMatrices);
        testBuf.GetData(testData);

        int startIdx = 0;
        do
        {
            int blockOffset = Mathf.Min(AGENT_NUM - startIdx, 1023);
            int endIdx = startIdx + blockOffset;
            System.Array.Copy(agentTransformMatrices, startIdx, agentTransformBlock, 0, blockOffset - 1);
            Graphics.DrawMeshInstanced(agentMesh, 0, agentMat, agentTransformBlock);
            startIdx = endIdx;
        } while (startIdx < AGENT_NUM);

       // Graphics.DrawMeshInstanced(agentMesh, 0, agentMat, agentTransformMatrices);
	}
}
