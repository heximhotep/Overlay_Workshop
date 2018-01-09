using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinningUtilities
{
    public class QJointTransform
    {
        public readonly Vector3 position, posInTangents, posOutTangents;
        public readonly Vector4 rotInTangents, rotOutTangents;
        public readonly Quaternion rotation;

        public QJointTransform(Vector3 _position, Quaternion _rotation)
        {
            position = _position;
            posInTangents = Vector3.zero;
            posOutTangents = Vector3.zero;
            rotation = _rotation;
            rotInTangents = Vector4.zero;
            rotOutTangents = Vector4.zero;
        }

        public QJointTransform(Vector3 _position, Quaternion _rotation, 
            Vector3 _pInTangents, Vector4 _rInTangents,
            Vector3 _pOutTangents, Vector4 _rOutTangents)
        {
            position = _position;
            posInTangents = _pInTangents;
            posOutTangents = _pOutTangents;
            rotation = _rotation;
            rotInTangents = _rInTangents;
            rotOutTangents = _rOutTangents;
        }

        public Matrix4x4 GetLocalTransform()
        {
            return Matrix4x4.Translate(position) * Matrix4x4.Rotate(rotation);
        }

        public static QJointTransform Interpolate(QJointTransform frameA, QJointTransform frameB, float progression)
        {
            Vector3 pos = Vector3.Lerp(frameA.position, frameB.position, progression);
            Quaternion rot = Quaternion.Lerp(frameA.rotation, frameB.rotation, progression);
            return new QJointTransform(pos, rot);
        }
    }
}
