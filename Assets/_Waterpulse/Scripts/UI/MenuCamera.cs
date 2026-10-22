using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Cámara de fondo del menú: gira despacio alrededor del centro de la piscina, siempre mirándolo
    [RequireComponent(typeof(Camera))]
    public class MenuCamera : MonoBehaviour
    {
        [SerializeField] MenuConfigSO config;
        [SerializeField] PoolBounds pool;

        float angle;

        void Awake() => GetComponent<Camera>().fieldOfView = config.cameraFieldOfView;

        void LateUpdate()
        {
            angle = Mathf.Repeat(angle + config.cameraOrbitSpeed * Time.unscaledDeltaTime, 360f);
            Place(angle);
        }

        // Coloca la cámara en un ángulo de la vuelta (grados). Con 0 se ve la piscina de lado, a lo largo
        public void Place(float degrees)
        {
            Vector3 center = pool.Center;
            Vector3 offset = Quaternion.Euler(0f, degrees, 0f) * new Vector3(0f, config.cameraHeight, -config.cameraDistance);
            transform.SetPositionAndRotation(center + offset, Quaternion.LookRotation(-offset));
        }
    }
}
