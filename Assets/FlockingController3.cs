using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FlockingController3 : MonoBehaviour {

    const int AGENT_NUM = 20;
    const int HALFMAT_NUM = AGENT_NUM * (AGENT_NUM - 1) / 2;
    const int MAX_OPS = 8;

    struct Agent
    {
        public Vector3 position;
        public Vector3 velocity;
        public Agent(Vector3 _position, Vector3 _velocity)
        {
            position = _position;
            velocity = _velocity;
        }
    }

    struct FlockDatum
    {
        Vector4 centroidTotal, headingTotal, spacingTotal;
        int centroidOpCount, headingOpCount, spacingOpCount;
    }

    struct Range
    {
        public int id0, id1;
        public Range(int i, int j)
        {
            id0 = i;
            id1 = j;
        }
    }

    [SerializeField]
    Mesh agentMesh;
    [SerializeField]
    Material agentMaterial;
    [SerializeField]
    Transform[] anchors;

    ComputeShader computer;

    [SerializeField]
    float maxSpeed, maxForce, noiseFactor, noiseMagnitude, initializationRange = 15f;
    [SerializeField]
    float centroidThreshold, headingThreshold, spacingThreshold;
    [SerializeField]
    float centroidWeight, headingWeight, spacingWeight;

    ComputeBuffer agentBuffer, anchorBuffer, rangeBuffer, flockDataBuffer, parAddBuffer, transformBuffer;

    Matrix4x4[] agents;
    Matrix4x4[] agentTransforms;
    Matrix4x4[] agentTransformBlock;
    Matrix4x4[] ranges;


    void InitializeBuffers()
    {
        //initialize agents
        agents = new Matrix4x4[AGENT_NUM / 2 + AGENT_NUM % 2];
        ranges = new Matrix4x4[HALFMAT_NUM / 8 + HALFMAT_NUM % 2];
        agentTransforms = new Matrix4x4[AGENT_NUM];
        agentTransformBlock = new Matrix4x4[Mathf.Min(1023, AGENT_NUM)];
        Agent[] agentAmt = new Agent[AGENT_NUM];
        for(int i = 0; i < AGENT_NUM; i++)
        {
            int matrixIndex = i / 2;
            int localIndex = i % 2;
            Vector3 position = transform.position + Random.insideUnitSphere * initializationRange;
            Vector3 velocity = Random.insideUnitSphere * maxSpeed / 10f;
            agentAmt[i] = new Agent(position, velocity);
        }
        for(int i = 0; i < agents.Length; i++)
        {
            Agent agent0 = agentAmt[2 * i];
            Vector4 col0 = new Vector4(agent0.position.x, agent0.position.y, agent0.position.z), 
                    col1 = new Vector4(agent0.velocity.x, agent0.velocity.y, agent0.velocity.z), 
                    col2 = new Vector4(), 
                    col3 = new Vector4();
            if(2 * i + 1 < AGENT_NUM)
            {
                Agent agent1 = agentAmt[2 * i + 1];
                col2 = new Vector4(agent1.position.x, agent1.position.y, agent1.position.z);
                col3 = new Vector4(agent1.velocity.x, agent1.velocity.y, agent1.velocity.z);
            }
            agents[i] = new Matrix4x4(col0, col1, col2, col3);
        }

        //initialize ranges
        Range[] rangeAmts = new Range[HALFMAT_NUM];
        int idx = 0;
        for(int i = 0; i < AGENT_NUM; i++)
        {
            for(int j = i + 1; j < AGENT_NUM; j++)
            {
                rangeAmts[idx++] = new Range(i, j);
            }
        }
        for(int i = 0; i < ranges.Length; i++)
        {
            Vector4 col0 = new Vector4(), col1 = new Vector4(), col2 = new Vector4(), col3 = new Vector4();
            col0.x = rangeAmts[8 * i].id0;
            col0.y = rangeAmts[8 * i].id1;
            if(8 * i + 1 < HALFMAT_NUM)
            {
                col0.z = rangeAmts[8 * i + 1].id0;
                col0.w = rangeAmts[8 * i + 1].id1;
            }
            if(8 * i + 2 < HALFMAT_NUM)
            {
                col1.x = rangeAmts[8 * i + 2].id0;
                col1.y = rangeAmts[8 * i + 2].id1;
            }
            if(8 * i + 3 < HALFMAT_NUM)
            {
                col1.z = rangeAmts[8 * i + 3].id0;
                col1.w = rangeAmts[8 * i + 3].id1;
            }
            if(8 * i + 4 < HALFMAT_NUM)
            {
                col2.x = rangeAmts[8 * i + 4].id0;
                col2.y = rangeAmts[8 * i + 4].id1;
            }
            if (8 * i + 5 < HALFMAT_NUM)
            {
                col2.z = rangeAmts[8 * i + 5].id0;
                col2.w = rangeAmts[8 * i + 5].id1;
            }
            if (8 * i + 6 < HALFMAT_NUM)
            {
                col3.x = rangeAmts[8 * i + 6].id0;
                col3.y = rangeAmts[8 * i + 6].id1;
            }
            if (8 * i + 7 < HALFMAT_NUM)
            {
                col3.z = rangeAmts[8 * i + 7].id0;
                col3.w = rangeAmts[8 * i + 7].id1;
            }
            Matrix4x4 rangeMat = new Matrix4x4(col0, col1, col2, col3);
            ranges[i] = rangeMat;
        }
        //initialize buffers
        agentBuffer = new ComputeBuffer(AGENT_NUM / 2 + 1, sizeof(float) * 16);
        agentBuffer.SetData(agents);
        anchorBuffer = new ComputeBuffer(anchors.Length, sizeof(float) * 4);
        rangeBuffer = new ComputeBuffer(HALFMAT_NUM / 8 + 1, sizeof(int) * 16);
        rangeBuffer.SetData(ranges);
        flockDataBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float) * 15);
        //initialize parAdd buffer to hold maxOps many values for each agent
        parAddBuffer = new ComputeBuffer(AGENT_NUM * MAX_OPS, sizeof(float) * 4);
        transformBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);
    }

	void Awake ()
    {
        computer = (ComputeShader)Resources.Load("FlockingComputer3");
        InitializeBuffers();
	}
	
	// Update is called once per frame
	void Update ()
    {
        anchorBuffer.SetData(new[] { new Vector4(transform.position.x, transform.position.y, transform.position.z, 1) });
        computer.SetFloat("centroidThreshold", centroidThreshold);
        computer.SetFloat("headingThreshold", headingThreshold);
        computer.SetFloat("spacingThreshold", spacingThreshold);
        computer.SetFloat("centroidWeight", centroidWeight);
        computer.SetFloat("headingWeight", headingWeight);
        computer.SetFloat("spacingWeight", spacingWeight);
        computer.SetFloat("_time", Time.time);
        computer.SetFloat("deltaTime", Time.deltaTime);
        computer.SetFloat("maxSpeed", maxSpeed);
        computer.SetFloat("maxForce", maxForce);
        computer.SetFloat("noiseFactor", noiseFactor);
        computer.SetFloat("noiseMagnitude", noiseMagnitude);
        computer.SetInt("anchorCount", anchors.Length);
        int groupCount = HALFMAT_NUM / 16 + 1;
        int kid = computer.FindKernel("TimeStep");
        computer.SetBuffer(kid, "RangeBuf", rangeBuffer);
        computer.SetBuffer(kid, "FlockDataBuf", flockDataBuffer);
        computer.SetBuffer(kid, "ParAddBuf", parAddBuffer);
        computer.SetBuffer(kid, "AgentBuf", agentBuffer);
        computer.SetBuffer(kid, "XFormBuf", transformBuffer);
        computer.SetBuffer(kid, "AnchorBuf", anchorBuffer);
        computer.Dispatch(kid, groupCount, 1, 1);
        agentBuffer.GetData(agents);
        transformBuffer.GetData(agentTransforms);

        //split transform into 1023 sized blocks
        int startIdx = 0;
        do
        {
            int blockOffset = Mathf.Min(AGENT_NUM - startIdx, 1023);
            int endIdx = startIdx + blockOffset;
            System.Array.Copy(agentTransforms, startIdx, agentTransformBlock, 0, blockOffset - 1);
            Graphics.DrawMeshInstanced(agentMesh, 0, agentMaterial, agentTransformBlock);
            startIdx = endIdx;
        } while (startIdx < AGENT_NUM);
	}

    private void OnDestroy()
    {
        agentBuffer.Release();
        rangeBuffer.Release();
        flockDataBuffer.Release();
        parAddBuffer.Release();
        transformBuffer.Release();
    }
}
