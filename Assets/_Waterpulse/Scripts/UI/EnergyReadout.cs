using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Provisional hasta el HUD de la Fase 8: energía del jugador que controla el humano
    public class EnergyReadout : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] string format = "Energia: {0}";

        PlayerEnergy energy;

        void OnEnable() => GameEvents.ControlledPlayerChanged += OnControlledChanged;
        void OnDisable() => GameEvents.ControlledPlayerChanged -= OnControlledChanged;

        void OnControlledChanged(GameObject player) => energy = player != null ? player.GetComponent<PlayerEnergy>() : null;

        void Update()
        {
            label.text = energy != null ? string.Format(format, Mathf.FloorToInt(energy.Current)) : string.Empty;
        }
    }
}
