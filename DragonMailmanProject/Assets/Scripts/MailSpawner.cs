using UnityEngine;

public class MailSpawner : MonoBehaviour
{
    public GameObject mailPrefab;
    public GameObject mailParent;

    private int lastIndex;
    private Vector3[] spawnPoints;

    public void Awake()
    {
        lastIndex = -1;
        spawnPoints = new Vector3[mailParent.transform.childCount];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPoints[i] = mailParent.transform.GetChild(i).position;
            Destroy(mailParent.transform.GetChild(i).gameObject);
        }
    }

    public void SpawnRandomMail()
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
        Instantiate(mailPrefab, spawnPoints[index], Quaternion.identity, mailParent.transform);
    }
}