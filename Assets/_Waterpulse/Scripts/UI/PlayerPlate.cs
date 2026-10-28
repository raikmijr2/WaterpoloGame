using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Mini placa sobre un jugador del equipo humano: su rol y su energía en una barra fina, con una marca en el coste
    // del Xut del Tràngol (el ataque especial). Cuando llega a ese coste, la barra se resalta un poco (EnergyHighlight).
    // PlayerMarkersView crea una por jugador a partir de una plantilla y la coloca en pantalla
    public class PlayerPlate : MonoBehaviour
    {
        [SerializeField] Text roleLabel;
        [Tooltip("Relleno de la barra de energía: su anchorMax.x es el valor (0-1)")]
        [SerializeField] RectTransform energyFill;
        [Tooltip("Marca en el coste del Xut del Tràngol")]
        [SerializeField] RectTransform costMark;
        [SerializeField] EnergyHighlight highlight;
        [Tooltip("Lo que se mueve la barra por segundo hacia su valor (fracción de la barra)")]
        [SerializeField] float barSpeed = 2.5f;

        PlayerEnergy energy;
        float shownEnergy;

        public PlayerController Player { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        public void Bind(PlayerController player, string role)
        {
            Player = player;
            energy = player.GetComponent<PlayerEnergy>();
            roleLabel.text = role;
            shownEnergy = energy != null ? energy.Normalized : 0f;

            bool hasAbility = player.PowerShot != null && energy != null && energy.Max > 0f;
            costMark.gameObject.SetActive(hasAbility);
            if (hasAbility)
            {
                float cost = Mathf.Clamp01(player.PowerShot.cost / energy.Max);
                costMark.anchorMin = new Vector2(cost, costMark.anchorMin.y);
                costMark.anchorMax = new Vector2(cost, costMark.anchorMax.y);
            }
            highlight.Snap(IsReady());
            Refresh(0f);
        }

        public void Refresh(float deltaTime)
        {
            if (energy == null) return;
            shownEnergy = Mathf.MoveTowards(shownEnergy, energy.Normalized, barSpeed * deltaTime);
            energyFill.anchorMax = new Vector2(Mathf.Clamp01(shownEnergy), energyFill.anchorMax.y);
            highlight.SetReady(IsReady());
        }

        bool IsReady() => Player.PowerShot != null && energy != null && energy.CanSpend(Player.PowerShot.cost);
    }
}
