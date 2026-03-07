using UnityEngine;

namespace Animation
{
    public class IdleBounceRotate : MonoBehaviour
    {
        public float bounceSpeed;
        public float bounceAmplitude;
        public float rotateSpeed;

        private Vector3 startPosition;

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

            float bounceOffset = Mathf.Sin(Time.time * bounceSpeed) * bounceAmplitude;
            transform.position = startPosition + new Vector3(0f, bounceOffset, 0f);
        }
    }
}
