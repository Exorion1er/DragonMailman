using System.Collections.Generic;
using UnityEngine;

public class PrefabConeEmitter : MonoBehaviour
{
    [Header("Emission Settings")]
    public GameObject prefabToEmit;
    public float emissionRate = 5f;

    [Header("Trajectory & Force"), Range(0f, 90f)]
    public float coneAngle = 30f;
    public float minLaunchForce = 10f;
    public float maxLaunchForce = 25f;

    [Header("Cleanup Settings")]
    public float prefabLifespan = 4f;

    private readonly Queue<TrackedPrefab> activePrefabsQueue = new();

    private float nextEmitTime;

    private void Update()
    {
        if (Time.time >= nextEmitTime)
        {
            EmitPrefab();
            nextEmitTime = Time.time + 1f / emissionRate;
        }

        CleanupOldPrefabs();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position;
        float radius = Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2 / 8;
            Vector3 localDir = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 1f).normalized;
            Gizmos.DrawRay(origin, transform.TransformDirection(localDir) * 5f);
        }
    }

    private void EmitPrefab()
    {
        GameObject spawnedObject = Instantiate(prefabToEmit, transform.position, transform.rotation);
        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        Vector3 launchDirection = GetRandomDirectionInCone();
        float launchForce = Random.Range(minLaunchForce, maxLaunchForce);

        rb.AddForce(launchDirection * launchForce, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * launchForce, ForceMode.VelocityChange);

        TrackedPrefab newTrackedPrefab = new()
        {
            instance = spawnedObject,
            destroyTime = Time.time + prefabLifespan
        };

        activePrefabsQueue.Enqueue(newTrackedPrefab);
    }

    private void CleanupOldPrefabs()
    {
        while (activePrefabsQueue.Count > 0 && Time.time >= activePrefabsQueue.Peek().destroyTime)
        {
            TrackedPrefab oldestPrefab = activePrefabsQueue.Dequeue();

            if (oldestPrefab.instance != null) Destroy(oldestPrefab.instance);
        }
    }

    private Vector3 GetRandomDirectionInCone()
    {
        Vector2 randomPointInCircle = Random.insideUnitCircle;
        float radius = Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);
        Vector3 localDirection =
            new Vector3(randomPointInCircle.x * radius, randomPointInCircle.y * radius, 1f).normalized;
        return transform.TransformDirection(localDirection);
    }

    private struct TrackedPrefab
    {
        public GameObject instance;
        public float destroyTime;
    }
}