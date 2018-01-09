using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkinningUtilities
{
    

    public class QKeyframe
    {
        public readonly float timeStamp;
        public readonly QJointTransform pose;
        
        public QKeyframe(float _timeStamp, QJointTransform _pose)
        {
            timeStamp = _timeStamp;
            pose = _pose;
        }
    }

    public struct TransformData
    {
        public Vector3 position, scale;
        public Quaternion rotation;
    }

    public class QAnimation
    {
        public readonly float length;
        public readonly HashSet<string> jointNames;
        public readonly QJoint hierarchy;
        public readonly Dictionary<string, KeyBlob> keyFrames;

        public QAnimation(float _length, HashSet<string> _jointNames, QJoint _hierarchy, Dictionary<string, KeyBlob> _keyFrames)
        {
            length = _length;
            hierarchy = _hierarchy;
            jointNames = _jointNames;
            keyFrames = _keyFrames;
        }

        public QJoint FindJointByIndex(int idx)
        {
            QJoint result = hierarchy;
            if (result.index == idx)
                return result;
            List<QJoint> remaining = new List<QJoint>();
            List<QJoint> nextBatch = new List<QJoint>();
            remaining.AddRange(result.children);
            do
            {
                var jEnum = remaining.GetEnumerator();
                while(jEnum.MoveNext())
                {
                    QJoint joint = jEnum.Current;
                    if (joint.index == idx)
                        return joint;
                    nextBatch.AddRange(joint.children);
                }
                jEnum.Dispose();
                remaining = new List<QJoint>(nextBatch);
                nextBatch.Clear();
            }
            while (remaining.Count > 0);
            Debug.LogError("joint index not found!");
            return null;
        }

        int BinarySearchLowerBound(IList<float> items, float target)
        {
            int result = items.Count / 2;
            int divisor = 4;
            do
            {
                if (items[result] == target)
                    return result;
                if (items[result] < target)
                {
                    result += items.Count / divisor;
                }
                else
                {
                    result -= items.Count / divisor;
                }
                divisor *= 2;
            } while (items.Count / divisor > 0);
            if (items[result] > target)
                return result - 1;
            else
                return result;
        }

        float CalculateInterpolation(float t, float minOutTangent, float minValue, float minTime,
                                              float maxInTangent, float maxValue, float maxTime)
        {
            float dt = maxTime - minTime;

            float tOff = t - minTime;

            return ((tOff * maxValue) + (dt - tOff) * minValue) / dt;

            /*
            float m0 = minOutTangent * dt;
            float m1 = maxInTangent * dt;

            float t2 = t * t;
            float t3 = t2 * t;

            float a = 2 * t3 - 3 * t2 + 1;
            float b = t3 - 2 * t2 + t;
            float c = t3 - t2;
            float d = -2 * t3 + 3 * t2;

            return a * minValue + b * m0 + c * m1 + d * maxValue;
            */
        }

        public TransformData GetTransformAt(int jointIdx, float time)
        {
            TransformData result = new TransformData();
            Vector3 rPosition = new Vector3();
            Vector4 rRotation = new Vector4();
            Vector3 rScale = new Vector3();
            QJoint joint = FindJointByIndex(jointIdx);
            var JointKeyFrames = keyFrames[joint.name];
            foreach(string attribName in JointKeyFrames.keyedAttributes.Keys)
            {
                SortedList<float, ScalarFrame> frames = JointKeyFrames.keyedAttributes[attribName].values;
                int minIdx = BinarySearchLowerBound(frames.Keys, time);
                float minTime = frames.Keys[minIdx];
                float lerpVal = frames[minTime].value;
                if(minIdx + 1 < frames.Keys.Count)
                {
                    float minTangent = frames[minTime].outTangent;
                    float maxTime = frames.Keys[minIdx + 1];
                    float maxVal = frames[maxTime].value;
                    float maxTangent = frames[maxTime].inTangent;
                    lerpVal = CalculateInterpolation(time, minTangent, lerpVal, minTime,
                                                           maxTangent,  maxVal, maxTime);
                }
                switch (attribName)
                {
                    case ("m_LocalPosition.x"):
                        rPosition.x = lerpVal;
                        break;
                    case ("m_LocalPosition.y"):
                        rPosition.y = lerpVal;
                        break;
                    case ("m_LocalPosition.z"):
                        rPosition.z = lerpVal;
                        break;

                    case ("m_LocalRotation.x"):
                        rRotation.x = lerpVal;
                        break;
                    case ("m_LocalRotation.y"):
                        rRotation.y = lerpVal;
                        break;
                    case ("m_LocalRotation.z"):
                        rRotation.z = lerpVal;
                        break;
                    case ("m_LocalRotation.w"):
                        rRotation.w = lerpVal;
                        break;

                    case ("m_LocalScale.x"):
                        rScale.x = lerpVal;
                        break;
                    case ("m_LocalScale.y"):
                        rScale.y = lerpVal;
                        break;
                    case ("m_LocalScale.z"):
                        rScale.z = lerpVal;
                        break;
                }
            }
            result.position = rPosition;
            rRotation.Normalize();
            result.rotation = new Quaternion(rRotation.x, rRotation.y, rRotation.z, rRotation.w);
            result.scale = rScale;
            return result;
        }
    }
}
