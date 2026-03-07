using UnityEngine;

public class RandomMesh : MonoBehaviour
{
    public GameObject[] meshes;

    private void Start()
    {
        foreach (GameObject gob in meshes)
        {
            gob.SetActive(false);
        }

        meshes[Random.Range(0, meshes.Length)].SetActive(true);
    }
}
