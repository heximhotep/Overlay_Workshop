//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//
//public class BasicFlockingController3 : MonoBehaviour {
//
//	struct MinAgent{
//		public Vector3 pos;
//		public Vector3 vel;
//		public Vector3 acc;
//		public MinAgent(Vector3 _pos, Vector3 _vel, Vector3 _acc)
//		{
//			pos = _pos;
//			vel = _vel;
//			acc = _acc;
//		}
//	}
//
//	struct Agent{
//		public Vector3 position;
//		public Vector3 velocity;
//		public Vector3 acceleration;
//		public Color color;
//		public Agent(Vector3 _position)
//		{
//			position = _position;
//			velocity = new Vector3();
//			acceleration = new Vector3();
//			color = new Color(Random.value, Random.value, Random.value);
//		}
//		public Agent(Vector3 _position, Vector3 _velocity)
//		{
//			position = _position;
//			velocity = _velocity;
//			acceleration = new Vector3();
//			color = new Color(Random.value, Random.value, Random.value);
//		}
//	}
//
//	[System.Serializable]
//	public class GeneralSettings
//	{
//		[Range(0, 1000)]
//		public int flockSize = 150;
//
//		public int spacingFactor = 1,
//		centroidFactor = 1, 
//		headingFactor = 1,
//		anchorFactor = 1;
//		public float maxSpeed = 0.25f, maxAcceleration = 0.1f;
//		[Range(0, 1)]
//		public float changeRatio = 0.1f;
//		public bool preAddSelfVelocity = false;
//		public bool preAddOtherVelocity = false;
//	}
//	[System.Serializable]
//	public class Settings
//	{
//		public float spacingRadius = 0.25f;
//		public float centroidDistance = 5f;
//		public float headingDistance = 4f;
//		public Transform anchor;
//	}
//
//	public GeneralSettings generalSettings;
//	public SpacingSettings spacingSettings;
//	public CentroidSettings centroidSettings;
//	public HeadingSettings headingSettings;
//	public AnchorSettings anchorSettings;
//
//	List<Agent> agents;
//
//	float maxSpeed, maxAcceleration;
//	float spacingRatio, centroidRatio, headingRatio, anchorRatio;
//
//	[SerializeField]
//	ComputeShader computer;
//
//	void SetRatios()
//	{
//		maxSpeed = generalSettings.maxSpeed;
//		maxAcceleration = generalSettings.maxAcceleration;
//		float factorSum = generalSettings.anchorFactor +
//			generalSettings.centroidFactor +
//			generalSettings.headingFactor +
//			generalSettings.spacingFactor;
//		spacingRatio = generalSettings.spacingFactor * 1f / factorSum;
//		centroidRatio = generalSettings.centroidFactor * 1f / factorSum;
//		headingRatio = generalSettings.headingFactor * 1f / factorSum;
//		anchorRatio = generalSettings.anchorFactor * 1f / factorSum;
//	}
//
//	// Use this for initialization
//	void Start () {
//		SetRatios ();
//		agents = new List<Agent> (generalSettings.flockSize);
//		for (int i = 0; i < generalSettings.flockSize; i++) {
//			Agent agent = new Agent (anchorSettings.anchor.position +
//				Random.insideUnitSphere * 5f, 
//				Random.insideUnitSphere * maxSpeed);
//			agents.Add (agent);
//		}
//	}
//
//	void OnDrawGizmos()
//	{
//		if (agents == null)
//			return;
//		foreach (Agent agent in agents) {
//			Gizmos.color = agent.color;
//			Gizmos.DrawCube (agent.position, Vector3.one * 0.25f);
//		}
//	}
//
//	Vector3 CalculateAcc(int curIdx)
//	{
//
//
//		Agent agent = agents [curIdx];
//		Vector3 spacing = new Vector3 ();
//		Vector3 centroid = new Vector3 ();
//		int centroidNeighbors = 0, headingNeighbors = 0;
//		Vector3 heading = new Vector3 ();
//		for (int i = 0; i < generalSettings.flockSize; i++) 
//		{
//			if (i == curIdx)
//				continue;
//			Agent other = agents [i];
//			//spacing
//			Vector3 offset = agent.position - other.position;
//			if (generalSettings.preAddSelfVelocity)
//				offset += agent.velocity;
//			if (generalSettings.preAddOtherVelocity)
//				offset += -other.velocity;
//			float offsetMag = offset.magnitude;
//			if (offsetMag < spacingSettings.spacingRadius &&
//				offsetMag > spacingSettings.minDistance) 
//			{
//				spacing += offset.normalized * (1f / offsetMag);
//			}
//			//--
//			//centroid
//			if (offsetMag < centroidSettings.centroidDistance) {
//				centroidNeighbors++;
//				centroid += other.position;
//			}
//			//--
//			//heading
//			if (offsetMag < headingSettings.headingDistance) {
//				headingNeighbors++;
//				heading += other.velocity;
//			}
//			//--
//		}
//
//		Vector3 anchor = (anchorSettings.anchor.position - agent.position) * 
//			agent.velocity.magnitude;
//
//		if (centroidNeighbors > 0) {
//			centroid *= 1f / centroidNeighbors;
//			centroid = centroid - agent.position;
//		}
//		if (headingNeighbors > 0) {
//			heading *= 1f / headingNeighbors;
//		}
//
//		Vector3 result = anchor * anchorRatio +
//			centroid * centroidRatio +
//			heading * headingRatio +
//			spacing * spacingRatio;
//		return result * generalSettings.changeRatio +
//			agent.acceleration * (1 - generalSettings.changeRatio);
//	}
//
//	// Update is called once per frame
//	void Update () {
//		SetRatios ();
//		MinAgent[] agentArray = new MinAgent[generalSettings.flockSize];
//		for (int i = 0; i < generalSettings.flockSize; i++) {
//			Agent agent = agents [i];
//			Vector3 nuAcc = CalculateAcc (i);
//			MinAgent minAgent = new MinAgent (agent.position, agent.velocity, nuAcc);
//			agentArray [i] = minAgent;
//		}
//		ComputeBuffer agentBuffer = new ComputeBuffer (agentArray.Length, sizeof(float) * 9, ComputeBufferType.Default);
//		agentBuffer.SetData (agentArray);
//		int kernelHandle = computer.FindKernel ("UpdateAgents");
//		computer.SetFloat ("maxVel", maxSpeed);
//		computer.SetFloat ("maxAcc", maxAcceleration);
//		computer.SetBuffer (kernelHandle, "AgentBuffer", agentBuffer);
//
//		computer.Dispatch (kernelHandle, agentBuffer.count / 10 + 1, 1, 1);
//
//		agentBuffer.GetData (agentArray);
//		for (int i = 0; i < generalSettings.flockSize; i++) 
//		{
//			MinAgent thisMin = agentArray [i];
//			Agent thisAgent = agents [i];
//			thisAgent.position = thisMin.pos;
//			thisAgent.velocity = thisMin.vel;
//			thisAgent.acceleration = thisMin.acc;
//			agents [i] = thisAgent;
//		}
//		agentBuffer.Release ();
//	}
//}