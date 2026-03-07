using System;
using System.Collections.Generic;
using UnityEngine;

namespace Poses
{
    public class ActiveRagdollController : MonoBehaviour
    {
        public List<JointController> joints = new();
        public RagdollPose currentTargetPose;
        public float transitionSpeed = 5f;

        private float blendFactor = 1f;
        private RagdollPose previousPose;

        private void FixedUpdate()
        {
            if (blendFactor < 1f)
            {
                blendFactor += Time.fixedDeltaTime * transitionSpeed;
                blendFactor = Mathf.Clamp01(blendFactor);
            }

            ApplyBlendedPose();
        }

        [ContextMenu("Auto-Find Joints")]
        public void InitializeJoints()
        {
            joints.Clear();
            ConfigurableJoint[] foundJoints = GetComponentsInChildren<ConfigurableJoint>();

            foreach (ConfigurableJoint cj in foundJoints)
            {
                joints.Add(new JointController
                {
                    jointName = cj.gameObject.name,
                    joint = cj,
                    startingLocalRotation = cj.transform.localRotation
                });
            }
        }

        [ContextMenu("Apply Pose")]
        private void ApplySelectedPose()
        {
            SetPose(currentTargetPose);
        }

        public void SetPose(RagdollPose newPose)
        {
            if (currentTargetPose != newPose)
            {
                previousPose = currentTargetPose;
                currentTargetPose = newPose;
                blendFactor = 0f;
            }

            foreach (JointController jc in joints)
            {
                if (newPose.TryGetRotation(jc.jointName, out Quaternion targetRot))
                    jc.joint.targetRotation = CalculateTargetRotation(targetRot, jc.startingLocalRotation);
            }
        }

        private void ApplyBlendedPose()
        {
            foreach (JointController jc in joints)
            {
                if (!currentTargetPose.TryGetRotation(jc.jointName, out Quaternion targetRot)) continue;

                Quaternion finalTargetRot = targetRot;

                if (blendFactor < 1f && previousPose)
                    if (previousPose.TryGetRotation(jc.jointName, out Quaternion prevRot))
                        finalTargetRot = Quaternion.Slerp(prevRot, targetRot, blendFactor);

                jc.joint.targetRotation = CalculateTargetRotation(finalTargetRot, jc.startingLocalRotation);
            }
        }

        private static Quaternion
            CalculateTargetRotation(Quaternion targetLocalRotation, Quaternion startLocalRotation) =>
            Quaternion.Inverse(targetLocalRotation) * startLocalRotation;

        [Serializable]
        public class JointController
        {
            public string jointName;
            public ConfigurableJoint joint;
            [HideInInspector]
            public Quaternion startingLocalRotation;
        }
    }
}
