using UnityEngine;
using System.Collections.Generic;
using SkinningUtilities;
#if (UNITY_EDITOR)
using UnityEditor;

// Editor window for listing all float curves in an animation clip
public class ClipInfo : EditorWindow
{
    private AnimationClip clip;
    private Mesh mesh;
    private string savePath;

    [MenuItem("Window/Clip Info")]
    static void Init()
    {
        GetWindow(typeof(ClipInfo));
    }

    public void OnGUI()
    {
        clip = EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false) as AnimationClip;
        mesh = EditorGUILayout.ObjectField("Model", mesh, typeof(Mesh), false) as Mesh;

        if (GUILayout.Button("Encode Animation Curves"))
        {
            if (clip != null)
            {
                Dictionary<string, List<string>> hierarchedBindings = new Dictionary<string, List<string>>();
                
                List<string> encodings = new List<string>();

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if(!hierarchedBindings.ContainsKey(binding.path))
                    {
                        List<string> bindFrames = new List<string>();
                        hierarchedBindings.Add(binding.path, bindFrames);
                    }

                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    
                    string thisJointField = string.Format("#_#{0}##{1}", binding.propertyName, curve.keys.Length);
                    hierarchedBindings[binding.path].Add(thisJointField);

                    foreach (Keyframe key in curve.keys)
                    {
                        string thisKeyFrame = string.Format("{0}*#*{1}*#*{2}*#*{3}", key.inTangent, key.value, key.outTangent, key.time);
                        hierarchedBindings[binding.path].Add(thisKeyFrame);
                    }
                }

                encodings.Add(clip.length.ToString());

                var bindingEnum = hierarchedBindings.Keys.GetEnumerator();
                while(bindingEnum.MoveNext())
                {
                    if (encodings.Count > 1)
                        encodings.Add(string.Format("&$*{0}", bindingEnum.Current));
                    else
                        encodings.Add(bindingEnum.Current);
                    var keyframes = hierarchedBindings[bindingEnum.Current];
                    foreach(string frame in keyframes)
                    {
                        encodings.Add(frame);
                    }
                }
                string path = @"C:\Users\User\Documents\nu_art\tin_drum\workshops\Flocking_Workshop\Assets\Scripts\" + clip.name + ".bagel";
                System.IO.File.WriteAllLines(path, encodings.ToArray());

                BagelLoader bagel = new BagelLoader(mesh);
                QAnimation myAnim = bagel.LoadBagel(path);
                for(int i = 0; i < 5; i++)
                {
                    TransformData sample = myAnim.GetTransformAt(0, myAnim.length / 5 * i);
                    Debug.Log(string.Format("Position: {0} \nRotation: {1} \nScale: {2}", sample.position.ToString(), sample.rotation.ToString(), sample.scale.ToString()));
                }
                var th = myAnim.GetType().TypeHandle;
                unsafe
                {
                    long size = *(*(long**)&th + 1);
                    Debug.Log(size);
                }
                
            }
        }
        
    }
}
#endif