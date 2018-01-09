using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeTest : MonoBehaviour {

	public ComputeShader computer;

	ComputeBuffer valbuf;
	ComputeBuffer outbuf;

	float[] result;
	float[] vals;

	void LogArray<T>(T[] value)
	{
		string log = "[ ";
		for(int i = 0; i < value.Length; i++)
		{
			log += value[i] + (i == value.Length - 1 ? "]" : ", ");
		}
		Debug.Log (log);
	}

	// Use this for initialization
	void Start () {
		vals = new float[53];
		for (int i = 0; i < vals.Length; i++) {
			vals [i] = Random.value;
		}
		valbuf = new ComputeBuffer (vals.Length, sizeof(float));
		outbuf = new ComputeBuffer (1, sizeof(float));

		float trueSum = 0;
		foreach (float val in vals)
			trueSum += val;

//		float maybeSum = 0;
//		float[] temp = new float[4];
//		vals.CopyTo (temp, 0);
//		int k = 1;
//		int n = Mathf.CeilToInt (Mathf.Log (vals.Length, 2)) + 1;
//		while (k < n) 
//		{
//			for (int j = 0; j < temp.Length; j++) {
//				Debug.Log("___________________-");
//				Debug.Log ("k: " + k + ", j: " + j);
//				LogArray (temp);
//				Debug.Log (j + Mathf.Pow (2, k - 1));
//				if (j % (Mathf.Pow (2, k)) == 0 && j + Mathf.Pow(2, k - 1) < temp.Length) 
//				{
//					temp [j] += temp [j + (int)Mathf.Pow (2, k - 1)];
//				}
//			}
//			k++;
//		}
		result = new float[1];
		valbuf.SetData (vals);
		outbuf.SetData (result);
     	int kid = computer.FindKernel ("CSMain");
		computer.SetInt ("count", vals.Length);
		computer.SetBuffer (kid, "vals", valbuf);
		computer.SetBuffer (kid, "outbuf", outbuf);
		computer.Dispatch (kid, vals.Length / 10 + 1, 1, 1);
		valbuf.GetData (vals);
		outbuf.GetData (result);
		LogArray (vals);
		Debug.Log ("true sum: " + trueSum);
		Debug.Log ("computer sum: " + result[0]);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
