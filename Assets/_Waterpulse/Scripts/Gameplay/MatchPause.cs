using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Pausa del partido: para el tiempo (Time.timeScale = 0) y avisa con GameEvents.PauseChanged para que nadie
    // obedezca a su cerebro. No se puede pausar con el partido acabado. Al continuar espera resumeDelay (sin escalar)
    // antes de seguir, para que la tecla con la que se confirma no se convierta en un pase o un tiro
    public class MatchPause : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] MatchFlow match;

        float scaleBeforePause = 1f;
        float resumeAt = -1f;   // < 0: no se está reanudando

        public bool IsPaused { get; private set; }
        public bool CanPause => !IsPaused && match.State != MatchState.FullTime && match.State != MatchState.Results;

        public bool Pause()
        {
            if (!CanPause) return false;
            IsPaused = true;
            resumeAt = -1f;
            scaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            GameEvents.RaisePauseChanged(true);
            return true;
        }

        public void Resume()
        {
            if (IsPaused && resumeAt < 0f) resumeAt = Time.unscaledTime + config.resumeDelay;
        }

        void Update()
        {
            if (resumeAt < 0f || Time.unscaledTime < resumeAt) return;
            resumeAt = -1f;
            IsPaused = false;
            Time.timeScale = scaleBeforePause;
            GameEvents.RaisePauseChanged(false);
        }

        // Al cambiar de escena desde la pausa (reiniciar o volver al menú) el tiempo vuelve a correr
        void OnDestroy()
        {
            if (IsPaused) Time.timeScale = scaleBeforePause;
        }
    }
}
