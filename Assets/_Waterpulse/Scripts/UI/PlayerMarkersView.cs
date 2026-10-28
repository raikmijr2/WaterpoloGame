using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Marcas sobre los jugadores, en pantalla:
    // - Una mini placa (PlayerPlate) sobre cada jugador de campo del humano, con su energía y si ya puede usar la habilidad.
    // - Una flecha sobre quien lleva la pelota: blanca si es del equipo humano, del color del rival si no.
    // - Esquinas alrededor del compañero al que iría el pase ahora mismo, o, defendiendo, del que cogerías con Q
    //   (con la tecla al lado). Su placa crece para leerla mientras apuntas.
    // Usa los mismos cálculos que el juego (BallController.ReceiverFor y SwitchTargeting), así que no se equivoca
    public class PlayerMarkersView : MonoBehaviour
    {
        [SerializeField] BallController ball;
        [SerializeField] Camera viewCamera;
        [SerializeField] TeamSide humanTeam = TeamSide.Home;
        [Tooltip("Placa de plantilla (inactiva): se copia una por jugador")]
        [SerializeField] PlayerPlate plateTemplate;
        [SerializeField] RectTransform ballArrow;
        [SerializeField] Image ballArrowImage;
        [SerializeField] Color ownBallColor = Color.white;
        [SerializeField] Color rivalBallColor = new Color(1f, 0.42f, 0.35f);
        [Tooltip("Esquinas alrededor del destino del pase o del siguiente jugador para Q")]
        [SerializeField] RectTransform targetBrackets;
        [Tooltip("Tecla de cambio de jugador, junto a las esquinas (solo defendiendo)")]
        [SerializeField] GameObject switchKey;
        [Tooltip("Altura sobre el agua a la que va la placa (m)")]
        [SerializeField] float plateHeight = 1.7f;
        [Tooltip("Altura sobre el agua del centro de las esquinas (m)")]
        [SerializeField] float bracketsHeight = 0.4f;
        [Tooltip("Separación entre la placa y la flecha de la pelota (px de referencia)")]
        [SerializeField] float arrowGap = 30f;
        [Tooltip("Tamaño de la placa del destino del pase o del cambio (1 = normal)")]
        [SerializeField] float targetPlateScale = 1.28f;
        [Tooltip("Abreviatura de cada rol en el orden de PlayerRole: Attacker, Playmaker, Defender, Goalkeeper")]
        [SerializeField] string[] roleShortNames = { "ATA", "ORG", "DEF", "POR" };

        readonly List<PlayerPlate> plates = new List<PlayerPlate>();
        readonly List<PlayerController> fieldTeam = new List<PlayerController>();
        readonly List<Vector3> positions = new List<Vector3>();
        Canvas canvas;
        PlayerController controlled;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            plateTemplate.gameObject.SetActive(false);
            ballArrow.gameObject.SetActive(false);
            targetBrackets.gameObject.SetActive(false);
        }

        void OnEnable() => GameEvents.ControlledPlayerChanged += OnControlledChanged;
        void OnDisable() => GameEvents.ControlledPlayerChanged -= OnControlledChanged;

        void OnControlledChanged(GameObject go) => controlled = go != null ? go.GetComponent<PlayerController>() : null;

        void Start()
        {
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (p.Team == humanTeam && !p.IsGoalkeeper) fieldTeam.Add(p);
            fieldTeam.Sort((a, b) => string.CompareOrdinal(a.name, b.name));   // mismo orden que ControlSwitcher

            foreach (var p in fieldTeam)
            {
                var plate = Instantiate(plateTemplate, plateTemplate.transform.parent);
                plate.name = "Plate_" + p.name;
                plate.transform.SetSiblingIndex(plateTemplate.transform.GetSiblingIndex() + 1);   // debajo de la flecha y las esquinas
                plate.gameObject.SetActive(true);
                int role = (int)p.Role;
                plate.Bind(p, role >= 0 && role < roleShortNames.Length ? roleShortNames[role] : p.Role.ToString());
                plates.Add(plate);
            }
        }

        void LateUpdate()
        {
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            var target = HighlightedPlayer(out bool isSwitch);

            foreach (var plate in plates)
            {
                plate.Refresh(Time.deltaTime);
                bool visible = ToScreen(plate.Player.Position, plateHeight, out Vector3 screen);
                if (plate.gameObject.activeSelf != visible) plate.gameObject.SetActive(visible);
                if (!visible) continue;
                plate.Rect.position = screen;
                plate.Rect.localScale = Vector3.one * (plate.Player == target ? targetPlateScale : 1f);
            }

            PlaceBallArrow(scale);
            PlaceBrackets(target, isSwitch);
        }

        // Atacando con la pelota: el receptor del pase. Defendiendo: el siguiente jugador para Q. Si no, nadie
        PlayerController HighlightedPlayer(out bool isSwitch)
        {
            isSwitch = false;
            if (controlled == null) return null;
            if (controlled.HasBall) return controlled.IsBusy ? null : ball.ReceiverFor(controlled, controlled.AimDirection());

            var holder = ball.Holder;
            if (holder != null && holder.Team == humanTeam) return null;   // la tiene el portero propio
            positions.Clear();
            foreach (var p in fieldTeam) positions.Add(p.Position);
            int index = SwitchTargeting.Nearest(ball.Position, positions, fieldTeam.IndexOf(controlled));
            isSwitch = index >= 0;
            return index >= 0 ? fieldTeam[index] : null;
        }

        void PlaceBallArrow(float scale)
        {
            var holder = ball.Holder;
            Vector3 screen = default;
            bool show = holder != null && ToScreen(holder.Position, plateHeight, out screen);
            if (ballArrow.gameObject.activeSelf != show) ballArrow.gameObject.SetActive(show);
            if (!show) return;

            bool own = holder.Team == humanTeam;
            ballArrowImage.color = own ? ownBallColor : rivalBallColor;
            // Sobre la placa en los propios (más arriba si la placa está ampliada); los rivales no llevan placa
            var plate = own ? plates.Find(p => p.Player == holder) : null;
            float lift = plate != null ? arrowGap * plate.Rect.localScale.y : 0f;
            ballArrow.position = screen + Vector3.up * lift * scale;
        }

        void PlaceBrackets(PlayerController target, bool isSwitch)
        {
            Vector3 screen = default;
            bool show = target != null && ToScreen(target.Position, bracketsHeight, out screen);
            if (targetBrackets.gameObject.activeSelf != show) targetBrackets.gameObject.SetActive(show);
            if (!show) return;
            targetBrackets.position = screen;
            if (switchKey.activeSelf != isSwitch) switchKey.SetActive(isSwitch);
        }

        // Punto de pantalla a "height" metros sobre el jugador; false si queda detrás de la cámara
        bool ToScreen(Vector3 position, float height, out Vector3 screen)
        {
            screen = viewCamera.WorldToScreenPoint(position + Vector3.up * height);
            if (screen.z <= 0f) return false;
            screen.z = 0f;
            return true;
        }
    }
}
