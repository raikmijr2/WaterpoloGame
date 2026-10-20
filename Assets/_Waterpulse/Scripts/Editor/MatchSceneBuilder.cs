using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Waterpulse.Abilities;
using Waterpulse.AI;
using Waterpulse.Brains;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;
using Waterpulse.UI;

namespace Waterpulse.EditorTools
{
    // Genera la escena del partido. Crea la escena desde cero cada vez (no duplica objetos)
    // y solo crea GameConfig.asset si no existe (no pisa los ajustes)
    public static class MatchSceneBuilder
    {
        public const string RootDir = "Assets/_Waterpulse";
        public const string ScenePath = RootDir + "/Scenes/Match.unity";
        public const string ConfigPath = RootDir + "/Config/GameConfig.asset";
        public const string DifficultyPath = RootDir + "/Config/Difficulty_Competidor.asset";
        public const string EnergyConfigPath = RootDir + "/Config/EnergyConfig.asset";
        public const string XutTragolPath = RootDir + "/Config/Ability_XutTragol.asset";
        public const string TorpedeBlauPath = RootDir + "/Config/Ability_TorpedeBlau.asset";
        const string MaterialDir = RootDir + "/Art/Materials";

        // Medidas puramente visuales de la escena (no afectan a la jugabilidad)
        const float WallThickness = 0.5f;
        const float WallTopAboveWater = 0.3f;
        const float WallDepth = 1.5f;
        const float EndMargin = 1.5f;      // agua detrás de cada portería
        const float SideMargin = 0.5f;     // agua fuera de las bandas
        const float PoolDepth = 2f;
        const float LineWidth = 0.08f;
        const float Line2m = 2f;
        const float Line5m = 5f;
        const float PostSize = 0.12f;
        const float NetDepth = 0.8f;
        const float PlayerHeight = 1.2f;
        const float MarkerHeight = 0.02f;
        const float SuitHeight = 0.12f;       // franja del bañador a la altura del agua

        // Efectos visuales de las habilidades (solo aspecto, no afectan a la jugabilidad)
        const float BallTrailTime = 0.35f;
        const float BallTrailWidth = 0.3f;
        const float PlayerTrailTime = 0.5f;
        const float PlayerTrailWidth = 0.7f;
        const float SplashDuration = 0.5f;
        const float SplashLifetime = 0.6f;
        const float SplashSpeed = 3f;
        const float SplashSize = 0.15f;
        const float SplashGravity = 0.6f;
        const float SplashConeAngle = 35f;
        const float SplashRadius = 0.3f;
        const short SplashParticles = 30;
        const string PhysicsDir = RootDir + "/Art/Physics";


        [MenuItem("Waterpulse/Build Match Scene", priority = 0)]
        static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            Debug.Log($"[Waterpulse] Escena generada en {ScenePath}. Pulsa Play y muévete con WASD.");
        }

