using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Panel del jugador que controla el humano (abajo a la izquierda): su rol, la energía, con una marca en el coste de
    // su habilidad y la tecla encendida cuando llega, y la estamina. Las barras se mueven suavemente hacia su valor.
    // Si pide la habilidad sin energía, la tecla parpadea en rojo y da un pequeño latido. Sustituye al texto "Energia: N"
    public class PlayerHudView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text roleLabel;
        [Tooltip("Relleno de la barra de energía: su anchorMax.x es el valor (0-1)")]
        [SerializeField] RectTransform energyFill;
        [Tooltip("Marca en el coste de la habilidad del jugador")]
        [SerializeField] RectTransform abilityCostMark;
        [Tooltip("Tecla de la habilidad: encendida si hay energía para usarla")]
        [SerializeField] CanvasGroup abilityKey;
        [Tooltip("Fondo de la tecla de la habilidad: parpadea en rojo si se pide sin energía")]
        [SerializeField] Image abilityKeyBackground;
        [SerializeField] Color noEnergyColor = new Color(0.9f, 0.2f, 0.22f);
        [Tooltip("Duración del parpadeo en rojo de la tecla al pedir la habilidad sin energía (s)")]
        [SerializeField] float noEnergyFlashDuration = 0.5f;
        [Tooltip("Cuánto crece la tecla al empezar el parpadeo (fracción de su tamaño)")]
        [SerializeField] float noEnergyPulse = 0.25f;
        [SerializeField] RectTransform staminaFill;
        [Tooltip("Lo que se mueven las barras por segundo hacia su valor (fracción de la barra)")]
        [SerializeField] float barSpeed = 2.5f;
        [Tooltip("Transparencia de la tecla de la habilidad cuando no hay energía para usarla")]
        [Range(0f, 1f)] [SerializeField] float keyDimAlpha = 0.25f;
        [Tooltip("Nombres de rol en el orden de PlayerRole: Attacker, Playmaker, Defender, Goalkeeper")]
        [SerializeField] string[] roleNames = { "Atacant", "Organitzador", "Defensor", "Porter" };

        PlayerController player;
        PlayerEnergy energy;
        PlayerStamina stamina;
        float shownEnergy;
        float shownStamina;
        Color keyColor;
        float flashTimer;

        void Awake()
        {
            keyColor = abilityKeyBackground.color;
            panel.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.ControlledPlayerChanged += OnControlledChanged;
            GameEvents.AbilityNoEnergy += OnAbilityNoEnergy;
        }

        void OnDisable()
        {
            GameEvents.ControlledPlayerChanged -= OnControlledChanged;
            GameEvents.AbilityNoEnergy -= OnAbilityNoEnergy;
        }

        void OnAbilityNoEnergy(GameObject go)
        {
            if (player != null && go == player.gameObject) flashTimer = noEnergyFlashDuration;
        }

        void OnControlledChanged(GameObject go)
        {
            player = go != null ? go.GetComponent<PlayerController>() : null;
            energy = go != null ? go.GetComponent<PlayerEnergy>() : null;
            stamina = go != null ? go.GetComponent<PlayerStamina>() : null;
            panel.SetActive(player != null);
            if (player == null) return;

            int role = (int)player.Role;
            roleLabel.text = (role >= 0 && role < roleNames.Length ? roleNames[role] : player.Role.ToString()).ToUpperInvariant();

            var ability = player.Ability;
            bool hasAbility = ability != null && energy != null && energy.Max > 0f;
            abilityCostMark.gameObject.SetActive(hasAbility);
            abilityKey.gameObject.SetActive(hasAbility);
            if (hasAbility)
            {
                float cost = Mathf.Clamp01(ability.cost / energy.Max);
                abilityCostMark.anchorMin = new Vector2(cost, abilityCostMark.anchorMin.y);
                abilityCostMark.anchorMax = new Vector2(cost, abilityCostMark.anchorMax.y);
            }

            // Al cambiar de jugador, las barras saltan a su valor en vez de deslizarse desde el del anterior
            shownEnergy = energy != null ? energy.Normalized : 0f;
            shownStamina = stamina != null ? stamina.Normalized : 0f;
            flashTimer = 0f;
            Apply();
        }

        void Update()
        {
            if (player == null) return;
            if (flashTimer > 0f) flashTimer -= Time.deltaTime;
            float step = barSpeed * Time.deltaTime;
            if (energy != null) shownEnergy = Mathf.MoveTowards(shownEnergy, energy.Normalized, step);
            if (stamina != null) shownStamina = Mathf.MoveTowards(shownStamina, stamina.Normalized, step);
            Apply();
        }

        void Apply()
        {
            SetFill(energyFill, shownEnergy);
            SetFill(staminaFill, shownStamina);
            if (!abilityKey.gameObject.activeSelf) return;
            // Parpadeo al pedirla sin energía: roja, a la vista aunque esté apagada, y con un latido que se deshincha
            float flash = noEnergyFlashDuration > 0f ? Mathf.Clamp01(flashTimer / noEnergyFlashDuration) : 0f;
            bool ready = energy != null && energy.CanSpend(player.Ability.cost);
            abilityKey.alpha = ready || flash > 0f ? 1f : keyDimAlpha;
            abilityKeyBackground.color = Color.Lerp(keyColor, noEnergyColor, flash);
            abilityKey.transform.localScale = Vector3.one * (1f + noEnergyPulse * flash);
        }

        static void SetFill(RectTransform fill, float value) => fill.anchorMax = new Vector2(Mathf.Clamp01(value), fill.anchorMax.y);
    }
}
