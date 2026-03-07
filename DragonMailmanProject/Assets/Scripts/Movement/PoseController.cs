using Posing;
using UnityEngine;

namespace Movement
{
    public class PoseController : MonoBehaviour
    {
        [Header("--- Ragdoll Reference ---")]
        public ActiveRagdollController ragdollController;

        [Header("--- Poses ---")]
        public RagdollPose hoverPose;
        public RagdollPose flyPose;

        public void SetHoverPose()
        {
            ragdollController.SetPose(hoverPose);
        }

        public void SetFlyPose()
        {
            ragdollController.SetPose(flyPose);
        }
    }
}
