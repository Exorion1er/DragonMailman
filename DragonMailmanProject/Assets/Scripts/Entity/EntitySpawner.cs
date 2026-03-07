using System.Collections.Generic;
using UnityEngine;

namespace Entity
{
    public class EntitySpawner : MonoBehaviour
    {
        public GameController gameController;
        public GameObject entityPrefab;
        public GameObject entitiesParent;

        private int lastIndex;
        private Vector3[] spawnPoints;

        public void Awake()
        {
            lastIndex = -1;
            spawnPoints = new Vector3[entitiesParent.transform.childCount];
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                spawnPoints[i] = entitiesParent.transform.GetChild(i).position;
                Destroy(entitiesParent.transform.GetChild(i).gameObject);
            }
        }

        public void SpawnRandomEntity(int amountToSpawn)
        {
            if (spawnPoints.Length == 0) return;
            int actualAmountToSpawn = Mathf.Min(amountToSpawn, spawnPoints.Length);

            List<int> availableIndices = new();
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                // Avoid the last point from the previous wave (if we aren't using every single point)
                if (actualAmountToSpawn < spawnPoints.Length && i == lastIndex) continue;

                availableIndices.Add(i);
            }

            for (int i = 0; i < actualAmountToSpawn; i++)
            {
                int randomListIndex = Random.Range(0, availableIndices.Count);
                int chosenSpawnIndex = availableIndices[randomListIndex];

                // Remove that index from the pool so we don't spawn two items in the exact same spot
                availableIndices.RemoveAt(randomListIndex);

                Spawn(chosenSpawnIndex);
            }
        }

        private void Spawn(int index)
        {
            lastIndex = index;
            GameObject gob = Instantiate(entityPrefab, spawnPoints[index], Quaternion.identity,
                entitiesParent.transform);

            if (gob.TryGetComponent(out EntityPickup pickup)) pickup.gameController = gameController;
        }
    }
}
