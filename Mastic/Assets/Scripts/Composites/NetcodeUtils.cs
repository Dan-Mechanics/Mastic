using UnityEngine;

namespace Mastic
{
    public static class NetcodeUtils
    {
        public static void AllowNoregLenience(Transform cam, ShootMessage shootMessage, float tolerance, LayerMask noregMask)
        {
            float dist = Vector3.Distance(cam.position, shootMessage.origin);
            if (dist <= tolerance)
                return;

            // DEBUG.
            Vector3 debugDiff = (cam.position - shootMessage.origin) / Time.fixedDeltaTime;
            Debug.LogWarning($"if (cam.position != shootMessage.origin) | if ({cam.position} != {shootMessage.origin})");
            Debug.LogWarning($"diff {debugDiff.magnitude}");

            // ALLOW LENIENCY IF WAS RECENTLY BOOPED AND ALSO RAYCAST LOS.
            Vector3 dir = shootMessage.origin - cam.position;
            dir.Normalize();
            if (!Physics.Raycast(cam.position, dir, out RaycastHit hit, dist, noregMask, QueryTriggerInteraction.Ignore))
            {
                cam.position = shootMessage.origin;
            }
            else
            {
                float wallBump = 0.1f;
                Vector3 difference = cam.position - hit.point;
                dist = difference.magnitude - wallBump;
                if (dist > tolerance)
                {
                    difference = Vector3.ClampMagnitude(difference, dist);
                    cam.position += difference;
                }
            }
        }

        public static bool GetAimingPoint(PlayerEntity entity, Transform cam, float range, LayerMask mask, ref Vector3 point)
        {
            entity.EnableHitbox(false);
            bool found = Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore);
            entity.EnableHitbox(true);
            if (found)
                point = hit.point;

            return found;
        }
    }
}