using UnityEngine;

namespace Waterpulse.Gameplay
{
    // Cálculos puros de la cámara que sigue la jugada: a qué distancia ponerse para encuadrar
    // un largo dado y cómo limitar el punto que mira para no enseñar demasiado fuera de la piscina
    public static class CameraFraming
    {
        // Distancia a la que el ancho visible horizontal es viewLength (con el FOV vertical y la proporción de pantalla)
        public static float DistanceForViewLength(float viewLength, float verticalFovDeg, float aspect)
        {
            float halfV = verticalFovDeg * 0.5f * Mathf.Deg2Rad;
            float halfH = Mathf.Atan(Mathf.Tan(halfV) * Mathf.Max(0.01f, aspect));
            return viewLength / (2f * Mathf.Tan(halfH));
        }

        // Media profundidad (eje Z) visible sobre el agua a esa distancia con la inclinación dada
        public static float HalfViewDepth(float distance, float verticalFovDeg, float pitchDeg)
        {
            float halfV = verticalFovDeg * 0.5f * Mathf.Deg2Rad;
            float sinPitch = Mathf.Max(0.1f, Mathf.Sin(pitchDeg * Mathf.Deg2Rad));
            return distance * Mathf.Tan(halfV) / sinPitch;
        }

        // Mantiene el foco donde la vista no se salga del campo más el margen.
        // Si la vista es más grande que el campo en un eje, el foco queda centrado en ese eje
        public static Vector3 ClampFocus(Vector3 focus, Vector3 center, float halfLength, float halfWidth,
                                         float margin, float halfViewX, float halfViewZ)
        {
            float limitX = Mathf.Max(0f, halfLength + margin - halfViewX);
            float limitZ = Mathf.Max(0f, halfWidth + margin - halfViewZ);
            focus.x = Mathf.Clamp(focus.x, center.x - limitX, center.x + limitX);
            focus.z = Mathf.Clamp(focus.z, center.z - limitZ, center.z + limitZ);
            return focus;
        }
    }
}
