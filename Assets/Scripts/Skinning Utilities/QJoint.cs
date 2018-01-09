 using System;
using UnityEngine;
using System.Collections.Generic;
namespace SkinningUtilities
{
    
    public class QJoint
    {
        public readonly int index;
        public readonly string name;
        public readonly List<QJoint> children = new List<QJoint>();
        public readonly Matrix4x4 bindTransform, inverseBindTransform;
        public Matrix4x4 animatedTransform;

        public QJoint(int _idx, string _name, Matrix4x4 _inverseBindTransform)
        {
            index = _idx;
            name = _name;
            inverseBindTransform = _inverseBindTransform;
            bindTransform = inverseBindTransform.inverse;
            animatedTransform = new Matrix4x4();
        }

        public void AddChild(QJoint child)
        {
            children.Add(child);
        }
    }
}
