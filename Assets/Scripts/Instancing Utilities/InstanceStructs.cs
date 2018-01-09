using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SkinningUtilities;

namespace InstancingUtilities
{
    public struct Agent
    {
        public Vector3 position, velocity;
        public Agent(Vector3 _pos, Vector3 _vel)
        {
            position = _pos;
            velocity = _vel;
        }
    }

    public struct KeyFrame
    {
        public float time, value;
        public KeyFrame(float _time, float _value)
        {
            time = _time; value = _value;
        }
    }

    public struct JointData
    {
        int tXStartIdx, tXLength;
        int tYStartIdx, tYLength;
        int tZStartIdx, tZLength;
        int rXStartIdx, rXLength;
        int rYStartIdx, rYLength;
        int rZStartIdx, rZLength;
        int rWStartIdx, rWLength;
        int sXStartIdx, sXLength;
        int sYStartIdx, sYLength;
        int sZStartIdx, sZLength;
        int numChildren;
        Matrix4x4 bindXForm, inverseBindXForm;
        public JointData(int[] trsArgs, int _numChildren, Matrix4x4 _inverseBindXForm)
        {
            numChildren = _numChildren;
            bindXForm = _inverseBindXForm.inverse;
            inverseBindXForm = _inverseBindXForm;

            tXStartIdx = trsArgs[0];
            tXLength = trsArgs[1];

            tYStartIdx = trsArgs[2];
            tYLength = trsArgs[3];

            tZStartIdx = trsArgs[4];
            tZLength = trsArgs[5];

            rXStartIdx = trsArgs[6];
            rXLength = trsArgs[7];

            rYStartIdx = trsArgs[8];
            rYLength = trsArgs[9];

            rZStartIdx = trsArgs[10];
            rZLength = trsArgs[11];

            rWStartIdx = trsArgs[12];
            rWLength = trsArgs[13];

            sXStartIdx = trsArgs[14];
            sXLength = trsArgs[15];

            sYStartIdx = trsArgs[16];
            sYLength = trsArgs[17];

            sZStartIdx = trsArgs[18];
            sZLength = trsArgs[19];
        }
    }

    public struct AnimationData
    {
        public List<KeyFrame>[] keyframes;
        public JointData[] jointData;
    }

    public class Hierarchy
    {
        public QAnimation animation;
        public List<int> entries;
        int GetHierarchySize(QJoint walker)
        {
            int result = 1;
            if (walker.children.Count > 0)
            {
                result += walker.children.Count;
                foreach (QJoint child in walker.children)
                {
                    result += GetHierarchySize(child);
                }
            }
            return result;
        }

        public Hierarchy(QAnimation _animation)
        {
            animation = _animation;
            int numJoints = animation.jointNames.Count;
            QJoint walker = animation.hierarchy;
            int hLen = GetHierarchySize(walker);
            entries = new List<int>(hLen);
            Queue<QJoint> remaining = new Queue<QJoint>();
            remaining.Enqueue(walker);
            List<List<int>> pieces = new List<List<int>>(numJoints);
            for (int i = 0; i < numJoints; i++)
            {
                pieces.Add(new List<int>());
            }
            while (remaining.Count > 0)
            {
                QJoint joint = remaining.Dequeue();
                pieces[joint.index].Add(joint.children.Count);
                foreach (QJoint child in joint.children)
                {
                    pieces[joint.index].Add(child.index);
                    remaining.Enqueue(child);
                }
            }
            for (int i = 0; i < numJoints; i++)
            {
                List<int> piece = pieces[i];
                entries.AddRange(piece);
            }
        }

        public int NumChildren(int jointIdx)
        {
            int result = entries[0];
            int curIdx = 0;
            while(jointIdx > 0)
            {
                curIdx += 1 + result;
                result = entries[curIdx];
                jointIdx--;
            }
            return result;
        }

        public QJoint FindJointByName(string name)
        {
            Queue<QJoint> remaining = new Queue<QJoint>();
            remaining.Enqueue(animation.hierarchy);
            while(remaining.Count > 0)
            {
                QJoint result = remaining.Dequeue();
                if (result.name == name)
                    return result;
                foreach(var child in result.children)
                {
                    remaining.Enqueue(child);
                }
            }
            return null;
        }

        public AnimationData MakeKeyFramesAndJointData(Mesh model)
        {
            int numJoints = animation.jointNames.Count;
            var keyBlobs = animation.keyFrames;
            AnimationData result = new AnimationData();
            result.jointData = new JointData[numJoints];
            result.keyframes = new List<KeyFrame>[10];
            for (int i = 0; i < 10; i++)
                result.keyframes[i] = new List<KeyFrame>();
            List<QJoint> indexedJoints = new List<QJoint>(numJoints);
            for(int i = 0; i < numJoints; i++)
            {
                indexedJoints.Add(null);
            }
            foreach(KeyValuePair<string, KeyBlob> kv in keyBlobs)
            {
                QJoint curJoint = FindJointByName(kv.Key);
                int curidx = curJoint.index;
                indexedJoints[curidx] = curJoint;
            }
            for (int i = 0; i < numJoints; i++)
            {
                QJoint joint = indexedJoints[i];
                KeyBlob blob = keyBlobs[joint.name];

                int[] jointDataArgs = new int[20];

                for (int j = 0; j < 10; j++)
                {
                    string attribName = "m_LocalPosition.x";
                    switch (j)
                    {
                        case (1):
                            attribName = "m_LocalPosition.y";
                            break;
                        case (2):
                            attribName = "m_LocalPosition.z";
                            break;
                        case (3):
                            attribName = "m_LocalRotation.x";
                            break;
                        case (4):
                            attribName = "m_LocalRotation.y";
                            break;
                        case (5):
                            attribName = "m_LocalRotation.z";
                            break;
                        case (6):
                            attribName = "m_LocalRotation.w";
                            break;
                        case (7):
                            attribName = "m_LocalScale.x";
                            break;
                        case (8):
                            attribName = "m_LocalScale.y";
                            break;
                        case (9):
                            attribName = "m_LocalScale.z";
                            break;
                    }
                    SortedList<float, ScalarFrame> frames = blob.keyedAttributes[attribName].values;
                    jointDataArgs[j * 2] = result.keyframes[j].Count;
                    jointDataArgs[j * 2 + 1] = frames.Count;
                    for (int k = 0; k < frames.Count; k++)
                    {
                        float time = frames.Keys[k];
                        ScalarFrame frame = frames.Values[k];
                        KeyFrame newFrame = new KeyFrame(time, frame.value);
                        result.keyframes[j].Add(newFrame);
                    }
                }
                int numChildren = NumChildren(i);
                Matrix4x4 bindPose = model.bindposes[i];
                result.jointData[i] = new JointData(jointDataArgs, numChildren, bindPose);
            }
            return result;
        }
    }
}
