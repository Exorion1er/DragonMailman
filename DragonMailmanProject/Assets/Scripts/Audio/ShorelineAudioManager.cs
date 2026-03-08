using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Audio
{
    public class ShorelineAudioManager : MonoBehaviour
    {
        public Transform player;
        public EventReference oceanSfx;
        public Transform shorelinePointsParent;
        public float waterLevelY;
        public float maxAudioDistance;

        private EventInstance oceanInstance;
        private Vector2[] shorePoints;

        private void Start()
        {
            int pointCount = shorelinePointsParent.childCount;
            shorePoints = new Vector2[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 pos = shorelinePointsParent.GetChild(i).position;
                shorePoints[i] = new Vector2(pos.x, pos.z);
            }

            oceanInstance = RuntimeManager.CreateInstance(oceanSfx);
            oceanInstance.start();
        }

        private void Update()
        {
            if (shorePoints.Length < 3 || !player) return;

            Vector2 playerPos2D = new(player.position.x, player.position.z);
            bool isLand = IsPlayerInland(playerPos2D);

            Vector3 targetWaterPos;
            if (!isLand)
                targetWaterPos = new Vector3(player.position.x, waterLevelY, player.position.z);
            else
            {
                Vector2 closestBeachPoint2D = GetClosestPointOnShore(playerPos2D);
                targetWaterPos = new Vector3(closestBeachPoint2D.x, waterLevelY, closestBeachPoint2D.y);
            }

            float trueDistance = Vector3.Distance(player.position, targetWaterPos);
            Vector3 finalSoundPosition;

            if (trueDistance < 0.01f)
                finalSoundPosition = player.position;
            else
            {
                Vector3 directionToWater = (targetWaterPos - player.position).normalized;
                float clampedDistance = Mathf.Min(trueDistance, maxAudioDistance);
                finalSoundPosition = player.position + directionToWater * clampedDistance;
            }

            transform.position = finalSoundPosition;
            oceanInstance.set3DAttributes(finalSoundPosition.To3DAttributes());
        }

        private void OnDestroy()
        {
            oceanInstance.stop(STOP_MODE.ALLOWFADEOUT);
            oceanInstance.release();
        }

        private bool IsPlayerInland(Vector2 p)
        {
            bool isInside = false;
            int j = shorePoints.Length - 1;
            for (int i = 0; i < shorePoints.Length; i++)
            {
                if (shorePoints[i].y > p.y != shorePoints[j].y > p.y && p.x <
                    (shorePoints[j].x - shorePoints[i].x) * (p.y - shorePoints[i].y) /
                    (shorePoints[j].y - shorePoints[i].y) + shorePoints[i].x)
                    isInside = !isInside;
                j = i;
            }
            return isInside;
        }

        private Vector2 GetClosestPointOnShore(Vector2 p)
        {
            Vector2 closest = Vector2.zero;
            float minSqrDist = Mathf.Infinity;

            int j = shorePoints.Length - 1;
            for (int i = 0; i < shorePoints.Length; i++)
            {
                Vector2 closestOnSegment = ClosestPointOnLineSegment(p, shorePoints[j], shorePoints[i]);
                float sqrDist = (p - closestOnSegment).sqrMagnitude;

                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = closestOnSegment;
                }
                j = i;
            }
            return closest;
        }

        private static Vector2 ClosestPointOnLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 aToP = p - a;
            Vector2 aToB = b - a;
            float sqrLen = aToB.sqrMagnitude;

            if (sqrLen == 0) return a;

            float t = Mathf.Clamp01(Vector2.Dot(aToP, aToB) / sqrLen);
            return a + aToB * t;
        }
    }
}
