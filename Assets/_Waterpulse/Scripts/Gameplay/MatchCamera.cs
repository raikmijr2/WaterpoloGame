using UnityEngine;
using Waterpulse.Config;

namespace Waterpulse.Gameplay
{
    // Cámara fija e inclinada que se aleja lo justo para que todo el campo quepa con cualquier proporción de pantalla
    [RequireComponent(typeof(Camera))]
    public class MatchCamera : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] PoolBounds pool;

        const int SearchIterations = 24;
        const float MinDistance = 1f;
        const float MaxDistance = 200f;

        Camera cam;
        readonly Vector3[] corners = new Vector3[8];

        void Awake() => cam = GetComponent<Camera>();

        // Se recalcula cada frame: cubre cambios de tamaño de ventana, rotación del móvil y ajustes en Play
        void LateUpdate() => Fit();

        public void Fit()
        {
            if (config == null || pool == null) return;
            if (cam == null) cam = GetComponent<Camera>();
            cam.fieldOfView = config.cameraFieldOfView;

            FillCorners();
            Quaternion rotation = Quaternion.Euler(config.cameraPitch, 0f, 0f);
            Vector3 center = pool.Center;

            // Búsqueda binaria de la distancia mínima en la que todas las esquinas quedan dentro del viewport
            float lo = MinDistance, hi = MaxDistance;
            for (int i = 0; i < SearchIterations; i++)
            {
                float mid = (lo + hi) * 0.5f;
                Place(center, rotation, mid);
                if (AllCornersVisible()) hi = mid;
                else lo = mid;
            }
            Place(center, rotation, hi);
        }

        void Place(Vector3 center, Quaternion rotation, float distance)
        {
            transform.SetPositionAndRotation(center - rotation * Vector3.forward * distance, rotation);
        }

        void FillCorners()
        {
            float hx = pool.HalfLength + config.cameraMargin;
            float hz = pool.HalfWidth + config.cameraMargin;
            float y0 = pool.WaterY;
            float y1 = pool.WaterY + config.goalHeight;
            Vector3 c = pool.Center;
            int i = 0;
            foreach (float y in new[] { y0, y1 })
            {
                corners[i++] = new Vector3(c.x - hx, y, c.z - hz);
                corners[i++] = new Vector3(c.x + hx, y, c.z - hz);
                corners[i++] = new Vector3(c.x - hx, y, c.z + hz);
                corners[i++] = new Vector3(c.x + hx, y, c.z + hz);
            }
        }

        bool AllCornersVisible()
        {
            foreach (var corner in corners)
            {
                Vector3 v = cam.WorldToViewportPoint(corner);
                if (v.z <= 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) return false;
            }
            return true;
        }
    }
}
