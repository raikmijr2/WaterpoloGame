using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Mur de Ferro delante de una portería (habilidad del portero, MurAiguaAbility). Para todos los tiros desde que se
    // activa hasta que acaba (GoalkeeperController). Se ve un muro de paneles de metal que cae desde arriba; al tocar el
    // agua salta una cortina de agua, sale una onada de verdad en el agua (shader WaterWaves, con espuma) con su
    // rociada por delante, y los jugadores de campo rivales a menos de wallPushRadius de la portería salen despedidos
    // por los aires con una esquitxada cada uno. Al acabar se hunde en el agua.
    // Los jugadores no chocan con él (si no, el portero quedaría encerrado detrás): solo para tiros
    public class WaterWall : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] PoolBounds pool;
        [Tooltip("Equipo que defiende esta portería (al que pertenece el muro)")]
        [SerializeField] TeamSide team;
        [Tooltip("Muro de metal (se mueve en vertical: cae, se queda y se hunde)")]
        [SerializeField] GameObject curtain;
        [Tooltip("Altura del muro (m): lo que baja al hundirse")]
        [SerializeField] float plateHeight = 2f;
        [Tooltip("Esquitxada al caer")]
        [SerializeField] ParticleSystem spray;
        [Tooltip("Gotas de la esquitxada al caer")]
        [SerializeField] int landingDrops = 160;
        [Tooltip("Anillo de espuma de la onada, tumbado sobre el agua")]
        [SerializeField] SpriteRenderer wave;
        [Tooltip("Rociada que viaja con la cresta de la onada (medio círculo hacia el campo)")]
        [SerializeField] ParticleSystem waveSpray;
        [SerializeField] int waveSprayDrops = 320;
        [Tooltip("Neblina de agua al caer")]
        [SerializeField] ParticleSystem mist;
        [SerializeField] int mistPuffs = 40;
        [Tooltip("Esquitxada en cada jugador empujado (espacio de mundo)")]
        [SerializeField] ParticleSystem pushSplash;
        [SerializeField] int pushSplashDrops = 45;

        static readonly int ShockA = Shader.PropertyToID("_ShockA");
        static readonly int ShockB = Shader.PropertyToID("_ShockB");

        enum Phase { Down, Falling, Up, Sinking }

        Phase phase = Phase.Down;
        float timer;       // lo que le queda levantado (para tiros)
        float phaseTime;   // tiempo dentro de la fase actual (caer, hundirse, onada)
        float waveTime = float.MaxValue;
        float restY;       // altura del muro apoyado en el agua
        Color waveColor;
        readonly List<PlayerController> players = new List<PlayerController>();

        public bool IsUp => timer > 0f;
        // Posición del muro (los tiros que la cruzan hacia la portería se paran)
        public Vector3 Position => transform.position;

        void Awake()
        {
            restY = curtain.transform.localPosition.y;
            if (wave != null)
            {
                waveColor = wave.color;
                wave.enabled = false;
            }
            curtain.SetActive(false);
            Shader.SetGlobalVector(ShockA, Vector4.zero);   // sin onada al empezar
        }

        void Start()
        {
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (p.Team != team && !p.IsGoalkeeper) players.Add(p);
        }

        public void Raise(float duration)
        {
            timer = Mathf.Max(timer, duration);
            if (phase == Phase.Falling || phase == Phase.Up) return;
            phase = Phase.Falling;
            phaseTime = 0f;
            curtain.SetActive(true);
            SetPlateY(restY + config.wallDropHeight);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (timer > 0f) timer -= dt;
            UpdateWave(dt);

            switch (phase)
            {
                case Phase.Falling:
                    phaseTime += dt;
                    // Cae acelerando, como algo pesado
                    float t = config.wallDropTime > 0f ? Mathf.Clamp01(phaseTime / config.wallDropTime) : 1f;
                    SetPlateY(restY + config.wallDropHeight * (1f - t * t));
                    if (t >= 1f) Land();
                    break;
                case Phase.Up:
                    if (timer <= 0f)
                    {
                        phase = Phase.Sinking;
                        phaseTime = 0f;
                    }
                    break;
                case Phase.Sinking:
                    phaseTime += dt;
                    float s = config.wallSinkTime > 0f ? Mathf.Clamp01(phaseTime / config.wallSinkTime) : 1f;
                    SetPlateY(restY - plateHeight * s);
                    if (s >= 1f)
                    {
                        phase = Phase.Down;
                        curtain.SetActive(false);
                    }
                    break;
            }
        }

        // Toca el agua: esquitxada, onada y empujón a los rivales del área
        void Land()
        {
            phase = timer > 0f ? Phase.Up : Phase.Sinking;
            phaseTime = 0f;
            SetPlateY(restY);
            if (spray != null) spray.Emit(landingDrops);
            if (waveSpray != null) waveSpray.Emit(waveSprayDrops);
            if (mist != null) mist.Emit(mistPuffs);
            waveTime = 0f;
            StartWaterWave();
            PushOpponents();
            GameEvents.RaiseWallLanded(gameObject);
        }

        void PushOpponents()
        {
            Vector3 goal = pool.AttackedGoalCenter(team.Opponent());
            Vector3 inward = Vector3.right * team.AttackSign();
            foreach (var p in players)
            {
                Vector3 v = WallPush.VelocityFor(p.Position, goal, inward, config.wallPushRadius, config.wallPushMargin, config.wallPushDuration);
                if (v == Vector3.zero) continue;
                p.Push(v, config.wallPushDuration);
                if (pushSplash != null)
                    pushSplash.Emit(new ParticleSystem.EmitParams { position = p.Position, applyShapeToPosition = true }, pushSplashDrops);
            }
        }

        // La onada en la superficie del agua: el shader la dibuja a partir de estos valores (mismo reloj que _Time.y)
        void StartWaterWave()
        {
            Vector3 c = transform.position;
            Shader.SetGlobalVector(ShockA, new Vector4(c.x, c.z, Time.timeSinceLevelLoad, config.wallWaveLife));
            Shader.SetGlobalVector(ShockB, new Vector4(config.wallWaveSpeed, config.wallWaveHeight, config.wallWaveWidth, config.wallWaveFoam));
        }

        // El anillo se abre hasta el radio del empujón y se desvanece
        void UpdateWave(float dt)
        {
            if (wave == null) return;
            if (waveTime >= config.wallWaveDuration)
            {
                wave.enabled = false;
                return;
            }
            waveTime += dt;
            float t = config.wallWaveDuration > 0f ? Mathf.Clamp01(waveTime / config.wallWaveDuration) : 1f;
            wave.enabled = t < 1f;
            float spriteSize = wave.sprite != null ? wave.sprite.bounds.size.x : 1f;
            float diameter = 2f * config.wallPushRadius * Mathf.Sqrt(t);   // sale rápido y frena
            wave.transform.localScale = Vector3.one * Mathf.Max(0.01f, diameter / spriteSize);
            wave.color = new Color(waveColor.r, waveColor.g, waveColor.b, waveColor.a * (1f - t));
        }

        void SetPlateY(float y)
        {
            var p = curtain.transform.localPosition;
            curtain.transform.localPosition = new Vector3(p.x, y, p.z);
        }
    }
}
