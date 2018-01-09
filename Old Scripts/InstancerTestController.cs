using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancerTestController : MonoBehaviour {
    [SerializeField]
    Transform[] tforms;
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Material meshmat;
    Matrix4x4[] mforms;
    TformData[] data;
    [SerializeField]
    ComputeShader shadyboi;

    ComputeBuffer bufferboi;
    ComputeBuffer outputboi;

    struct TformData
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 up;
    }

	// Use this for initialization
	void Start ()
    {
        bufferboi = new ComputeBuffer(tforms.Length, sizeof(float) * 9);
        outputboi = new ComputeBuffer(tforms.Length, sizeof(float) * 16);
        data = new TformData[tforms.Length];
        mforms = new Matrix4x4[tforms.Length];
    }
	
	// Update is called once per frame
	void Update ()
    {
        for (int i = 0; i < data.Length; i++)
        {
            Transform form = tforms[i];
            TformData datum = new TformData();
            datum.position = form.position;
            datum.direction = form.forward;
            datum.up = form.up;
            data[i] = datum;
        }
        bufferboi.SetData(data);
        shadyboi.SetInt("instanceCount", data.Length);
        int kernelIdx = shadyboi.FindKernel("CSMain");
        shadyboi.SetBuffer(kernelIdx, "instanceBuf", bufferboi);
        shadyboi.SetBuffer(kernelIdx, "outputBuf", outputboi);
        int groupCount = data.Length / 10 + 1;
        shadyboi.Dispatch(kernelIdx, groupCount, 1, 1);
        outputboi.GetData(mforms);
        Graphics.DrawMeshInstanced(mesh, 0, meshmat, mforms);
	}
}
