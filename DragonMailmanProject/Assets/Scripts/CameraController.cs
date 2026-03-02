using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public float smoothSpeed;

    private Vector3 velocity;

    private void LateUpdate()
    {
        Vector3 desiredPosition = target.transform.position + offset;
        transform.position =  Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        transform.LookAt(target);
    }
}
