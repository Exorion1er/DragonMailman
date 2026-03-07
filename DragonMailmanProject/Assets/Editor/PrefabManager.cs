using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class PrefabManager : EditorWindow
    {
        private bool convexColliders;
        private Transform newParent;
        private string prefabFolder = "Assets/Prefabs/MapAssets";

        private void OnGUI()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);
            convexColliders = EditorGUILayout.Toggle("Convex Mesh Colliders", convexColliders);

            EditorGUILayout.Space();

            if (GUILayout.Button("1. Convert Assets to Prefabs and Add Mesh Collider")) CreatePrefabsFromProject();

            if (GUILayout.Button("2. Re-link Scene Objects to Prefabs By Mesh Name")) RelinkSceneToPrefabs();
            EditorGUILayout.Space();
            GUILayout.Label("Hierarchy Organization", EditorStyles.boldLabel);
            newParent = (Transform)EditorGUILayout.ObjectField("New Parent (Optional)", newParent, typeof(Transform),
                true);
        }

        [MenuItem("Tools/Prefab Manager")]
        public static void ShowWindow() => GetWindow<PrefabManager>("Prefab Manager");

        private void CreatePrefabsFromProject()
        {
            Object[] selectedAssets = Selection.GetFiltered<Object>(SelectionMode.Assets);
            if (!Directory.Exists(prefabFolder)) Directory.CreateDirectory(prefabFolder);

            foreach (Object asset in selectedAssets)
            {
                GameObject fbx = asset as GameObject;
                if (!fbx) continue;

                MeshFilter mf = fbx.GetComponentInChildren<MeshFilter>();
                string nameToUse = mf && mf.sharedMesh ? mf.sharedMesh.name : asset.name;
                string path = Path.Combine(prefabFolder, nameToUse + ".prefab");

                GameObject tempGo = Instantiate(fbx);

                // --- COLLIDER LOGIC ---
                // Find all mesh filters (in case the FBX has multiple sub-meshes)
                MeshFilter[] meshFilters = tempGo.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter filter in meshFilters)
                {
                    if (filter.sharedMesh)
                    {
                        // Add MeshCollider if it doesn't already have one
                        MeshCollider mc = filter.gameObject.GetComponent<MeshCollider>();
                        if (!mc) mc = filter.gameObject.AddComponent<MeshCollider>();

                        mc.sharedMesh = filter.sharedMesh;
                        mc.convex = convexColliders;
                    }
                }
                // ----------------------

                PrefabUtility.SaveAsPrefabAsset(tempGo, path);
                DestroyImmediate(tempGo);
            }
            AssetDatabase.Refresh();
            Debug.Log("Step 1 Complete: Prefabs created with Mesh Colliders.");
        }

        private void RelinkSceneToPrefabs()
        {
            GameObject[] sceneObjects = Selection.gameObjects;
            string[] prefabFiles = Directory.GetFiles(prefabFolder, "*.prefab");
            Dictionary<string, GameObject> prefabMap = new();

            foreach (string file in prefabFiles)
            {
                GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                if (pf) prefabMap[pf.name] = pf;
            }

            int count = 0;
            foreach (GameObject sceneObj in sceneObjects)
            {
                MeshFilter mf = sceneObj.GetComponentInChildren<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;

                string meshName = mf.sharedMesh.name;

                if (prefabMap.ContainsKey(meshName))
                {
                    GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabMap[meshName]);

                    Transform parentToUse = newParent ? newParent : sceneObj.transform.parent;
                    newInstance.transform.SetParent(parentToUse);
                    newInstance.transform.position = sceneObj.transform.position;
                    newInstance.transform.rotation = sceneObj.transform.rotation;
                    newInstance.transform.localScale = sceneObj.transform.localScale;

                    Undo.RegisterCreatedObjectUndo(newInstance, "Replace with Prefab");
                    Undo.DestroyObjectImmediate(sceneObj);
                    count++;
                }
            }
            Debug.Log($"Step 2 Complete: Re-linked {count} instances.");
        }
    }
}
