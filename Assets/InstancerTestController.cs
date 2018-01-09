using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancerTestController : MonoBehaviour
{
    [SerializeField]
    Transform[] tforms;
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Material meshmat;
    Matrix4x4[] mforms;
    Matrix4x4[] data;
    [SerializeField]
    ComputeShader shadyboi;

    ComputeBuffer bufferboi;
    ComputeBuffer outputboi;

    // Use this for initialization
    void Start()
    {
        bufferboi = new ComputeBuffer(tforms.Length, sizeof(float) * 16);
        outputboi = new ComputeBuffer(tforms.Length, sizeof(float) * 16);
        data = new Matrix4x4[tforms.Length];
        mforms = new Matrix4x4[tforms.Length];
        foreach (Transform t in tforms)
        {
            GameObject cub = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cub.transform.SetParent(t, false);
            cub.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
    }

    void OnDestroy()
    {
        bufferboi.Release();
        outputboi.Release();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < data.Length; i++)
        {
            Transform form = tforms[i];
            Matrix4x4 datum = new Matrix4x4(
                    new Vector4(form.position.x, form.position.y, form.position.z),
                    new Vector4(form.forward.x, form.forward.y, form.forward.z),
                    new Vector4(),
                    new Vector4()
                );
            
            data[i] = datum;
        }
        bufferboi.SetData(data);
        bufferboi.GetData(data);
        shadyboi.SetInt("instanceCount", data.Length);
        int kernelIdx = shadyboi.FindKernel("MakeInstanceMatrices");
        shadyboi.SetBuffer(kernelIdx, "instanceBuf", bufferboi);
        shadyboi.SetBuffer(kernelIdx, "outputBuf", outputboi);
        int groupCount = data.Length / 16 + 1;
        shadyboi.Dispatch(kernelIdx, groupCount, 1, 1);
        outputboi.GetData(mforms);
        Graphics.DrawMeshInstanced(mesh, 0, meshmat, mforms);
    }
}