        public static void Build()
        {
            EnsureFolder(RootDir + "/Scenes");
            EnsureFolder(RootDir + "/Config");
            EnsureFolder(MaterialDir);
            EnsureFolder(PhysicsDir);

            // La escena nueva se crea ANTES de cargar assets: NewScene descarga los assets no usados
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = LoadOrCreateConfig();
            var difficulty = LoadOrCreate<DifficultySO>(DifficultyPath);
            var mats = new Materials();

            BuildLight();
            var pool = BuildPool(config, mats);
            BuildGoal(config, mats, pool.transform, -1f, TeamSide.Home);
            BuildGoal(config, mats, pool.transform, 1f, TeamSide.Away);
            var ball = BuildBall(config, mats, pool);

            var abilities = new AbilitySetup
            {
                Energy = LoadOrCreate<EnergyConfigSO>(EnergyConfigPath),
                XutTragol = LoadOrCreate<AbilityDefinitionSO>(XutTragolPath, InitXutTragol),
                TorpedeBlau = LoadOrCreate<AbilityDefinitionSO>(TorpedeBlauPath, InitTorpedeBlau),
                BallTrail = ball.GetComponent<TrailRenderer>(),
            };

            // TeamAI agresivo para el rival (persigue, presiona, corta, pasa y tira); pasivo para los compañeros del humano (solo marcajes)
            BuildTeam(config, mats, ball, pool, TeamSide.Home, mats.HomeCap, mats.HomeSuit, BuildTeamAI(TeamSide.Home, ball, difficulty, false), abilities);
            BuildTeam(config, mats, ball, pool, TeamSide.Away, mats.AwayCap, mats.AwaySuit, BuildTeamAI(TeamSide.Away, ball, difficulty, true), abilities);

            var flow = BuildMatchSystems(config, ball, pool, abilities.Energy);
            BuildHumanControl(config, ball, mats);
            BuildCamera(config, pool);
            BuildHud(config, flow);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        static GameConfigSO LoadOrCreateConfig() => LoadOrCreate<GameConfigSO>(ConfigPath);

        // Solo crea el asset si no existe: nunca pisa los valores ajustados.
        // "init" pone los valores por defecto propios de ese asset (solo al crearlo)
        static T LoadOrCreate<T>(string path, System.Action<T> init = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        // Valores del GDD: Xut del Tràngol, 35 de energía, tiro muy potente que el portero solo para si lo anticipa
        static void InitXutTragol(AbilityDefinitionSO a)
        {
            a.kind = AbilityKind.PowerShot;
            a.displayName = "Xut del Tràngol";
            a.cost = 35f;
            a.power = 1.6f;
            a.goalkeeperReach = 0.6f;
        }

        // Valores del GDD: Torpede Blau, 25 de energía, acelerón de ~1 s
        static void InitTorpedeBlau(AbilityDefinitionSO a)
        {
            a.kind = AbilityKind.Boost;
            a.displayName = "Torpede Blau";
            a.cost = 25f;
            a.power = 2f;
            a.duration = 1f;
        }

        class AbilitySetup
        {
            public EnergyConfigSO Energy;
            public AbilityDefinitionSO XutTragol;
            public AbilityDefinitionSO TorpedeBlau;
            public TrailRenderer BallTrail;
        }

        static TeamAI BuildTeamAI(TeamSide team, BallController ball, DifficultySO difficulty, bool aggressive)
        {
            var go = new GameObject("TeamAI_" + team);
            var teamAI = go.AddComponent<TeamAI>();
            SetEnum(teamAI, "team", (int)team);
            SetRef(teamAI, "ball", ball);
            SetRef(teamAI, "difficulty", difficulty);
            SetFlag(teamAI, "aggressive", aggressive);
            return teamAI;
        }

        // ---------- Escena ----------

        static void BuildLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.65f);
        }

        static PoolBounds BuildPool(GameConfigSO c, Materials mats)
        {
            var root = new GameObject("Pool");
            var bounds = root.AddComponent<PoolBounds>();
            SetRef(bounds, "config", c);
            var t = root.transform;

            float waterL = c.poolLength + EndMargin * 2f;
            float waterW = c.poolWidth + SideMargin * 2f;
            Prim(PrimitiveType.Cube, "Water", t, new Vector3(0f, -0.025f, 0f), new Vector3(waterL, 0.05f, waterW), mats.Water, false, false);
            Prim(PrimitiveType.Cube, "Floor", t, new Vector3(0f, -PoolDepth, 0f), new Vector3(waterL, 0.2f, waterW), mats.Floor, false);

            // Paredes con colisión: los límites físicos coinciden con las bandas y las líneas de gol.
            // En cada línea de gol la pared se parte en dos tramos y deja libre la boca de la portería
            float wallH = WallDepth + WallTopAboveWater;
            float wallY = WallTopAboveWater - wallH * 0.5f;
            var walls = new GameObject("Walls").transform;
            walls.SetParent(t, false);
            float hx = c.poolLength * 0.5f, hz = c.poolWidth * 0.5f, w = WallThickness;
            Prim(PrimitiveType.Cube, "Wall_North", walls, new Vector3(0f, wallY, hz + w * 0.5f), new Vector3(c.poolLength + w * 2f, wallH, w), mats.Wall, true);
            Prim(PrimitiveType.Cube, "Wall_South", walls, new Vector3(0f, wallY, -hz - w * 0.5f), new Vector3(c.poolLength + w * 2f, wallH, w), mats.Wall, true);

            float mouth = c.goalWidth * 0.5f + PostSize;
            float segment = hz - mouth;
            foreach (float s in new[] { -1f, 1f })
            {
                string side = s > 0f ? "East" : "West";
                float x = (hx + w * 0.5f) * s;
                Prim(PrimitiveType.Cube, $"Wall_{side}_North", walls, new Vector3(x, wallY, mouth + segment * 0.5f), new Vector3(w, wallH, segment), mats.Wall, true);
                Prim(PrimitiveType.Cube, $"Wall_{side}_South", walls, new Vector3(x, wallY, -mouth - segment * 0.5f), new Vector3(w, wallH, segment), mats.Wall, true);
            }

            // Líneas: gol (blanca), 2 m (roja), 5 m (amarilla) y medio campo (blanca)
            var lines = new GameObject("Lines").transform;
            lines.SetParent(t, false);
            CrossLine(lines, "Center", 0f, c.poolWidth, mats.LineWhite);
            foreach (float s in new[] { -1f, 1f })
            {
                string side = s > 0f ? "East" : "West";
                CrossLine(lines, "GoalLine_" + side, hx * s, c.poolWidth, mats.LineWhite);
                CrossLine(lines, "Line2m_" + side, (hx - Line2m) * s, c.poolWidth, mats.LineRed);
                CrossLine(lines, "Line5m_" + side, (hx - Line5m) * s, c.poolWidth, mats.LineYellow);
            }
            return bounds;
        }

