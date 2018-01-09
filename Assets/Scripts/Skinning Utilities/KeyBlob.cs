using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinningUtilities
{
    public class ScalarKeyFrame
    {
        public readonly float inTangent, outTangent, value, time;
        public ScalarKeyFrame(float _intangent, float _outtangent, float _value, float _time)
        {
            inTangent = _intangent;
            outTangent = _outtangent;
            value = _value;
            time = _time;
        }
    }

    public struct ScalarFrame
    {
        public float inTangent, outTangent, value;
        public ScalarFrame(float _intangent, float _outtangent, float _value)
        {
            inTangent = _intangent;
            outTangent = _outtangent;
            value = _value;
        }
    }

    public struct ScalarBlob
    {
        public readonly string attributeName;
        public readonly SortedList<float, ScalarFrame> values;
        public ScalarBlob(string _attribName, SortedList<float, ScalarFrame> _values)
        {
            attributeName = _attribName;
            values = _values;
        }
    }

    public class KeyBlob
    {
        public readonly Dictionary<string, ScalarBlob> keyedAttributes;
        public KeyBlob( Dictionary<string, ScalarBlob> attribs)
        {
            keyedAttributes = attribs;
        }
    }

    
}
