using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Provisional hasta el HUD de la Fase 9: energía y estamina del jugador que controla el humano
    public class EnergyReadout : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] string format = "Energia: {0}   Estamina: {1}";

        PlayerEnergy energy;
        PlayerStamina stamina;

        void OnEnable() => GameEvents.ControlledPlayerChanged += OnControlledChanged;
        void OnDisable() => GameEvents.ControlledPlayerChanged -= OnControlledChanged;

        void OnControlledChanged(GameObject player)
        {
            energy = player != null ? player.GetComponent<PlayerEnergy>() : null;
            stamina = player != null ? player.GetComponent<PlayerStamina>() : null;
        }

        void Update()
        {
            if (energy == null && stamina == null)
            {
                label.text = string.Empty;
                return;
            }
            int e = energy != null ? Mathf.FloorToInt(energy.Current) : 0;
            int s = stamina != null ? Mathf.FloorToInt(stamina.Current) : 0;
            label.text = string.Format(format, e, s);
        }
    }
}
