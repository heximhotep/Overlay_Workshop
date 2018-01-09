using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinningUtilities
{
    public class QModel
    {
        public Mesh mesh;
        public Material material;

        QJoint rootJoint;
        int jointCount;

        public QModel(Mesh _mesh, Material _material, QJoint _rootJoint, int _jointCount)
        {
            mesh = _mesh;
            material = _material;
            rootJoint = _rootJoint;
            jointCount = _jointCount;
            //rootJoint.CalculateInverseTransform(new Matrix4x4());
        }

        public Mesh GetMesh()
        {
            return mesh;
        }

        public Material GetMaterial()
        {
            return material;
        }

        public QJoint GetRootJoint()
        {
            return rootJoint;
        }

        public void DoAnimation()
        {

        }

        public Matrix4x4[] GetJointTransforms()
        {
            Matrix4x4[] result = new Matrix4x4[jointCount];
            AddJointsToArray(rootJoint, result);
            return result;
        }

        private void AddJointsToArray(QJoint headJoint, Matrix4x4[] jointArray)
        {
            //jointArray[headJoint.index] = headJoint.animatedTransform;
            foreach(QJoint joint in headJoint.children)
            {
                AddJointsToArray(joint, jointArray);
            }
        }
    }
}
