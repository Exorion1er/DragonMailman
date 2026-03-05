using UnityEngine;

public class MailIdle : MonoBehaviour
{
    public float bounceSpeed;
    public float bounceAmplitude;
    public float rotateSpeed;
    public float floorOffset;

    private Vector3 startPos;

    private void Start()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit))
        {
            startPos = hit.point += new Vector3(0, floorOffset, 0);
        }
        else
        {
            Debug.Log($"MailIdle on {gameObject.name}: No floor detected below, using current position");
            startPos = transform.position;
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        float newY = Mathf.Sin(Time.time * bounceSpeed) * bounceAmplitude;
        transform.position = startPos + new Vector3(0, newY, 0);
    }
}