        static void CrossLine(Transform parent, string name, float x, float width, Material mat)
        {
            Prim(PrimitiveType.Cube, name, parent, new Vector3(x, 0.01f, 0f), new Vector3(LineWidth, 0.01f, width), mat, false, false);
        }

        // Portería: postes y red con colisión (la red cierra la boca por detrás) y trigger de gol dentro de la red
        static void BuildGoal(GameConfigSO c, Materials mats, Transform pool, float side, TeamSide defendingTeam)
        {
            var root = new GameObject(side > 0f ? "Goal_East" : "Goal_West").transform;
            root.SetParent(pool, false);
            root.localPosition = new Vector3(c.poolLength * 0.5f * side, 0f, 0f);

            float hw = c.goalWidth * 0.5f, h = c.goalHeight;
            float postH = h + WallDepth;
            float postY = h - postH * 0.5f;
            float netZ = hw + PostSize * 0.5f;
            Prim(PrimitiveType.Cube, "Post_Left", root, new Vector3(0f, postY, -netZ), new Vector3(PostSize, postH, PostSize), mats.GoalFrame, true);
            Prim(PrimitiveType.Cube, "Post_Right", root, new Vector3(0f, postY, netZ), new Vector3(PostSize, postH, PostSize), mats.GoalFrame, true);
            Prim(PrimitiveType.Cube, "Crossbar", root, new Vector3(0f, h + PostSize * 0.5f, 0f), new Vector3(PostSize, PostSize, c.goalWidth + PostSize * 2f), mats.GoalFrame, false);
            Prim(PrimitiveType.Cube, "Net_Back", root, new Vector3(NetDepth * side, postY, 0f), new Vector3(PostSize, postH, c.goalWidth + PostSize * 2f), mats.Net, true, false);
            Prim(PrimitiveType.Cube, "Net_Left", root, new Vector3(NetDepth * 0.5f * side, postY, -netZ), new Vector3(NetDepth, postH, PostSize), mats.Net, true, false);
            Prim(PrimitiveType.Cube, "Net_Right", root, new Vector3(NetDepth * 0.5f * side, postY, netZ), new Vector3(NetDepth, postH, PostSize), mats.Net, true, false);
            Prim(PrimitiveType.Cube, "Net_Top", root, new Vector3(NetDepth * 0.5f * side, h, 0f), new Vector3(NetDepth, 0.02f, c.goalWidth), mats.Net, false, false);

            // Trigger de gol: empieza a 2 radios de la línea, así la pelota cuenta cuando la ha cruzado entera
            float front = c.ballRadius * 2f;
            float back = NetDepth - PostSize * 0.5f;
            var trigger = new GameObject("GoalTrigger");
            trigger.transform.SetParent(root, false);
            trigger.transform.localPosition = new Vector3((front + back) * 0.5f * side, postY, 0f);
            var box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(back - front, postH, c.goalWidth);
            var goal = trigger.AddComponent<Goal>();
            SetEnum(goal, "defendingTeam", (int)defendingTeam);
        }

        static BallController BuildBall(GameConfigSO c, Materials mats, PoolBounds pool)
        {
            var go = Prim(PrimitiveType.Sphere, "Ball", null, pool.Center, Vector3.one * c.ballRadius * 2f, mats.Ball, true);
            go.GetComponent<SphereCollider>().sharedMaterial = LoadOrCreateBallPhysics(c);
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = c.ballMass;
            var ball = go.AddComponent<BallController>();
            SetRef(ball, "config", c);
            SetRef(ball, "pool", pool);

            // Estela del Xut del Tràngol (apagada hasta que se usa la habilidad)
            AddTrail(go, mats.Trail, BallTrailTime, BallTrailWidth, new Color(1f, 0.6f, 0.1f));
            return ball;
        }

