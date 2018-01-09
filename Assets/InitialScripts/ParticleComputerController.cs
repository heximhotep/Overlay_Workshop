using System.Collections.Generic;
using UnityEngine;

public class ParticleComputerController : MonoBehaviour {

	public ComputeShader computer;
	public LineRenderer lineTemplate;
	public float maxDist = 1;

	struct RangeCheck{
		public int p1_id;
		public int p2_id;
		public Vector3 pos1;
		public Vector3 pos2;
		public int inRange;
	};

	struct ParticlePair{
		public int id0;
		public int id1;

		public ParticlePair(int i0, int i1)
		{
			this.id0 = i0;
			this.id1 = i1;
		}
	};

	Dictionary<ParticlePair, LineRenderer> lrLookup;

	ComputeBuffer particleBuffer;

	ParticleSystem pSystem;
	ParticleSystem.Particle[] particles;
	RangeCheck[] checkArray;
	List<LineRenderer> lines;
	// Use this for initialization
	void Start () {
		lines = new List<LineRenderer> ();
		lrLookup = new Dictionary<ParticlePair, LineRenderer> ();
		pSystem = GetComponent<ParticleSystem> ();
	}

	void OnDestroy()
	{
		particleBuffer.Release ();
	}

	// Update is called once per frame
	void LateUpdate () {
		int pCount = pSystem.particleCount;
		if (pCount > 1) {
			particles = new ParticleSystem.Particle[pCount];
			pSystem.GetParticles (particles);
			checkArray = new RangeCheck[halfMatrixCount(pCount)];
			int idx = 0;
			for (int i = 0; i < pCount; i++) 
			{
				Vector3 thisPos = particles [i].position;
				for (int j = i + 1; j < pCount; j++) 
				{
					Vector3 thatPos = particles [j].position;

					RangeCheck thisRange = new RangeCheck ();
					thisRange.p1_id = i;
					thisRange.p2_id = j;
					thisRange.pos1 = thisPos;
					thisRange.pos2 = thatPos;
					thisRange.inRange = 0;

					checkArray[idx++] = thisRange;
				}
			}

			if (pCount % 10 > 0) {
				pCount += 10 - pCount % 10;
			}

			particleBuffer = new ComputeBuffer (checkArray.Length, sizeof(float) * 6 +
				sizeof(int) * 3,
				ComputeBufferType.Default);

			particleBuffer.SetData (checkArray);
			ComputeStepFrame ();
		
		}


	}

	private void ComputeStepFrame()
	{
		int kernelHandle = computer.FindKernel ("CSMain");
		computer.SetBuffer (kernelHandle, "RangeBuffer", particleBuffer);
		computer.SetFloat ("MaxDist", maxDist);

		computer.Dispatch (kernelHandle, particleBuffer.count / 10, 1, 1);
		particleBuffer.GetData (checkArray);

		/*for (int i = lines.Count - 1; i >= 0; i--) {
			GameObject thisLine = lines [i].gameObject;
			lines.RemoveAt(i);
			Destroy (thisLine);
		}*/
		foreach (RangeCheck check in checkArray) 
		{
			int loId = Mathf.Min (check.p1_id, check.p2_id);
			int hiId = Mathf.Max (check.p1_id, check.p2_id);

			ParticlePair thisPair = new ParticlePair (loId, hiId);

			LineRenderer thisLine;
			bool lineExists = lrLookup.TryGetValue(thisPair, out thisLine);

			if (check.inRange == 1) {
				if (!lineExists) {
					thisLine = Instantiate (lineTemplate, transform, false);
					lrLookup [thisPair] = thisLine;
				}
				thisLine.SetPosition (0, check.pos1);
				thisLine.SetPosition (1, check.pos2);
			} else {
				if (lineExists) {
					lrLookup.Remove (thisPair);
					Destroy (thisLine.gameObject);
				}
			}
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
