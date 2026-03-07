using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Posing
{
    [CreateAssetMenu(fileName = "New Ragdoll Pose", menuName = "Active Ragdoll/Pose Data")]
    public class RagdollPose : ScriptableObject
    {
        public List<JointPose> jointPoses = new();

        // Helper method to easily grab a rotation by bone name
        public bool TryGetRotation(string jointName, out Quaternion rotation)
        {
            foreach (JointPose pose in jointPoses.Where(pose => pose.jointName == jointName))
            {
                rotation = pose.localRotation;
                return true;
            }
            rotation = Quaternion.identity;
            return false;
        }

        [Serializable]
        public struct JointPose
        {
            public string jointName;
            public Quaternion localRotation;
        }
    }
}
