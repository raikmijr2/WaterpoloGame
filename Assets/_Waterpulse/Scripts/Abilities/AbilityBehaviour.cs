using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Abilities
{
    // Base de las habilidades: al pedirla el jugador (L), comprueba que se puede usar, gasta la energía,
    // la ejecuta, salpica y avisa con AbilityUsed. Sin energía suficiente no pasa nada
    [RequireComponent(typeof(PlayerController), typeof(PlayerEnergy))]
    public abstract class AbilityBehaviour : MonoBehaviour
    {
        [Tooltip("Salpicadura al activarse")]
        [SerializeField] ParticleSystem splash;

        protected PlayerController Player { get; private set; }
        protected PlayerEnergy Energy { get; private set; }
        protected AbilityDefinitionSO Definition => Player.Ability;

        protected virtual void Awake()
        {
            Player = GetComponent<PlayerController>();
            Energy = GetComponent<PlayerEnergy>();
        }

        void OnEnable() => Player.AbilityRequested += TryActivate;
        void OnDisable() => Player.AbilityRequested -= TryActivate;

        void TryActivate()
        {
            if (Definition == null || !CanActivate() || !Energy.TrySpend(Definition.cost)) return;
            Activate();
            if (splash != null) splash.Play();
            GameEvents.RaiseAbilityUsed(gameObject, Definition.displayName);
        }

        // Condición propia de la habilidad (p. ej. tener la pelota). Se comprueba antes de gastar energía
        protected abstract bool CanActivate();
        protected abstract void Activate();
    }
}
