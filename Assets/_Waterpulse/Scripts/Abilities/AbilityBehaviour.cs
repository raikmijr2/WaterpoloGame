using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Abilities
{
    // Base de las habilidades: al pedirla el jugador (L), comprueba que se puede usar, gasta la energía,
    // la ejecuta, salpica y avisa con AbilityUsed. Sin energía suficiente no se usa y avisa con AbilityNoEnergy
    // (el HUD y el sonido se lo hacen saber al humano). Cada jugador de campo lleva las dos habilidades y las dos escuchan
    // la misma tecla: sus condiciones (con o sin pelota) no se solapan, así que solo responde una
    [RequireComponent(typeof(PlayerController), typeof(PlayerEnergy))]
    public abstract class AbilityBehaviour : MonoBehaviour
    {
        [Tooltip("Salpicadura al activarse")]
        [SerializeField] ParticleSystem splash;

        protected PlayerController Player { get; private set; }
        protected PlayerEnergy Energy { get; private set; }
        // Definición de esta habilidad en el jugador (PowerShot o Boost)
        protected abstract AbilityDefinitionSO Definition { get; }

        protected virtual void Awake()
        {
            Player = GetComponent<PlayerController>();
            Energy = GetComponent<PlayerEnergy>();
        }

        void OnEnable() => Player.AbilityRequested += TryActivate;
        void OnDisable() => Player.AbilityRequested -= TryActivate;

        void TryActivate()
        {
            if (Definition == null || !CanActivate()) return;
            if (!Energy.TrySpend(Definition.cost))
            {
                GameEvents.RaiseAbilityNoEnergy(gameObject);
                return;
            }
            Activate();
            if (splash != null) splash.Play();
            GameEvents.RaiseAbilityUsed(gameObject, Definition.displayName);
        }

        // Condición propia de la habilidad (p. ej. tener la pelota). Se comprueba antes de gastar energía
        protected abstract bool CanActivate();
        protected abstract void Activate();
    }
}