        static TrailRenderer AddTrail(GameObject go, Material material, float time, float width, Color color)
        {
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = time;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.emitting = false;
            return trail;
        }

        // Salpicadura: ráfaga de gotas hacia arriba que se lanza al activar una habilidad
        static ParticleSystem AddSplash(Transform parent, Material material)
        {
            var go = new GameObject("Splash");
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // el cono emite hacia arriba
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = SplashDuration;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = SplashLifetime;
            main.startSpeed = SplashSpeed;
            main.startSize = SplashSize;
            main.startColor = new Color(0.75f, 0.9f, 1f, 0.9f);
            main.gravityModifier = SplashGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, SplashParticles) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = SplashConeAngle;
            shape.radius = SplashRadius;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return ps;
        }

        // Un equipo: Atacant, Organitzador y Defensor con AIBrain, y el portero con GoalkeeperController
        static void BuildTeam(GameConfigSO c, Materials mats, BallController ball, PoolBounds pool, TeamSide team,
                              Material cap, Material suit, TeamAI teamAI, AbilitySetup abilities)
        {
            var root = new GameObject("Team_" + team).transform;
            foreach (var role in new[] { PlayerRole.Attacker, PlayerRole.Playmaker, PlayerRole.Defender, PlayerRole.Goalkeeper })
            {
                // Aparecen en su posición de saque (la misma que usa MatchFlow)
                Vector3 position = KickoffLayout.Position(role, team, pool.Center, pool.HalfLength, c);
                bool keeper = role == PlayerRole.Goalkeeper;
                var player = BuildPlayer(c, mats, ball, pool, root, team, role, position, KickoffLayout.Facing(team),
                                         keeper ? mats.GoalkeeperCap : cap, suit, keeper ? null : teamAI);
                if (!keeper) AddAbility(player, role, abilities, mats);   // los porteros no tienen habilidad en el prototipo
            }
        }

        static GameObject BuildPlayer(GameConfigSO c, Materials mats, BallController ball, PoolBounds pool, Transform parent, TeamSide team,
                                PlayerRole role, Vector3 position, Vector3 facing, Material capMaterial, Material suitMaterial, TeamAI teamAI)
        {
            var go = new GameObject($"{team}_{role}");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing));

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = c.playerRadius;
            capsule.height = PlayerHeight;
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = c.playerMass;
            var controller = go.AddComponent<PlayerController>();
            SetRef(controller, "config", c);
            SetRef(controller, "ball", ball);
            SetEnum(controller, "team", (int)team);
            SetEnum(controller, "role", (int)role);

            // Cerebro por defecto: el portero tiene el suyo; los de campo, IA de colocación
            if (role == PlayerRole.Goalkeeper)
            {
                var keeper = go.AddComponent<GoalkeeperController>();
                SetRef(keeper, "config", c);
                SetRef(keeper, "ball", ball);
                SetRef(keeper, "pool", pool);
            }
            else
            {
                var brain = go.AddComponent<AIBrain>();
                SetRef(brain, "config", c);
                SetRef(brain, "ball", ball);
                SetRef(brain, "pool", pool);
                SetRef(brain, "teamAI", teamAI);
            }

            // Visual: cuerpo medio sumergido, bañador del equipo, gorro y "nariz" para ver hacia dónde mira
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            float d = c.playerRadius * 2f;
            Prim(PrimitiveType.Capsule, "Body", visual, Vector3.zero, new Vector3(d, PlayerHeight * 0.5f, d), mats.Body, false);
            Prim(PrimitiveType.Cylinder, "Suit", visual, Vector3.zero, new Vector3(d * 1.05f, SuitHeight, d * 1.05f), suitMaterial, false);
            Prim(PrimitiveType.Sphere, "Cap", visual, new Vector3(0f, PlayerHeight * 0.5f, 0f), Vector3.one * d * 0.8f, capMaterial, false);
            Prim(PrimitiveType.Cube, "Nose", visual, new Vector3(0f, PlayerHeight * 0.35f, c.playerRadius), Vector3.one * d * 0.3f, mats.Nose, false);
            return go;
        }

        // Energía y habilidad de un jugador de campo: Atacant → Xut del Tràngol; Organitzador y Defensor → Torpede Blau
        static void AddAbility(GameObject go, PlayerRole role, AbilitySetup setup, Materials mats)
        {
            var energy = go.AddComponent<PlayerEnergy>();
            SetRef(energy, "config", setup.Energy);

            var splash = AddSplash(go.transform, mats.Splash);
            var controller = go.GetComponent<PlayerController>();
            if (role == PlayerRole.Attacker)
            {
                SetRef(controller, "ability", setup.XutTragol);
                var xut = go.AddComponent<XutTragolAbility>();
                SetRef(xut, "splash", splash);
                SetRef(xut, "ballTrail", setup.BallTrail);
            }
            else
            {
                SetRef(controller, "ability", setup.TorpedeBlau);
                var torpede = go.AddComponent<TorpedeBlauAbility>();
                SetRef(torpede, "splash", splash);
                SetRef(torpede, "trail", AddTrail(go, mats.Trail, PlayerTrailTime, PlayerTrailWidth, new Color(0.3f, 0.7f, 1f)));
            }
        }

        static MatchFlow BuildMatchSystems(GameConfigSO c, BallController ball, PoolBounds pool, EnergyConfigSO energy)
        {
            var go = new GameObject("Match");
            var flow = go.AddComponent<MatchFlow>();
            SetRef(flow, "config", c);
            SetRef(flow, "ball", ball);
            SetRef(flow, "pool", pool);
            var rewards = go.AddComponent<EnergyRewards>();
            SetRef(rewards, "config", energy);
            return flow;
        }

        // Control humano estilo FIFA: un único HumanBrain que ControlSwitcher asigna al jugador con la pelota
        static void BuildHumanControl(GameConfigSO c, BallController ball, Materials mats)
        {
            var go = new GameObject("HumanControl");
            var human = go.AddComponent<HumanBrain>();
            var switcher = go.AddComponent<ControlSwitcher>();
            var marker = Prim(PrimitiveType.Cylinder, "ControlMarker", null, new Vector3(0f, MarkerHeight, 0f), new Vector3(1.4f, 0.01f, 1.4f), mats.Marker, false, false);
            SetRef(switcher, "config", c);
            SetRef(switcher, "human", human);
            SetRef(switcher, "ball", ball);
            SetRef(switcher, "marker", marker.transform);
            SetEnum(switcher, "humanTeam", (int)TeamSide.Home);
        }

        // HUD básico: marcador con tiempo, mensaje central y pantalla final. El HUD completo es de la Fase 8
        static void BuildHud(GameConfigSO c, MatchFlow flow)
        {
            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            var score = Label(root, "Scoreboard", 48, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1200f, 80f));
            var message = Label(root, "Message", 80, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1400f, 140f));
            message.text = string.Empty;

            var scoreboard = canvasGo.AddComponent<ScoreboardView>();
            SetRef(scoreboard, "config", c);
            SetRef(scoreboard, "match", flow);
            SetRef(scoreboard, "scoreLabel", score);
            SetRef(scoreboard, "messageLabel", message);

            // Nombre de la habilidad al activarse y energía del jugador controlado (provisional hasta la Fase 8)
            var announce = Label(root, "AbilityName", 72, new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(1400f, 120f));
            announce.color = new Color(1f, 0.85f, 0.2f);
            var announcer = canvasGo.AddComponent<AbilityAnnouncer>();
            SetRef(announcer, "config", c);
            SetRef(announcer, "label", announce);
            var energyText = Label(root, "Energy", 40, Vector2.zero, new Vector2(220f, 50f), new Vector2(400f, 60f));
            var energyReadout = canvasGo.AddComponent<EnergyReadout>();
            SetRef(energyReadout, "label", energyText);

            // Pantalla final (se activa en Results)
            var panel = new GameObject("ResultsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var result = Label(panel.transform, "Result", 72, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1400f, 120f));
            var winner = Label(panel.transform, "Winner", 56, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 100f));
            var button = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(panel.transform, false);
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.32f);
            buttonRect.sizeDelta = new Vector2(520f, 110f);
            button.GetComponent<Image>().color = new Color(0.1f, 0.3f, 0.95f);
            var buttonLabel = Label(button.transform, "Label", 44, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 110f));
            buttonLabel.text = "Tornar a jugar";

            var results = canvasGo.AddComponent<ResultsScreen>();
            SetRef(results, "config", c);
            SetRef(results, "match", flow);
            SetRef(results, "panel", panel);
            SetRef(results, "resultLabel", result);
            SetRef(results, "winnerLabel", winner);
            SetRef(results, "restartButton", button.GetComponent<Button>());

            // El botón necesita un EventSystem que lea el Input System nuevo
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        static Text Label(Transform parent, string name, int size, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, anchor.y >= 1f ? 1f : 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }

        static PhysicsMaterial LoadOrCreateBallPhysics(GameConfigSO c)
        {
            string path = PhysicsDir + "/Ball.physicMaterial";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (mat == null)
            {
                mat = new PhysicsMaterial("Ball");
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.bounciness = c.ballBounciness;
            mat.bounceCombine = PhysicsMaterialCombine.Maximum;
            mat.dynamicFriction = 0f;
            mat.staticFriction = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void BuildCamera(GameConfigSO c, PoolBounds pool)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.2f);
            cam.fieldOfView = c.cameraFieldOfView;
            go.AddComponent<AudioListener>();
            var matchCamera = go.AddComponent<MatchCamera>();
            SetRef(matchCamera, "config", c);
            SetRef(matchCamera, "pool", pool);
            matchCamera.Fit();
        }

        // ---------- Utilidades ----------

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider, bool castShadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (!castShadows) r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static void SetFlag(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Waterpulse] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Waterpulse] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Waterpulse] Campo '{field}' no encontrado en {target.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (prop.objectReferenceValue != value)
                Debug.LogError($"[Waterpulse] No se pudo asignar '{field}' en {target.GetType().Name}");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // Materiales URP generados (se actualizan en cada ejecución, no se duplican)
        class Materials
        {
            public readonly Material Water, Floor, Wall, LineWhite, LineRed, LineYellow, GoalFrame, Net, Body, Nose;
            public readonly Material HomeCap, AwayCap, GoalkeeperCap, HomeSuit, AwaySuit, Ball, Marker, Trail, Splash;

            public Materials()
            {
                Water = Lit("Water", new Color(0.1f, 0.5f, 0.85f, 0.6f), true);
                Floor = Lit("PoolFloor", new Color(0.55f, 0.82f, 0.92f));
                Wall = Lit("PoolWall", new Color(0.88f, 0.9f, 0.92f));
                LineWhite = Lit("LineWhite", Color.white);
                LineRed = Lit("LineRed", new Color(0.9f, 0.15f, 0.15f));
                LineYellow = Lit("LineYellow", new Color(1f, 0.85f, 0.1f));
                GoalFrame = Lit("GoalFrame", Color.white);
                Net = Lit("GoalNet", new Color(1f, 1f, 1f, 0.3f), true);
                Body = Lit("PlayerBody", new Color(0.95f, 0.8f, 0.65f));
                HomeCap = Lit("HomeCap", new Color(0.1f, 0.3f, 0.95f));
                AwayCap = Lit("AwayCap", new Color(0.97f, 0.97f, 0.97f));
                HomeSuit = Lit("HomeSuit", new Color(0.05f, 0.18f, 0.6f));
                AwaySuit = Lit("AwaySuit", new Color(0.85f, 0.85f, 0.85f));
                GoalkeeperCap = Lit("GoalkeeperCap", new Color(0.9f, 0.1f, 0.1f));
                Ball = Lit("Ball", new Color(1f, 0.85f, 0.1f));
                Marker = Lit("ControlMarker", new Color(0.3f, 1f, 0.4f));
                Trail = Unlit("Trail", "Sprites/Default");
                Splash = Unlit("Splash", "Universal Render Pipeline/Particles/Unlit");
                Nose = Lit("PlayerNose", new Color(0.1f, 0.1f, 0.1f));
            }

            static Material Lit(string name, Color color, bool transparent = false)
            {
                string path = $"{MaterialDir}/{name}.mat";
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(shader);
                    AssetDatabase.CreateAsset(m, path);
                }
                m.SetColor("_BaseColor", color);
                if (transparent)
                {
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_ZWrite", 0f);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.renderQueue = (int)RenderQueue.Transparent;
                    m.SetShaderPassEnabled("ShadowCaster", false);
                }
                EditorUtility.SetDirty(m);
                return m;
            }

            // Material sin iluminación que respeta el color por vértice (estelas y partículas)
            static Material Unlit(string name, string shaderName)
            {
                string path = $"{MaterialDir}/{name}.mat";
                var shader = Shader.Find(shaderName) ?? Shader.Find("Sprites/Default");
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(shader);
                    AssetDatabase.CreateAsset(m, path);
                }
                else if (m.shader != shader)
                {
                    m.shader = shader;
                }
                EditorUtility.SetDirty(m);
                return m;
            }
        }
    }
}
