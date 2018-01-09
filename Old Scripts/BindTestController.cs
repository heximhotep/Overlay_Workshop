using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BindTestController : MonoBehaviour {

    public Mesh model;

	// Use this for initialization
	void Start ()
    {
        Matrix4x4[] xForms = model.bindposes;
        for(int i = 0; i < xForms.Length; i++)
        {
            Matrix4x4 xform = xForms[i].inverse;
            Vector3 position = xform.MultiplyPoint(new Vector3());
            Quaternion rotation = xform.rotation;
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = i.ToString();
            g.transform.position = position;
            g.transform.rotation = rotation;
        }
	}
}
