using Animation;
using UnityEngine;

namespace Entity
{
    public class EntityPickup : MonoBehaviour
    {
        public GameController gameController;
        public IdleBounceRotate idle;
        public GameObject beam;
        public PickupType pickupType;
        public float initialSpeed;
        public float acceleration;
        public float maxSpeed;
        public float pickupDistance;
        public float tumbleDegreesPerSecond;
        public float axisDriftDegreesPerSecond;

        private float currentSpeed;
        private bool movingToPlayer;
        private Transform player;
        private Vector3 tumbleAxis;

        private void Update()
        {
            if (!movingToPlayer) return;

            if (tumbleAxis.sqrMagnitude < 1e-8f || float.IsNaN(tumbleAxis.x) || float.IsInfinity(tumbleAxis.x))
                tumbleAxis = Random.onUnitSphere;
            else
                tumbleAxis.Normalize();

            if (axisDriftDegreesPerSecond > 0f)
            {
                Vector3 driftAxis = Vector3.Cross(tumbleAxis, Vector3.up);
                if (driftAxis.sqrMagnitude < 1e-6f) driftAxis = Vector3.right;
                driftAxis.Normalize();

                tumbleAxis = Quaternion.AngleAxis(axisDriftDegreesPerSecond * Time.deltaTime, driftAxis) * tumbleAxis;
                tumbleAxis.Normalize();
            }

            transform.Rotate(tumbleAxis, tumbleDegreesPerSecond * Time.deltaTime, Space.Self);

            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            Vector3 targetPos = player.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) <= pickupDistance) PickedUp();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (movingToPlayer || !other.CompareTag("PickupTrigger")) return;

            player = other.transform;
            StartPickupAnimation();
        }

        private void StartPickupAnimation()
        {
            movingToPlayer = true;
            currentSpeed = initialSpeed;
            tumbleAxis = Random.onUnitSphere;
            idle.enabled = false;
            if (beam) beam.SetActive(false);
        }

        private void PickedUp()
        {
            Destroy(gameObject);
            gameController.EntityPickedUp(pickupType);
        }
    }
}
