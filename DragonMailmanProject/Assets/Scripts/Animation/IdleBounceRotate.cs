using UnityEngine;

namespace Animation
{
    public class IdleBounceRotate : MonoBehaviour
    {
        public float bounceSpeed;
        public float bounceAmplitude;
        public float rotateSpeed;

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

            float newY = Mathf.Sin(Time.time * bounceSpeed) * bounceAmplitude;
            transform.position = new Vector3(0, newY, 0);
        }
    }
}
