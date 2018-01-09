using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockingController0 : MonoBehaviour {

	struct Agent{
		public Vector3 position;
		public Vector3 velocity;
		public Vector3 acceleration;
		public Agent(Vector3 _position)
		{
			position = _position;
			velocity = new Vector3();
			acceleration = new Vector3();
		}
		public Agent(Vector3 _position, Vector3 _velocity)
		{
			position = _position;
			velocity = _velocity;
			acceleration = new Vector3();
		}
	}

	struct RangeCheck{
		public int id1;
		public int id2;
		public Vector3 pos1;
		public Vector3 pos2;
		public float dist;
		public RangeCheck(int _id1, int _id2, Vector3 _pos1, Vector3 _pos2)
		{
			id1 = _id1;
			id2 = _id2;
			pos1 = _pos1;
			pos2 = _pos2;
			dist = -1f;
		}
	}

	[System.Serializable]
	public class Settings
	{
		public int flockSize = 150;
		public int maxNeighbors = 10;
		public float maxSpeed = 0.25f, maxAcceleration = 0.1f;
		public float changeRatio = 0.1f;
		public int centroidFactor = 1,
			headingFactor = 1,
			anchorFactor = 1,
			spacingFactor = 1;
		public float spacingRadius = 1f;
		public float centroidRadius = 1.25f;
		public float headingDistance = 2f;
		public float jitter = 0.05f;
		public Transform anchor;
	}

	public Settings settings;
	public ComputeShader computer;

	RangeCheck[] checkArray;
	ComputeBuffer checkBuffer;
	Agent[] agentArray;
	ComputeBuffer agentBuffer;
	List<Agent> agents;
	List<Color> colors;
	float centroidRatio, headingRatio, anchorRatio, spacingRatio;
	Vector3[] spacings, headings, centroids;
	float[] headingNeighbors, centroidNeighbors;

	void SetRatios()
	{
		float factorSum = settings.anchorFactor +
			settings.centroidFactor +
			settings.headingFactor +
			settings.spacingFactor;
		spacingRatio = settings.spacingFactor * 1f / factorSum;
		centroidRatio = settings.centroidFactor * 1f / factorSum;
		headingRatio = settings.headingFactor * 1f / factorSum;
		anchorRatio = settings.anchorFactor * 1f / factorSum;
	}

	void SpawnAgents()
	{
		agents = new List<Agent> (settings.flockSize);
		colors = new List<Color> (settings.flockSize);

		spacings = new Vector3[settings.flockSize];
		centroids = new Vector3[settings.flockSize];
		headings = new Vector3[settings.flockSize];
		centroidNeighbors = new float[settings.flockSize];
		headingNeighbors = new float[settings.flockSize];

		for (int i = 0; i < settings.flockSize; i++) {
			Agent agent = new Agent (settings.anchor.position + Random.insideUnitSphere * 5f,
				              Random.insideUnitSphere * settings.maxSpeed);
			float v = Random.value;
			Color color = new Color (v, v / 4, 0.1f + Random.value * 0.1f);
			colors.Add (color);
			agents.Add (agent);
		
			spacings [i] = new Vector3 ();
			centroids [i] = new Vector3 ();
			headings [i] = new Vector3 ();
		}

		//agentBuffer = new ComputeBuffer (settings.flockSize, 
		//	sizeof(float) * 9, 
		//	ComputeBufferType.Default);
	}

	// Use this for initialization
	void Awake () {
		
		SetRatios ();
		SpawnAgents ();
		checkArray = new RangeCheck[halfMatrixCount (settings.flockSize)];

	}

	void OnDestroy() {
		agentBuffer.Release ();
		checkBuffer.Release ();
	}

	void OnDrawGizmos()
	{
		if (agents == null)
			return;
		for (int i = 0; i < settings.flockSize; i++) {
			Gizmos.color = colors [i];
			Gizmos.DrawCube (agents [i].position, Vector3.one * 0.25f);
		}
	}

	void BuildRangeChecks()
	{
		int idx = 0;
		for (int i = 0; i < settings.flockSize; i++) 
		{
			Vector3 thisPos = agents [i].position;
			for (int j = i + 1; j < settings.flockSize; j++) 
			{
				Vector3 thatPos = agents [j].position;
				RangeCheck thisRange = new RangeCheck (i, j, thisPos, thatPos);
				checkArray [idx++] = thisRange;
			}
		}
	}

	void ResetArrays()
	{
		spacings = new Vector3[settings.flockSize];
		centroids = new Vector3[settings.flockSize];
		headings = new Vector3[settings.flockSize];
		centroidNeighbors = new float[settings.flockSize];
		headingNeighbors = new float[settings.flockSize];
//		for (int i = 0; i < settings.flockSize; i++) 
//		{
//			spacings [i] = new Vector3 ();
//			centroids [i] = new Vector3 ();
//			headings [i] = new Vector3 ();
//		}
	}

	// Update is called once per frame
	void Update () {
		SetRatios ();
		ResetArrays ();
		BuildRangeChecks ();
		checkBuffer = new ComputeBuffer (checkArray.Length, 
			sizeof(int) * 2 + sizeof(float) * 7, 
			ComputeBufferType.Default);
		checkBuffer.SetData (checkArray);
		int kernelHandle = computer.FindKernel ("CheckRanges");
		computer.SetBuffer (kernelHandle, "CheckBuffer", checkBuffer);
		computer.Dispatch (kernelHandle, checkBuffer.count / 50 + 1, 1, 1);
		checkBuffer.GetData (checkArray);
		checkBuffer.Release ();
		foreach (RangeCheck check in checkArray) 
		{
			int id1 = check.id1;
			int id2 = check.id2;
			Agent a1 = agents [id1];
			Agent a2 = agents [id2];
			float dist = check.dist;
			Vector3 offset = a1.position - a2.position;
			//check spacing
			if (dist < settings.spacingRadius && dist > 0.01f) {
				offset = offset.normalized * 1f / offset.magnitude;
				spacings [id1] += offset;
				spacings [id2] += -offset;
			}

			if (dist < settings.centroidRadius) {
				float scaling = dist / settings.centroidRadius;
				centroids [id1] += a2.position * scaling;
				centroids [id2] += a1.position * scaling;
				centroidNeighbors [id1]+= scaling;
				centroidNeighbors [id2]+= scaling;
			}

			if (dist < settings.headingDistance) {
				float scaling = Mathf.Max(0.05f, dist / settings.headingDistance);
				headings [id1] += a2.velocity * scaling;
				headings [id2] += a1.velocity * scaling;
				headingNeighbors [id1]+= scaling;
				headingNeighbors [id2]+= scaling;
			}
		}

		for (int i = 0; i < settings.flockSize; i++) {
			Agent agent = agents [i];
			Vector3 centroid = centroids [i];
			Vector3 heading = headings [i];
			Vector3 spacing = spacings [i];
			float centroidNeighbor = centroidNeighbors [i];
			float headingNeighbor = headingNeighbors [i];
			Vector3 anchor = (settings.anchor.position - agent.position);
			if (centroidNeighbor > 0) {
				centroid *= 1f / centroidNeighbor;
				centroid = centroid - agent.position;
			}
			if (headingNeighbor > 0) {
				heading *= 1f / headingNeighbor;
			}
			Vector3 nuAcc = anchor.normalized * anchorRatio +
				centroid.normalized * centroidRatio +
				heading.normalized * headingRatio +
				spacing.normalized * spacingRatio;
			nuAcc = nuAcc * settings.changeRatio + agent.acceleration * (1 - settings.changeRatio);
			agent.acceleration = nuAcc + Random.insideUnitSphere * settings.jitter;
			agents [i] = agent;
		}
		agentArray = agents.ToArray ();
		agentBuffer = new ComputeBuffer (agentArray.Length, sizeof(float) * 9, ComputeBufferType.Default);
		agentBuffer.SetData (agentArray);
		kernelHandle = computer.FindKernel ("UpdateAgents");
		computer.SetFloat ("maxSpeed", settings.maxSpeed);
		computer.SetFloat ("maxAcceleration", settings.maxAcceleration);
		//computer.SetFloat ("changeRatio", settings.changeRatio);
		computer.SetBuffer (kernelHandle, "AgentBuffer", agentBuffer);

		computer.Dispatch (kernelHandle, agentBuffer.count / 50 + 1, 1, 1);

		agentBuffer.GetData (agentArray);
		agentBuffer.Release ();
		for (int i = 0; i < settings.flockSize; i++) {
			agents [i] = agentArray [i];
		}
	}

	private int halfMatrixCount(int n)
	{
		if (n <= 1)
			return 0;
		else

			return n - 1 + halfMatrixCount (n - 1);
	}
}
