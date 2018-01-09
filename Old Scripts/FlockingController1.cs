using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockingController1 : MonoBehaviour {

	struct Agent{
		public Vector3 position;
		public Vector3 velocity;
		public float value;
		public Agent(Vector3 _position)
		{
			position = _position;
			velocity = new Vector3();
			value = Random.value;
		}
		public Agent(Vector3 _position, Vector3 _velocity)
		{
			position = _position;
			velocity = _velocity;
			value = Random.value;
		}
	}

	struct Range{
		public int id0;
		public int id1;
		public float distance;
		public Range(int _id0, int _id1, float _dist)
		{
			id0 = _id0;
			id1 = _id1;
			distance = _dist;
		}
	}

	[System.Serializable]
	public class Settings
	{
		public int flockSize = 150;
		public float maxSpeed = 0.25f, maxAcceleration = 0.1f;
		public float changeRatio = 0.1f;
		[Range(0, 25f)]
		public int centroidFactor = 1,
		headingFactor = 1,
		anchorFactor = 1,
		spacingFactor = 1;
		public float spacingRadius = 1f;
		public float centroidRadius = 1.25f;
		public float headingDistance = 2f;
		public float jitter = 0.05f;
		public Transform anchor;
		public float spawnRadius = 10f;
	}

	public Settings settings;
	public ComputeShader computer;

	ComputeBuffer agentBuf, rangeBuf;


	void InitializeAgents()
	{
		Agent[] agents = new Agent[settings.flockSize];
		for (int i = 0; i < settings.flockSize; i++) {
			agents [i] = new Agent 
			(
				settings.anchor.position + Random.insideUnitSphere * settings.spawnRadius,
				Random.insideUnitSphere * settings.maxSpeed
			);
		}
		if (agentBuf != null)
			agentBuf.Release ();
		agentBuf = new ComputeBuffer (settings.flockSize, sizeof(float) * 7);
		agentBuf.SetData (agents);

		int halfMatCount = halfMatrixCount (settings.flockSize);

		rangeBuf = new ComputeBuffer (halfMatCount, sizeof(float) + sizeof(int) * 2);

		Range[] ranges = new Range[halfMatCount];
		int idx = 0;
		for (int i = 0; i < settings.flockSize; i++) {
			for (int j = i + 1; j < settings.flockSize; j++) {
				Range thisRange = new Range (i, j, -1f);
				ranges [idx++] = thisRange;
			}
		}

		rangeBuf.SetData (ranges);
		computer.SetInt ("halfMatCount", halfMatCount);
		int kernelIdx = computer.FindKernel ("Update");
		computer.SetBuffer (kernelIdx, "RangeBuf", rangeBuf);
		computer.SetBuffer (kernelIdx, "AgentBuf", agentBuf);
	}

	// Use this for initialization
	void Start () 
	{
		InitializeAgents ();
	}
	
	// Update is called once per frame
	void Update () {
		int kernelIdx = computer.FindKernel ("Update");
		computer.Dispatch (kernelIdx, settings.flockSize / 100, 1, 1);
	}

	int halfMatrixCount(int n)
	{
		int result = 0;
		for (int i = 1; i < n; i++)
			result += i;
		return result;
	}
}
