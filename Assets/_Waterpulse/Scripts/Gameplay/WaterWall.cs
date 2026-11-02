using UnityEngine;

namespace Waterpulse.Gameplay
{
    // Muro de agua delante de una portería: mientras está levantado, el portero para todos los tiros que llegan
    // (GoalkeeperController). Lo levanta la habilidad del portero (MurAiguaAbility). Se ve una cortina de agua y una
    // esquitxada que sube a lo largo de la boca de la portería
    public class WaterWall : MonoBehaviour
    {
        [Tooltip("Cortina de agua (se enciende mientras está levantado)")]
        [SerializeField] GameObject curtain;
        [Tooltip("Esquitxada que sube a lo largo del muro (emite en continuo; aquí se ajusta cuánto)")]
        [SerializeField] ParticleSystem spray;
        [Tooltip("Partículas por segundo de la esquitxada con el muro levantado")]
        [SerializeField] float sprayRate = 140f;

        float timer;

        public bool IsUp => timer > 0f;
        // Posición del muro (los tiros que la cruzan hacia la portería se paran)
        public Vector3 Position => transform.position;

        void Awake() => Apply();

        public void Raise(float duration)
        {
            timer = Mathf.Max(timer, duration);
            Apply();
        }

        void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) Apply();
        }

        void Apply()
        {
            bool up = IsUp;
            if (curtain.activeSelf != up) curtain.SetActive(up);
            var emission = spray.emission;
            emission.rateOverTime = up ? sprayRate : 0f;
        }
    }
}
