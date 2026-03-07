#if UNITY_EDITOR
using Posing;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class RagdollPoseExporter : EditorWindow
    {
        public Transform rigRoot;
        public string poseFileName = "pose";

        private void OnGUI()
        {
            GUILayout.Label("Export Rig Rotations", EditorStyles.boldLabel);

            rigRoot = (Transform)EditorGUILayout.ObjectField("Rig Root Bone", rigRoot, typeof(Transform), true);
            poseFileName = EditorGUILayout.TextField("Save As (Name)", poseFileName);

            if (GUILayout.Button("Export Pose to ScriptableObject")) ExportPose();
        }

        [MenuItem("Tools/Active Ragdoll/Pose Exporter")]
        public static void ShowWindow()
        {
            GetWindow<RagdollPoseExporter>("Pose Exporter");
        }

        private void ExportPose()
        {
            if (rigRoot == null)
            {
                Debug.LogWarning("Please assign the root bone of the rig.");
                return;
            }

            RagdollPose poseAsset = CreateInstance<RagdollPose>();
            Transform[] allBones = rigRoot.GetComponentsInChildren<Transform>();

            foreach (Transform bone in allBones)
            {
                poseAsset.jointPoses.Add(new RagdollPose.JointPose
                {
                    jointName = bone.name,
                    localRotation = bone.localRotation
                });
            }

            // Ensure the Assets directory exists
            string path = $"Assets/{poseFileName}.asset";
            AssetDatabase.CreateAsset(poseAsset, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>Success!</color> Pose saved to {path}");

            // Highlight the file in the project window
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = poseAsset;
        }
    }
}
#endif
