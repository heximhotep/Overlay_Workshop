using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockingController2 : MonoBehaviour {

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
        Vector3 centroidTotal, headingTotal, spacingTotal;
        float centroidWeightTotal, headingWeightTotal, spacingWeightTotal;
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
    Transform anchor;

    //[SerializeField]
    ComputeShader computer; 

    [SerializeField]
    float maxSpeed, maxForce, noiseFactor, noiseMagnitude;

    [SerializeField]
    float centroidDistance, headingDistance, spacingDistance;

    [SerializeField]
    float centroidWeight, headingWeight, spacingWeight;

    ComputeBuffer agentBuffer, rangeBuffer, dataBuffer, transformBuffer, float3Buffer, floatBuffer;
    Agent[] agents;
    Matrix4x4[] agentTransforms;
    Matrix4x4[] agentTransformBlock;
    Range[] ranges;
    int AGENT_NUM = 200;
    int halfMatNum;

    void InitializeBuffers()
    {
        agents = new Agent[AGENT_NUM];
        agentTransforms = new Matrix4x4[AGENT_NUM];
        agentTransformBlock = new Matrix4x4[Mathf.Min(1023, AGENT_NUM)];

        float offsetMagnitude = 125f;

        Vector3 startOffset0 = Random.onUnitSphere * offsetMagnitude;
        Vector3 startOffset1 = Random.onUnitSphere * offsetMagnitude;
        Vector3 startOffset2 = Random.onUnitSphere * offsetMagnitude; 

        for (int i = 0; i < AGENT_NUM; i++)
        {
            Vector3 startOffset = startOffset0;
            float diceRoll = Random.value;
            if(diceRoll >= 2f / 3)
            {
                startOffset = startOffset1;
            }
            else if(diceRoll >= 1f / 3)
            {
                startOffset = startOffset2;
            }
            agents[i] = new Agent(startOffset + Random.onUnitSphere * 45f, -Random.insideUnitSphere * maxSpeed / 10);
        }

        //initialize ranges
        ranges = new Range[halfMatNum];
        int idx = 0;
        for(int i = 0; i < AGENT_NUM; i++)
            for(int j = i + 1; j < AGENT_NUM; j++)
            {
                Range thisRange = new Range(i, j);
                ranges[idx++] = thisRange;
            }



        agentBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float) * 6);
        agentBuffer.SetData(agents);
        rangeBuffer = new ComputeBuffer(halfMatNum, sizeof(int) * 2);
        rangeBuffer.SetData(ranges);
        dataBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float) * 12);
        float3Buffer = new ComputeBuffer(halfMatNum, sizeof(float) * 3);
        floatBuffer = new ComputeBuffer(halfMatNum, sizeof(float));
        transformBuffer = new ComputeBuffer(AGENT_NUM, sizeof(float) * 16);
    }

    // Use this for initialization
    void Awake () {
        halfMatNum = AGENT_NUM * (AGENT_NUM - 1) / 2;
        computer = (ComputeShader)Resources.Load("FlockingComputer2");
        InitializeBuffers();
	}
	
	// Update is called once per frame
	void Update () {
        computer.SetVector("anchorPosition", anchor.position);
        computer.SetFloat("centroidDist", centroidDistance);
        computer.SetFloat("headingDist", headingDistance);
        computer.SetFloat("spacingDist", spacingDistance);
        computer.SetFloat("centroidWeight", centroidWeight);
        computer.SetFloat("headingWeight", headingWeight);
        computer.SetFloat("spacingWeight", spacingWeight);
        computer.SetFloat("_time", Time.time);
        computer.SetFloat("deltaTime", Time.deltaTime);
        computer.SetFloat("maxSpeed", maxSpeed);
        computer.SetFloat("maxForce", maxForce);
        computer.SetFloat("noiseFactor", noiseFactor);
        computer.SetFloat("noiseMagnitude", noiseMagnitude);
        int groupCount = halfMatNum / 100 + 1;
        int kid = computer.FindKernel("CSMain");
        computer.SetBuffer(kid, "ranges", rangeBuffer);
        computer.SetBuffer(kid, "flockData", dataBuffer);
        computer.SetBuffer(kid, "float3Vals", float3Buffer);
        computer.SetBuffer(kid, "floatVals", floatBuffer);
        computer.SetBuffer(kid, "agents", agentBuffer);
        computer.SetBuffer(kid, "agentTransforms", transformBuffer);
        computer.Dispatch(kid, groupCount, 1, 1);
        agentBuffer.GetData(agents);
        transformBuffer.GetData(agentTransforms);

        //split transforms into 1023 sized blocks

        int startIdx = 0;
        do
        {
            int blockoffset = Mathf.Min(AGENT_NUM - startIdx, 1023);
            int endIdx = startIdx + blockoffset - 1;
            System.Array.Copy(agentTransforms, startIdx, agentTransformBlock, 0, blockoffset);
            Graphics.DrawMeshInstanced(agentMesh, 0, agentMaterial, agentTransformBlock);
            startIdx = endIdx + 1;
        } while (startIdx < AGENT_NUM);
    }

    void OnDestroy()
    {
        agentBuffer.Release();
        rangeBuffer.Release();
        dataBuffer.Release();
        float3Buffer.Release();
        floatBuffer.Release();
        transformBuffer.Release();
    }

    /*void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if(agents != null)
            for(int i = 0; i < AGENT_NUM; i++)
            {
                Gizmos.DrawCube(agents[i].position, Vector3.one * 0.75f);
            }
    }*/
}
