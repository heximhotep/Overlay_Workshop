using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicFlockingController : MonoBehaviour {
	[SerializeField]
	int spacingFactor, centroidFactor, headingFactor, anchorFactor;

	[SerializeField]
	GameObject anchor;

	[SerializeField]
	int flockSize;

	[SerializeField]
	[Range(0, 1)]
	float changeRatio = 0.1f;

	[SerializeField]
	float maxSpeed = 0.1f, maxAcceleration = 0.01f;

	float spacingRatio, centroidRatio, headingRatio, anchorRatio;

	struct Agent{
		public Vector3 position;
		public Vector3 velocity;
		public Vector3 acceleration;
		public Color color;
		public Agent(Vector3 _position)
		{
			position = _position;
			velocity = new Vector3();
			acceleration = new Vector3();
			color = new Color(Random.value, Random.value, Random.value);
		}
		public Agent(Vector3 _position, Vector3 _velocity)
		{
			position = _position;
			velocity = _velocity;
			acceleration = new Vector3();
			color = new Color(Random.value, Random.value, Random.value);
		}
	}

	List<Agent> agents;
	Vector3 fullCentroid;

	void setRatios()
	{
		float factorSum = anchorFactor + spacingFactor + headingFactor + centroidFactor;
		spacingRatio = spacingFactor * 1f / factorSum;
		headingRatio = headingFactor * 1f / factorSum;
		centroidRatio = centroidFactor * 1f / factorSum;
		anchorRatio = anchorFactor * 1f / factorSum;
	}

	// Use this for initialization
	void Start () {
		setRatios ();
		agents = new List<Agent> (flockSize);

		for (int i = 0; i < flockSize; i++) {
			Agent nuGuy = new Agent (anchor.transform.position + Random.insideUnitSphere * 8f, Random.insideUnitSphere * 0.1f);
			agents.Add (nuGuy);
		}
	}

	void OnDrawGizmos()
	{
		if (agents == null)
			return;
		foreach (Agent agent in agents) {
			Gizmos.color = agent.color;
			Gizmos.DrawCube (agent.position, Vector3.one * 0.25f);
		}
	}

	// Update is called once per frame
	void Update () 
	{
		setRatios ();
		for (int i = 0; i < flockSize; i++) {
			fullCentroid += agents [i].position;
		}
		fullCentroid *= 1f / flockSize;
		for (int i = 0; i < flockSize; i++) 
		{
			Agent agent = agents [i];
			Vector3 spacing = findSpacing (i) * spacingRatio;
			Vector3 centroid = findCentroid (i) * centroidRatio;
			Vector3 heading = findHeading (i) * headingRatio;
			Vector3 anchoring = anchorForce (i) * anchorRatio;
			Vector3 nuAcc = spacing + centroid + heading + anchoring;
			if (nuAcc.magnitude > maxAcceleration) 
			{
				nuAcc = nuAcc.normalized * maxAcceleration;
			}
			nuAcc = nuAcc * changeRatio + agent.acceleration * (1f - changeRatio);
			agent.acceleration = nuAcc;
			agents [i] = agent;
		}
		for (int i = 0; i < flockSize; i++) {
			Agent agent = agents [i];
			agent.velocity += agent.acceleration;
			if (agent.velocity.magnitude > maxSpeed) {
				agent.velocity = agent.velocity.normalized * maxSpeed;
			}
			agent.position += agent.velocity;
			agent.acceleration = Vector3.zero;
			//agent.velocity *= 0.99f;
			agents [i] = agent;
		}
	}

	Vector3 findSpacing(int curIdx)
	{
		Agent agent = agents [curIdx];
		float diam = 0.5f;
		Vector3 result = new Vector3 ();
		for (int i = 0; i < flockSize; i++) {
			if (i == curIdx)
				continue;
			Agent other = agents [i];
			Vector3 offset = agent.position + agent.velocity - other.position;
			if (offset.magnitude < diam * 2 && offset.magnitude > 0.01f) 
			{
				result += offset.normalized * (1f / offset.magnitude);
			}
		}
		return result;
	}

	Vector3 findCentroid(int curIdx)
	{
		Agent agent = agents [curIdx];
		Vector3 centre = new Vector3 ();
		int neighborCount = 0;
		for (int i = 0; i < flockSize; i++) {
			if (i == curIdx)
				continue;
			Agent other = agents [i];
			Vector3 offset = agent.position + agent.velocity - other.position;
			if (offset.magnitude < 2f) {
				neighborCount++;
				centre += other.position;
			}
		}
		if (neighborCount == 0)
			return new Vector3 ();
		centre *= 1f / neighborCount;
		Vector3 result = centre - agent.position - agent.velocity;
		return result;
	}

	Vector3 anchorForce(int curIdx)
	{
		Agent agent = agents [curIdx];
		Vector3 offset = anchor.transform.position - agent.position;
		return offset * agent.velocity.magnitude;
	}

	Vector3 findHeading(int curIdx)
	{
		Agent agent = agents [curIdx];
		Vector3 aggHead = new Vector3 ();
		int neighborCount = 0;
		for (int i = 0; i < flockSize; i++) {
			if (i == curIdx)
				continue;
			Agent other = agents [i];
			if (Vector3.Distance (other.position, agent.position) < 4f) {
				neighborCount++;
				aggHead += other.velocity;
			}
		}
		if (neighborCount > 0) {
			aggHead *= 1f / neighborCount;
			return aggHead;
		} else
			return new Vector3 ();
	}
}
