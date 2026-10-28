using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Panel del jugador que controla el humano (abajo a la izquierda), como una ficha de retransmisión: rol abreviado en
    // grande, nombre del rol, energía (barra con una marca en el coste del Xut del Tràngol y la cifra) y estamina en una línea
    // fina. Con energía para el Xut, la barra se resalta un poco (EnergyHighlight). Las barras se mueven suavemente
    // hacia su valor. Si pide la habilidad sin energía, el carril de la barra parpadea en rojo. Sustituye al texto "Energia: N"
    public class PlayerHudView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [Tooltip("Rol abreviado, en grande (ATA, ORG, DEF)")]
        [SerializeField] Text roleShortLabel;
        [SerializeField] Text roleLabel;
        [Tooltip("Relleno de la barra de energía: su anchorMax.x es el valor (0-1)")]
        [SerializeField] RectTransform energyFill;
        [Tooltip("Carril de la barra de energía: parpadea en rojo si se pide la habilidad sin energía")]
        [SerializeField] Image energyTrack;
        [Tooltip("Cifra de la energía")]
        [SerializeField] Text energyValueLabel;
        [Tooltip("Marca en el coste del Xut del Tràngol")]
        [SerializeField] RectTransform abilityCostMark;
        [SerializeField] EnergyHighlight energyHighlight;
        [SerializeField] Color noEnergyColor = new Color(0.9f, 0.2f, 0.22f);
        [Tooltip("Duración del parpadeo en rojo del carril al pedir la habilidad sin energía (s)")]
        [SerializeField] float noEnergyFlashDuration = 0.5f;
        [SerializeField] RectTransform staminaFill;
        [Tooltip("Lo que se mueven las barras por segundo hacia su valor (fracción de la barra)")]
        [SerializeField] float barSpeed = 2.5f;
        [Tooltip("Nombres de rol en el orden de PlayerRole: Attacker, Playmaker, Defender, Goalkeeper")]
        [SerializeField] string[] roleNames = { "Atacant", "Organitzador", "Defensor", "Porter" };
        [Tooltip("Abreviatura de cada rol, en el mismo orden (la misma que la mini placa)")]
        [SerializeField] string[] roleShortNames = { "ATA", "ORG", "DEF", "POR" };

        PlayerController player;
        PlayerEnergy energy;
        PlayerStamina stamina;
        float shownEnergy;
        float shownStamina;
        Color trackColor;
        float flashTimer;

        void Awake()
        {
            trackColor = energyTrack.color;
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
            roleShortLabel.text = role >= 0 && role < roleShortNames.Length ? roleShortNames[role] : string.Empty;

            var ability = player.PowerShot;
            bool hasAbility = ability != null && energy != null && energy.Max > 0f;
            abilityCostMark.gameObject.SetActive(hasAbility);
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
            energyHighlight.Snap(IsReady());
            Apply();
        }

        void Update()
        {
            if (player == null) return;
            if (flashTimer > 0f) flashTimer -= Time.deltaTime;
            float step = barSpeed * Time.deltaTime;
            if (energy != null) shownEnergy = Mathf.MoveTowards(shownEnergy, energy.Normalized, step);
            if (stamina != null) shownStamina = Mathf.MoveTowards(shownStamina, stamina.Normalized, step);
            energyHighlight.SetReady(IsReady());
            Apply();
        }

        bool IsReady() => player.PowerShot != null && energy != null && energy.CanSpend(player.PowerShot.cost);

        void Apply()
        {
            SetFill(energyFill, shownEnergy);
            SetFill(staminaFill, shownStamina);
            energyValueLabel.text = energy != null ? Mathf.FloorToInt(energy.Current).ToString() : string.Empty;
            float flash = noEnergyFlashDuration > 0f ? Mathf.Clamp01(flashTimer / noEnergyFlashDuration) : 0f;
            energyTrack.color = Color.Lerp(trackColor, noEnergyColor, flash);
        }

        static void SetFill(RectTransform fill, float value) => fill.anchorMax = new Vector2(Mathf.Clamp01(value), fill.anchorMax.y);
    }
}
