using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkinningUtilities;

public class SkinnedInstancerController2 : MonoBehaviour {

    public Mesh model;
    string path = @"C:\Users\User\Documents\nu_art\tin_drum\workshops\Flocking_Workshop\Assets\Scripts\Fly_Loop.bagel";
    QAnimation animation;
    float timeFrame;
    Matrix4x4[] boneXForms;
    GameObject[] bonePlaceholders;
    TransformData[] tDat;
    ComputeBuffer xFormBuf, boneIdxBuf, boneWeightBuf;
    Material mat;
    Matrix4x4[] debugs;
    
    void GenerateChildren(QJoint parent)
    {
        foreach(QJoint child in parent.children)
        {
            bonePlaceholders[child.index] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //bonePlaceholders[child.index].transform.SetParent(bonePlaceholders[parent.index].transform);
            bonePlaceholders[child.index].name = child.index.ToString();
            GenerateChildren(child);
        }
    }

    // Use this for initialization
    void Start ()
    {
        

        mat = GetComponent<Renderer>().material;
        BagelLoader bagelLoader = new BagelLoader(model);
        animation = bagelLoader.LoadBagel(path);
        tDat = new TransformData[animation.jointNames.Count];
        bonePlaceholders = new GameObject[tDat.Length];

        QJoint hierarchy = animation.hierarchy;
        bonePlaceholders[0] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GenerateChildren(hierarchy);

        boneXForms = new Matrix4x4[animation.jointNames.Count];
        debugs = new Matrix4x4[boneXForms.Length];

        xFormBuf = new ComputeBuffer(boneXForms.Length, sizeof(float) * 16);
        
        var wghts = model.boneWeights;
        boneIdxBuf = new ComputeBuffer(wghts.Length, sizeof(float) * 4);
        boneWeightBuf = new ComputeBuffer(wghts.Length, sizeof(float) * 4);

        Vector4[] boneIndices = new Vector4[wghts.Length];
        Vector4[] boneWeights = new Vector4[wghts.Length];
        for(int i = 0; i < wghts.Length; i++)
        {
            BoneWeight w = wghts[i];
            boneIndices[i] = new Vector4(w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3);
            boneWeights[i] = new Vector4(w.weight0, w.weight1, w.weight2, w.weight3);
        }
        boneIdxBuf.SetData(boneIndices);
        boneWeightBuf.SetData(boneWeights);
        mat.SetBuffer("boneIndices", boneIdxBuf);
        mat.SetBuffer("weights", boneWeightBuf);
	}
	
    void ApplyPoseTransform(QJoint child, QJoint parent)
    {
        boneXForms[child.index] = boneXForms[parent.index] * boneXForms[child.index];
        foreach(QJoint childChild in child.children)
        {
            ApplyPoseTransform(childChild, child);
        }
        boneXForms[child.index] =  boneXForms[child.index] * child.inverseBindTransform;
    }

	// Update is called once per frame
	void Update ()
    {
        timeFrame += Time.deltaTime;
        if (timeFrame >= animation.length)
            timeFrame %= animation.length;
        for(int i = 0; i < boneXForms.Length; i++)
        {
            TransformData tData = animation.GetTransformAt(i, timeFrame);
            /*
            debugs[i].SetRow(0, new Vector4(tData.position.x, tData.position.y, tData.position.z));
            debugs[i].SetRow(1, new Vector4(tData.rotation.x, tData.rotation.y, tData.rotation.z, tData.rotation.w));
            debugs[i].SetRow(2, new Vector4(tData.scale.x, tData.scale.y, tData.scale.z));
            */
            tDat[i] = tData;
            boneXForms[i] = Matrix4x4.TRS(tData.position, tData.rotation, tData.scale);
            debugs[i] = boneXForms[i];
        }
        QJoint joint = animation.hierarchy;
        foreach(QJoint child in joint.children)
        {
            ApplyPoseTransform(child, joint);
        }
        boneXForms[0] = joint.inverseBindTransform * boneXForms[0];
        /*
        for(int i = 0; i < boneXForms.Length; i++)
        {
            QJoint joint = animation.FindJointByIndex(i);
            
            foreach(QJoint child in joint.children)
            {
                boneXForms[child.index] =  boneXForms[child.index] * boneXForms[joint.index];
            }
            
            boneXForms[joint.index] *= joint.inverseBindTransform;
        }
        */
        xFormBuf.SetData(boneXForms);
        mat.SetBuffer("xforms", xFormBuf);
        for(int i = 0; i < boneXForms.Length; i++)
        {
            Quaternion rot = boneXForms[i].rotation;
            bonePlaceholders[i].transform.position = new Vector3(boneXForms[i].m03, boneXForms[i].m13, boneXForms[i].m23);
            bonePlaceholders[i].transform.rotation = rot;
            //bonePlaceholders[i].transform.localPosition = tDat[i].position;
            //bonePlaceholders[i].transform.localRotation = tDat[i].rotation;
        }
	}
    /*
    private void OnDrawGizmos()
    {
        foreach(Matrix4x4 mat in boneXForms)
        {
            Vector3 pos = new Vector3(mat.m03, mat.m13, mat.m23);
            Gizmos.DrawCube(pos, Vector3.one * 0.5f);
        }
    }*/
}
