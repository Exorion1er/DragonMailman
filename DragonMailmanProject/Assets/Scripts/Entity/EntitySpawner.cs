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

        public void SpawnRandomEntity()
        {
            // If there's only one point, we can't avoid repeating it.
            if (spawnPoints.Length <= 1)
            {
                Spawn(0);
                return;
            }

            int randomIndex = lastIndex;

            // Keep picking a new index until it's different from the last one
            while (randomIndex == lastIndex)
            {
                randomIndex = Random.Range(0, spawnPoints.Length);
            }

            Spawn(randomIndex);
        }

        private void Spawn(int index)
        {
            lastIndex = index;
            GameObject gob = Instantiate(entityPrefab, spawnPoints[index], Quaternion.identity,
                entitiesParent.transform);
            gob.GetComponent<EntityPickup>().gameController = gameController;
        }
    }
}
