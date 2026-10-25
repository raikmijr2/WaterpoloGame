using UnityEngine;

namespace Waterpulse.Gameplay
{
    // Robo con J: solo desde delante del que lleva la pelota. De espaldas, la protege con el cuerpo
    public static class RecoverGeometry
    {
        // true si el ladrón está dentro del arco frontal (frontAngle grados en total) del poseedor
        public static bool IsInFront(Vector3 holderPosition, Vector3 holderForward, Vector3 thiefPosition, float frontAngle)
        {
            Vector3 forward = new Vector3(holderForward.x, 0f, holderForward.z);
            Vector3 toThief = thiefPosition - holderPosition;
            toThief.y = 0f;
            if (forward.sqrMagnitude < 0.0001f || toThief.sqrMagnitude < 0.0001f) return true;
            return Vector3.Angle(forward, toThief) <= frontAngle * 0.5f;
        }
    }
}
