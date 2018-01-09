using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockingController : MonoBehaviour {

	struct Agent{
		public Vector3 position;
		public Vector3 velocity;
		public Vector3 acceleration;
		public ushort treeIndex;
	}

	float LN2 = 1f / Mathf.Log (2f);

	[SerializeField]
	int nAgents = 15;
	[SerializeField]
	GameObject instanceObj;

	Agent[] agentTree;
	ComputeShader computer;
	ComputeBuffer agentBuffer;

	void buildTree ()
	{
	}
	// Use this for initialization
	void Start () {
		
	}

	void OnDestroy()
	{
		
	}

	// Update is called once per frame
	void Update () {
		
	}
}
