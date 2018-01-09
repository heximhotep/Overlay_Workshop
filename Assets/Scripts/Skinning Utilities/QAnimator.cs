using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinningUtilities
{
    public class QAnimator
    {
        public QAnimation currentAnimation;
        public QModel entity;
        float animationTime = 0;

        public QAnimator(QModel _entity)
        {
            entity = _entity;
        }

        public void DoAnimation(QAnimation animation)
        {
            animationTime = 0;
            currentAnimation = animation;
        }

        public void Update()
        {
            if (currentAnimation == null)
                return;
            IncreaseAnimationTime();
            Dictionary<string, Matrix4x4> currentPose = CalculateCurrentAnimationPose();
            ApplyPoseToJoints(currentPose, entity.GetRootJoint(), new Matrix4x4());

            
        }

        void IncreaseAnimationTime()
        {
            animationTime += Time.deltaTime;
            if (animationTime > currentAnimation.length)
            {
                animationTime %= currentAnimation.length;
            }
        }

        Dictionary<string, Matrix4x4> CalculateCurrentAnimationPose()
        {
            Dictionary<string, Matrix4x4> result = new Dictionary<string, Matrix4x4>();



            return result;
        }

        void ApplyPoseToJoints(Dictionary<string, Matrix4x4> currentPose, QJoint joint, Matrix4x4 parentTransform)
        {
            Matrix4x4 currentLocalTransform = currentPose[joint.name];
            Matrix4x4 currentTransform = parentTransform * currentLocalTransform;
            foreach (QJoint child in joint.children)
            {
                ApplyPoseToJoints(currentPose, child, currentTransform);
            }
            //currentTransform *= joint.GetInverseBindTransform();
            //joint.animatedTransform = currentTransform;
        }
    }
    /*
    class QAnimator
    {
        public QModel entity;
        public QAnimation currentAnimation;
        float animationTime = 0;

        public QAnimator(QModel _entity)
        {
            entity = _entity;
        }

        public void DoAnimation(QAnimation animation)
        {
            animationTime = 0;
            currentAnimation = animation;
        }

        public void Update()
        {
            if (currentAnimation == null)
                return;
            IncreaseAnimationTime();
            Dictionary<string, Matrix4x4> currentPose = CalculateCurrentAnimationPose();
            ApplyPoseToJoints(currentPose, entity.GetRootJoint(), new Matrix4x4());
        }

        void IncreaseAnimationTime()
        {
            animationTime += Time.deltaTime;
            if (animationTime > currentAnimation.length)
                animationTime %= currentAnimation.length;
        }

        Dictionary<string, Matrix4x4> CalculateCurrentAnimationPose()
        {
            Dictionary<string, Matrix4x4> result = new Dictionary<string, Matrix4x4>();
            var nameEnumerator = currentAnimation.jointNames.GetEnumerator();
            while(nameEnumerator.MoveNext())
            {
                string jointName = nameEnumerator.Current;
                QKeyframe[] frameWindow = GetFrameWindow(jointName);
                float progression = CalculateProgression(frameWindow[0], frameWindow[1]);
                result.Add(jointName, QJointTransform.Interpolate(frameWindow[0].pose, frameWindow[1].pose, progression).GetLocalTransform());
            }
            return result;
        }

        void ApplyPoseToJoints(Dictionary<string, Matrix4x4> currentPose, QJoint joint, Matrix4x4 parentTransform)
        {
            Matrix4x4 currentLocalTransform = currentPose[joint.name];
            Matrix4x4 currentTransform = parentTransform * currentLocalTransform;
            foreach(QJoint child in joint.children)
            {
                ApplyPoseToJoints(currentPose, child, currentTransform);
            }
            currentTransform *= joint.GetInverseBindTransform();
            joint.animatedTransform = currentTransform;
        }

        QKeyframe[] GetFrameWindow(string jointName)
        {
            QKeyframe[] frames = currentAnimation.keyFrames[jointName];
            QKeyframe prevFrame = frames[0], nextFrame = frames[0];
            for(int i = 1; i < frames.Length; i++)
            {
                nextFrame = frames[i];
                if (nextFrame.timeStamp > animationTime)
                    break;
                prevFrame = frames[i];
            }
            return new[] { prevFrame, nextFrame };
        }

        float CalculateProgression(QKeyframe prevFrame, QKeyframe nextFrame)
        {
            float totalTime = nextFrame.timeStamp - prevFrame.timeStamp;
            float currentTime = animationTime - prevFrame.timeStamp;
            return totalTime > 0 ? currentTime / totalTime : 0;
        }
    }
    */
}
