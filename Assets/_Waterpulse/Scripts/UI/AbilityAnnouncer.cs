using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.UI
{
    // Muestra en grande, en un bàner, el nombre de la habilidad al activarse ("XUT DEL TRÀNGOL!")
    public class AbilityAnnouncer : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [Tooltip("Bàner que se ve solo mientras se anuncia la habilidad")]
        [SerializeField] GameObject panel;
        [SerializeField] Text label;

        float timer;

        void OnEnable() => GameEvents.AbilityUsed += OnAbilityUsed;
        void OnDisable() => GameEvents.AbilityUsed -= OnAbilityUsed;

        void Awake()
        {
            label.text = string.Empty;
            panel.SetActive(false);
        }

        void OnAbilityUsed(GameObject player, string abilityName)
        {
            label.text = abilityName.ToUpperInvariant() + "!";
            panel.SetActive(true);
            timer = config.abilityAnnounceDuration;
        }

        void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            label.text = string.Empty;
            panel.SetActive(false);
        }
    }
}
