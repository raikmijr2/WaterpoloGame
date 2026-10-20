using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.UI
{
    // Muestra en grande el nombre de la habilidad al activarse ("XUT DEL TRÀNGOL!")
    public class AbilityAnnouncer : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] Text label;

        float timer;

        void OnEnable() => GameEvents.AbilityUsed += OnAbilityUsed;
        void OnDisable() => GameEvents.AbilityUsed -= OnAbilityUsed;

        void Awake() => label.text = string.Empty;

        void OnAbilityUsed(GameObject player, string abilityName)
        {
            label.text = abilityName.ToUpperInvariant() + "!";
            timer = config.abilityAnnounceDuration;
        }

        void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) label.text = string.Empty;
        }
    }
}
