using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Cámara lenta mientras alguien (humano o IA, de los dos equipos) carga un tiro: da tiempo a apuntar y, si tira el
    // rival, a coger al portero (E) y levantar el muro. El paso de física se escala igual para que todo siga suave.
    // En la pausa no toca nada (MatchPause pone el tiempo a 0 y lo devuelve como estaba)
    public class ShotSlowMotion : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;

        PlayerController[] players;
        float baseFixedDeltaTime;
        bool paused;

        void Awake() => baseFixedDeltaTime = Time.fixedDeltaTime;

        void Start() => players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        void OnEnable() => GameEvents.PauseChanged += OnPauseChanged;
        void OnDisable() => GameEvents.PauseChanged -= OnPauseChanged;

        void OnPauseChanged(bool isPaused) => paused = isPaused;

        void Update()
        {
            if (paused || players == null) return;
            bool charging = false;
            foreach (var p in players)
            {
                if (!p.IsChargingShot) continue;
                charging = true;
                break;
            }
            SetScale(charging ? config.shotSlowMotionScale : 1f);
        }

        void SetScale(float scale)
        {
            if (Mathf.Approximately(Time.timeScale, scale)) return;
            Time.timeScale = scale;
            Time.fixedDeltaTime = baseFixedDeltaTime * scale;
        }

        // Al salir de la escena (reiniciar, menú) el tiempo vuelve a la normalidad
        void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDeltaTime;
        }
    }
}
