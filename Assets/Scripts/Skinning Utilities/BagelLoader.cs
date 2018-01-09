using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinningUtilities
{
    public class BagelLoader
    {
        Mesh model;
        QJoint hierarchy;
        HashSet<string> jointNames;
        Dictionary<string, KeyBlob> keyframes;
        Dictionary<string, Dictionary<string, ScalarBlob>> scalarBlobs;

        public BagelLoader(Mesh _model)
        {
            model = _model;
            jointNames = new HashSet<string>();
            keyframes = new Dictionary<string, KeyBlob>();
            scalarBlobs = new Dictionary<string, Dictionary<string, ScalarBlob>>();
        }

        QJoint SiftChildren(QJoint parent, string targetName)
        {
            foreach (QJoint child in parent.children)
            {
                if (child.name == targetName)
                    return child;
            }
            return null;
        }

        public QAnimation LoadBagel(string path)
        {
            int jointIndex = 0;

            string bagelData = System.IO.File.ReadAllText(path);

            string[] jointBlocks = bagelData.Split(new[] { "&$*" }, StringSplitOptions.RemoveEmptyEntries);

            float animationLength = -1f;

            Matrix4x4[] bindXForms = model.bindposes;

            for(int i = 0; i < jointBlocks.Length; i++)
            {
                string jointBlock = jointBlocks[i];
                string[] lines = jointBlock.Split('\n');
                //we offset the base index since the first jointblock has the
                //animation length encoded in the 0th line
                int baseIndex = i == 0 ? 1 : 0;
                if (i == 0)
                    animationLength = float.Parse(lines[0]);

                string jointPath = lines[baseIndex];
                string[] hierarchyOrder = jointPath.Split(new[] { '/', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string jointName = hierarchyOrder[hierarchyOrder.Length - 1];

                jointNames.Add(jointName);
                
                scalarBlobs.Add(jointName, new Dictionary<string, ScalarBlob>());

                //case on whether we are reading the root
                if (hierarchy == null)
                {
                    hierarchy = new QJoint(jointIndex, jointName, bindXForms[jointIndex]);
                    jointIndex++;
                }
                else
                {
                    QJoint hierarchyWalker = hierarchy;

                    int hierarchyIndex = 1;
                    while (hierarchyIndex < hierarchyOrder.Length - 1)
                    {
                        hierarchyWalker = SiftChildren(hierarchyWalker, hierarchyOrder[hierarchyIndex++]);
                    }

                    //Matrix4x4 localBind = bindXForms[hierarchyWalker.index] * bindXForms[jointIndex].inverse;

                    //int startIdx = jointName.IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
                    //string numSub = jointName.Substring(startIdx);
                    //jointIndex = int.Parse(numSub) - 1;
                    QJoint newJoint = hierarchyWalker;
                    if(jointIndex != 0)
                    {
                        newJoint = new QJoint(jointIndex, jointName, bindXForms[jointIndex]);
                        hierarchyWalker.AddChild(newJoint);
                    }
                    jointIndex++;
                }
                

                //parsing keyframes for each attribute
                string[] animatedAttributes = jointBlock.Split(new[] { "#_#" }, StringSplitOptions.None);
                //we ignore the 0th entry in animatedAttributes since that will contain the information that
                //we just parsed before
                for (int j = 1; j < animatedAttributes.Length; j++)
                {
                    string[] attributeLines = animatedAttributes[j].Split('\n');
                    string attributeNameAndElementLength = attributeLines[0];
                    string[] nameAndLength = attributeNameAndElementLength.Split(new[] { "##" }, StringSplitOptions.None);
                    string attributeName = nameAndLength[0];
                    int nKeyframes = int.Parse(nameAndLength[1]);
                    for(int k = 1; k < nKeyframes + 1; k++)
                    {
                        string[] curveProperties = attributeLines[k].Split(new[] { "*#*" }, StringSplitOptions.None);
                        float inTangent = float.Parse(curveProperties[0]),
                            frameValue = float.Parse(curveProperties[1]),
                            outTangent = float.Parse(curveProperties[2]),
                            time = float.Parse(curveProperties[3]);
                        ScalarKeyFrame propKey = new ScalarKeyFrame( inTangent, outTangent, frameValue, time);
                        ScalarFrame propFrame = new ScalarFrame(inTangent, outTangent, frameValue);
                        //check if we have previous entries for a property
                        if(!scalarBlobs[jointName].ContainsKey(attributeName))
                        {
                            scalarBlobs[jointName].Add(attributeName, new ScalarBlob(attributeName, new SortedList<float, ScalarFrame>()));
                        }
                        scalarBlobs[jointName][attributeName].values.Add(time, propFrame);
                    }
                }
                keyframes.Add(jointName, new KeyBlob(scalarBlobs[jointName]));
            }
            return new QAnimation(animationLength, jointNames, hierarchy, keyframes);
        }

        
        //KeyBlob ComposeScalarKeyFrames()

        /*
        SortedList<float, KeyBlob> ComposeScalarKeyFrames(Dictionary<string, ScalarBlob> keys)
        {
            SortedList<float, KeyBlob> result = new SortedList<float, KeyBlob>();
            var timeZones = new SortedList<float, Dictionary<string, ScalarKeyFrame>>();
            var keyEnum = keys.Keys.GetEnumerator();
            while(keyEnum.MoveNext())
            {
                ScalarBlob theseKeys = keys[keyEnum.Current];
                foreach(KeyValuePair<thisTime,  frame in theseKeys)
                {
                    if(!timeZones.ContainsKey(frame.time))
                    {
                        timeZones.Add(frame.time, new Dictionary<string, ScalarKeyFrame>());
                    }

                    timeZones[frame.time][keyEnum.Current] = frame;
                }
            }
            foreach(KeyValuePair<float, Dictionary<string, ScalarKeyFrame>> timeZone in timeZones)
            {
                KeyBlob blob = new KeyBlob(timeZone.Key, timeZone.Value);
                result.Add(timeZone.Key, blob);
            }
            return result;
        }
        */
    }
}
