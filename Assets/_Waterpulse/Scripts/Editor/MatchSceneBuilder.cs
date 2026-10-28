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
using Waterpulse.Audio;
using Waterpulse.Brains;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;
using Waterpulse.UI;
using static Waterpulse.EditorTools.SharedPages;
using static Waterpulse.EditorTools.UiFactory;

namespace Waterpulse.EditorTools
{
    // Genera la escena del partido. Crea la escena desde cero cada vez (no duplica objetos)
    // y solo crea GameConfig.asset si no existe (no pisa los ajustes).
    // La piscina, las porterías, la luz y las utilidades son internal: el menú (MenuSceneBuilder) las reutiliza de fondo
    public static class MatchSceneBuilder
    {
        public const string RootDir = "Assets/_Waterpulse";
        public const string ScenePath = RootDir + "/Scenes/Match.unity";
        public const string ConfigPath = RootDir + "/Config/GameConfig.asset";
        public const string DifficultyPath = RootDir + "/Config/Difficulty_Competidor.asset";
        public const string EnergyConfigPath = RootDir + "/Config/EnergyConfig.asset";
        public const string XutTragolPath = RootDir + "/Config/Ability_XutTragol.asset";
        public const string TorpedeBlauPath = RootDir + "/Config/Ability_TorpedeBlau.asset";
        public const string ScoringConfigPath = RootDir + "/Config/ScoringConfig.asset";
        public const string UsersConfigPath = RootDir + "/Config/UsersConfig.asset";
        public const string DefaultPlayerModelPath = RootDir + "/Prefabs/Waterpolo_TreadingWater.prefab";
        internal const string MaterialDir = RootDir + "/Art/Materials";

        // Medidas puramente visuales de la escena (no afectan a la jugabilidad)
        const float WallThickness = 0.5f;
        const float WallTopAboveWater = 0.3f;
        const float WallDepth = 1.5f;
        const float PoolDepth = 2f;
        const float LineWidth = 0.08f;
        const float Line2m = 2f;
        const float Line5m = 5f;
        const float PostSize = 0.12f;
        const float NetDepth = 0.8f;
        const float PlayerHeight = 1.2f;
        // Arcos en el agua bajo el jugador controlado, con la punta hacia donde mira (como en el FIFA)
        const float MarkerHeight = 0.03f;       // justo encima del agua (m)
        const float MarkerDiameter = 1.7f;      // ancho del sprite de los arcos (m)
        const int MarkerSortingOrder = 10;      // se dibuja después del agua (los dos son transparentes)
        // HUD: marcas sobre los jugadores (PlayerMarkersView), en px de referencia (1920×1080)
        static readonly Vector2 PlateSize = new Vector2(124f, 34f);
        static readonly Vector2 PlateRoleSize = new Vector2(48f, 34f);
        static readonly Vector2 PlateBarSize = new Vector2(64f, 6f);
        const float PlateCostMarkWidth = 2f;
        const float PlateGlowSpread = 4f;       // lo que sobresale el resplandor de la barra con la habilidad lista
        const float PanelGlowSpread = 5f;
        const float BallArrowSize = 30f;
        const float BracketsSize = 150f;        // lado del cuadrado que forman las cuatro esquinas
        const float BracketCornerSize = 26f;
        const float SwitchKeySize = 34f;
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

        // Solo para probar: la misma escena sin el equipo visitante (ni jugadores de campo ni portero).
        // Build Match Scene la vuelve a dejar normal
        [MenuItem("Waterpulse/Build Match Scene (sin rivales)", priority = 1)]
        static void BuildWithoutRivalsFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(false);
            Debug.Log($"[Waterpulse] Escena SIN RIVALES generada en {ScenePath}. Para el partido normal, Build Match Scene.");
        }

        public static void Build() => Build(true);

        public static void Build(bool withRivals)
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
            UiSpriteGenerator.EnsureSprites();   // formas de la interfaz, antes de montar el HUD

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
            var awayAI = BuildTeamAI(TeamSide.Away, ball, difficulty, true);
            if (withRivals) BuildTeam(config, mats, ball, pool, TeamSide.Away, mats.AwayCap, mats.AwaySuit, awayAI, abilities);

            var flow = BuildMatchSystems(config, ball, pool, abilities.Energy, LoadOrCreate<ScoringConfigSO>(ScoringConfigPath),
                                         LoadOrCreate<UsersConfigSO>(UsersConfigPath), out var scoring, out var pause);
            BuildHumanControl(config, ball);
            var cam = BuildCamera(config, pool, ball);
            BuildHud(config, LoadOrCreate<MenuConfigSO>(MenuSceneBuilder.ConfigPath), flow, scoring, pause, cam, ball);
            BuildAudio(SoundGenerator.EnsureAudio(abilities.XutTragol, abilities.TorpedeBlau), AudioScene.Match);

            EditorSceneManager.SaveScene(scene, ScenePath);
            MenuSceneBuilder.RegisterBuildScenes();
            AssetDatabase.SaveAssets();
        }

        static GameConfigSO LoadOrCreateConfig() => LoadOrCreate<GameConfigSO>(ConfigPath);

        // Solo crea el asset si no existe: nunca pisa los valores ajustados.
        // "init" pone los valores por defecto propios de ese asset (solo al crearlo)
        internal static T LoadOrCreate<T>(string path, System.Action<T> init = null) where T : ScriptableObject
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

        internal static void BuildLight()
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

        internal static PoolBounds BuildPool(GameConfigSO c, Materials mats)
        {
            var root = new GameObject("Pool");
            var bounds = root.AddComponent<PoolBounds>();
            SetRef(bounds, "config", c);
            var t = root.transform;

            // El agua (y el fondo) ocupa justo el campo, de pared a pared y de línea de gol a línea de gol: no sobresale.
            // Las redes, que quedan 0,8 m detrás de la línea, quedan fuera del agua (decidido por el usuario)
            Prim(PrimitiveType.Cube, "Water", t, new Vector3(0f, -0.025f, 0f), new Vector3(c.poolLength, 0.05f, c.poolWidth), mats.Water, false, false);
            Prim(PrimitiveType.Cube, "Floor", t, new Vector3(0f, -PoolDepth, 0f), new Vector3(c.poolLength, 0.2f, c.poolWidth), mats.Floor, false);

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
        internal static void BuildGoal(GameConfigSO c, Materials mats, Transform pool, float side, TeamSide defendingTeam)
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

            // La esfera que se ve es una hija: así puede ir en la mano del modelo sin mover la pelota física
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(go.GetComponent<MeshFilter>());
            var visual = Prim(PrimitiveType.Sphere, "Visual", go.transform, Vector3.zero, Vector3.one, mats.Ball, false);
            var ballVisual = visual.AddComponent<BallVisual>();
            SetRef(ballVisual, "config", c);
            SetRef(ballVisual, "ball", ball);

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
                if (!keeper) AddAbility(c, player, abilities, mats);   // los porteros no tienen habilidad en el prototipo
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

            // Visual: los jugadores de campo locales llevan el modelo animado (si existe); el resto, cápsulas
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var model = team == TeamSide.Home && role != PlayerRole.Goalkeeper ? PlayerModel(c) : null;
            if (model != null)
            {
                AddPlayerModel(c, model, visual, go.GetComponent<PlayerController>());
                return go;
            }

            // Cápsula: cuerpo medio sumergido, bañador del equipo, gorro y "nariz" para ver hacia dónde mira
            float d = c.playerRadius * 2f;
            Prim(PrimitiveType.Capsule, "Body", visual, Vector3.zero, new Vector3(d, PlayerHeight * 0.5f, d), mats.Body, false);
            Prim(PrimitiveType.Cylinder, "Suit", visual, Vector3.zero, new Vector3(d * 1.05f, SuitHeight, d * 1.05f), suitMaterial, false);
            Prim(PrimitiveType.Sphere, "Cap", visual, new Vector3(0f, PlayerHeight * 0.5f, 0f), Vector3.one * d * 0.8f, capMaterial, false);
            Prim(PrimitiveType.Cube, "Nose", visual, new Vector3(0f, PlayerHeight * 0.35f, c.playerRadius), Vector3.one * d * 0.3f, mats.Nose, false);
            return go;
        }

        static GameObject PlayerModel(GameConfigSO c) =>
            c.homePlayerModel != null ? c.homePlayerModel : AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPlayerModelPath);

        // Modelo animado dentro de "Visual" (así el saltito del pase y la subida del tiro también lo mueven)
        // y PlayerAnimator junto a su Animator para los parámetros y el evento ReleaseBall
        static void AddPlayerModel(GameConfigSO c, GameObject prefab, Transform visual, PlayerController controller)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, visual);
            model.transform.localPosition = c.playerModelOffset;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * c.playerModelScale;

            // El jugador ya tiene su Rigidbody y su collider: los del modelo no deben moverse ni chocar
            foreach (var body in model.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
            foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;

            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"[Waterpulse] El modelo {prefab.name} no tiene Animator: no se animará");
                return;
            }
            animator.applyRootMotion = false;
            var playerAnimator = animator.gameObject.AddComponent<PlayerAnimator>();
            SetRef(playerAnimator, "player", controller);
        }

        // Estamina, energía y las dos habilidades de un jugador de campo
        static void AddAbility(GameConfigSO c, GameObject go, AbilitySetup setup, Materials mats)
        {
            var stamina = go.AddComponent<PlayerStamina>();
            SetRef(stamina, "config", c);
            var energy = go.AddComponent<PlayerEnergy>();
            SetRef(energy, "config", setup.Energy);

            // Todos los jugadores de campo tienen las dos habilidades con la tecla L (decidido por el usuario):
            // con la pelota, el Xut del Tràngol; sin ella, el Torpede Blau
            var splash = AddSplash(go.transform, mats.Splash);
            var controller = go.GetComponent<PlayerController>();
            SetRef(controller, "powerShot", setup.XutTragol);
            SetRef(controller, "boost", setup.TorpedeBlau);
            var xut = go.AddComponent<XutTragolAbility>();
            SetRef(xut, "splash", splash);
            SetRef(xut, "ballTrail", setup.BallTrail);
            var torpede = go.AddComponent<TorpedeBlauAbility>();
            SetRef(torpede, "splash", splash);
            SetRef(torpede, "trail", AddTrail(go, mats.Trail, PlayerTrailTime, PlayerTrailWidth, new Color(0.3f, 0.7f, 1f)));
        }

        static MatchFlow BuildMatchSystems(GameConfigSO c, BallController ball, PoolBounds pool, EnergyConfigSO energy,
                                           ScoringConfigSO scoringConfig, UsersConfigSO usersConfig, out MatchScoring scoring, out MatchPause pause)
        {
            var go = new GameObject("Match");
            var flow = go.AddComponent<MatchFlow>();
            SetRef(flow, "config", c);
            SetRef(flow, "ball", ball);
            SetRef(flow, "pool", pool);
            var rewards = go.AddComponent<EnergyRewards>();
            SetRef(rewards, "config", energy);
            scoring = go.AddComponent<MatchScoring>();
            SetRef(scoring, "config", scoringConfig);
            SetRef(scoring, "users", usersConfig);
            SetRef(scoring, "ball", ball);
            pause = go.AddComponent<MatchPause>();
            SetRef(pause, "config", c);
            SetRef(pause, "match", flow);
            return flow;
        }

        // Sonido de la escena: música, público (en el partido), efectos e interfaz. Lo usa también el menú
        internal static void BuildAudio(AudioConfigSO audio, AudioScene scene)
        {
            var director = new GameObject("Audio").AddComponent<AudioDirector>();
            SetRef(director, "config", audio);
            SetEnum(director, "scene", (int)scene);
        }

        // Control humano estilo FIFA: un único HumanBrain que ControlSwitcher asigna al jugador con la pelota
        static void BuildHumanControl(GameConfigSO c, BallController ball)
        {
            var go = new GameObject("HumanControl");
            var human = go.AddComponent<HumanBrain>();
            SetRef(human, "config", c);
            var switcher = go.AddComponent<ControlSwitcher>();
            // Raíz que sigue al jugador y gira con él; el sprite, tumbado sobre el agua con la punta hacia delante
            var marker = new GameObject("ControlMarker");
            marker.transform.position = new Vector3(0f, MarkerHeight, 0f);
            var arcs = new GameObject("Arcs", typeof(SpriteRenderer));
            arcs.transform.SetParent(marker.transform, false);
            arcs.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var arcsRenderer = arcs.GetComponent<SpriteRenderer>();
            arcsRenderer.sprite = UiSpriteGenerator.Load(UiSpriteGenerator.ControlArcs);
            arcsRenderer.color = LocalColor;
            arcsRenderer.sortingOrder = MarkerSortingOrder;
            arcsRenderer.shadowCastingMode = ShadowCastingMode.Off;
            if (arcsRenderer.sprite != null)
                arcs.transform.localScale = Vector3.one * (MarkerDiameter / arcsRenderer.sprite.bounds.size.x);
            SetRef(switcher, "config", c);
            SetRef(switcher, "human", human);
            SetRef(switcher, "ball", ball);
            SetRef(switcher, "marker", marker.transform);
            SetEnum(switcher, "humanTeam", (int)TeamSide.Home);
        }

        // HUD de retransmisión: marcas sobre los jugadores, marcador, bàners de avisos y de habilidad, panel del jugador,
        // barra de tiro, botón de pausa, resumen final y menú de pausa
        static void BuildHud(GameConfigSO c, MenuConfigSO menuConfig, MatchFlow flow, MatchScoring scoring, MatchPause pause, Camera cam, BallController ball)
        {
            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            BuildPlayerMarkers(canvasGo, cam, ball);   // el primero: queda debajo de los paneles
            BuildScoreboard(canvasGo, c, flow);
            BuildAbilityBanner(canvasGo, c);
            BuildPlayerPanel(canvasGo);

            // Barra de tiro al lado del jugador del humano mientras carga: relleno blanco y zona verde encima
            var meterBar = Box(root, "ShotMeter", Vector2.zero, new Vector2(30f, 190f), false);
            var meterFill = StretchedImage(meterBar, "Fill", new Color(1f, 1f, 1f, 0.9f));
            meterFill.rectTransform.anchorMax = new Vector2(1f, 0f);
            var meterGreen = StretchedImage(meterBar, "Green", new Color(Volt.r, Volt.g, Volt.b, 0.85f));
            meterBar.gameObject.SetActive(false);
            var meterView = canvasGo.AddComponent<ShotMeterView>();
            SetRef(meterView, "ball", ball);
            SetRef(meterView, "viewCamera", cam);
            SetEnum(meterView, "humanTeam", (int)TeamSide.Home);
            SetRef(meterView, "bar", meterBar);
            SetRef(meterView, "greenZone", meterGreen.rectTransform);
            SetRef(meterView, "fill", meterFill.rectTransform);

            // Botón de pausa para ratón y táctil. Sin navegación: si quedara seleccionado, el A del mando (pase) lo pulsaría
            var pauseButton = StyledButton<Button>(root, "PauseButton", "II", 44, Vector2.one, new Vector2(-70f, -60f), new Vector2(90f, 90f));
            pauseButton.navigation = new Navigation { mode = Navigation.Mode.None };

            BuildResults(canvasGo, c, menuConfig, flow, scoring);
            BuildPauseMenu(canvasGo, menuConfig, flow, pause, pauseButton);   // la última: se dibuja encima de todo

            // Los botones necesitan un EventSystem que lea el Input System nuevo
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // Marcas sobre los jugadores (PlayerMarkersView): plantilla de la mini placa (rol y energía con la marca del coste de
        // la habilidad, que se resalta un poco cuando está lista), flecha sobre quien lleva la pelota y cuatro esquinas
        // alrededor del destino del pase o del siguiente jugador para Q, con la tecla al lado
        static void BuildPlayerMarkers(GameObject canvasGo, Camera cam, BallController ball)
        {
            var layer = StretchedGroup(canvasGo.transform, "PlayerMarkers").transform;
            var left = new Vector2(0f, 0.5f);

            var plate = Box(layer, "PlateTemplate", Vector2.zero, PlateSize, false);
            var role = Label(plate, "Role", 22, left, new Vector2(PlateRoleSize.x * 0.5f, 0f), PlateRoleSize);
            float barX = PlateRoleSize.x + PlateBarSize.x * 0.5f;
            var energyFill = Bar(plate, "EnergyBar", left, new Vector2(barX, 0f), PlateBarSize, EnergyIdleColor);
            var costMark = CostMark(energyFill, PlateCostMarkWidth, PlateBarSize.y * 0.5f);

            var plateView = plate.gameObject.AddComponent<PlayerPlate>();
            SetRef(plateView, "roleLabel", role);
            SetRef(plateView, "energyFill", energyFill);
            SetRef(plateView, "costMark", costMark);
            SetRef(plateView, "highlight", Highlight(energyFill, PlateGlowSpread));
            plate.gameObject.SetActive(false);

            var arrow = Icon(layer, "BallArrow", UiSpriteGenerator.Chevron, Vector2.zero, Vector2.zero, BallArrowSize, LocalColor);

            var brackets = new GameObject("TargetBrackets", typeof(RectTransform)).GetComponent<RectTransform>();
            brackets.SetParent(layer, false);
            brackets.anchorMin = brackets.anchorMax = Vector2.zero;
            brackets.sizeDelta = Vector2.one * BracketsSize;
            float half = BracketCornerSize * 0.5f;
            // El sprite es la esquina de arriba a la izquierda; las otras tres son la misma girada
            (string name, Vector2 anchor, float angle)[] corners =
            {
                ("TopLeft", new Vector2(0f, 1f), 0f), ("TopRight", Vector2.one, -90f),
                ("BottomRight", new Vector2(1f, 0f), 180f), ("BottomLeft", Vector2.zero, 90f),
            };
            foreach (var (name, anchor, angle) in corners)
            {
                var offset = new Vector2(anchor.x > 0f ? -half : half, anchor.y > 0f ? -half : half);
                var corner = Icon(brackets, "Corner" + name, UiSpriteGenerator.Corner, anchor, offset, BracketCornerSize, LocalColor);
                corner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            var key = Panel(brackets, "SwitchKey", Vector2.one, new Vector2(SwitchKeySize * 0.5f + 6f, -SwitchKeySize * 0.5f), Vector2.one * SwitchKeySize, LocalColor);
            var keyLabel = Label(key.transform, "Label", 24, Middle, Vector2.zero, Vector2.one * SwitchKeySize);
            keyLabel.text = "Q";   // la tecla del cambio de jugador (ControlSwitcher)
            keyLabel.color = DarkText;
            keyLabel.GetComponent<Outline>().enabled = false;

            var view = canvasGo.AddComponent<PlayerMarkersView>();
            SetRef(view, "ball", ball);
            SetRef(view, "viewCamera", cam);
            SetEnum(view, "humanTeam", (int)TeamSide.Home);
            SetRef(view, "plateTemplate", plateView);
            SetRef(view, "ballArrow", arrow.rectTransform);
            SetRef(view, "ballArrowImage", arrow);
            SetRef(view, "targetBrackets", brackets);
            SetRef(view, "switchKey", key.gameObject);
        }

        // Marcador de retransmisión arriba en el centro: bloque de color de cada equipo con su nombre, goles a los lados del
        // tiempo y un punto volt bajo el equipo con la posesión. Debajo, el bàner de los avisos ("Preparats…", "GOL!"…)
        static void BuildScoreboard(GameObject canvasGo, GameConfigSO c, MatchFlow flow)
        {
            var root = canvasGo.transform;
            var board = Box(root, "Scoreboard", Top, new Vector2(860f, 84f), false);
            board.anchoredPosition = new Vector2(0f, -66f);
            var homeBlock = Panel(board, "HomeBlock", new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(224f, 70f), LocalColor);
            var homeName = Label(homeBlock.transform, "HomeName", 40, Middle, Vector2.zero, new Vector2(210f, 60f));
            homeName.color = DarkText;
            homeName.GetComponent<Outline>().enabled = false;   // texto oscuro sobre blanco: sin contorno
            var homeScore = Label(board, "HomeScore", 64, Middle, new Vector2(-148f, 0f), new Vector2(100f, 80f));
            var clock = Label(board, "Clock", 52, Middle, Vector2.zero, new Vector2(196f, 70f));
            clock.color = Volt;
            var awayScore = Label(board, "AwayScore", 64, Middle, new Vector2(148f, 0f), new Vector2(100f, 80f));
            var awayBlock = Panel(board, "AwayBlock", new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(224f, 70f), RivalColor);
            var awayName = Label(awayBlock.transform, "AwayName", 40, Middle, Vector2.zero, new Vector2(210f, 60f));
            var homeDot = Panel(board, "HomePossession", Vector2.zero, new Vector2(120f, -14f), new Vector2(18f, 18f), Volt);
            var awayDot = Panel(board, "AwayPossession", Vector2.right, new Vector2(-120f, -14f), new Vector2(18f, 18f), Volt);

            var banner = Box(root, "MessageBanner", new Vector2(0.5f, 0.62f), new Vector2(900f, 130f));
            var message = Label(banner, "Message", 84, Middle, Vector2.zero, new Vector2(880f, 120f));

            var view = canvasGo.AddComponent<ScoreboardView>();
            SetRef(view, "config", c);
            SetRef(view, "match", flow);
            SetRef(view, "homeNameLabel", homeName);
            SetRef(view, "homeScoreLabel", homeScore);
            SetRef(view, "clockLabel", clock);
            SetRef(view, "awayScoreLabel", awayScore);
            SetRef(view, "awayNameLabel", awayName);
            SetRef(view, "homePossession", homeDot.gameObject);
            SetRef(view, "awayPossession", awayDot.gameObject);
            SetRef(view, "messagePanel", banner.gameObject);
            SetRef(view, "messageLabel", message);
        }

        // Bàner con el nombre de la habilidad al activarse
        static void BuildAbilityBanner(GameObject canvasGo, GameConfigSO c)
        {
            var banner = Box(canvasGo.transform, "AbilityBanner", new Vector2(0.5f, 0.78f), new Vector2(1000f, 110f));
            var label = Label(banner, "AbilityName", 68, Middle, Vector2.zero, new Vector2(980f, 100f));
            label.color = Volt;
            var announcer = canvasGo.AddComponent<AbilityAnnouncer>();
            SetRef(announcer, "config", c);
            SetRef(announcer, "panel", banner.gameObject);
            SetRef(announcer, "label", label);
        }

        // Panel del jugador controlado, abajo a la izquierda, como la ficha de la maqueta: franja del equipo, rol abreviado en
        // grande, nombre del rol, energía (cifra y barra con la marca del coste de la habilidad, que se resalta un poco
        // cuando está lista) y estamina en una línea fina
        static void BuildPlayerPanel(GameObject canvasGo)
        {
            var size = new Vector2(520f, 112f);
            var panel = Box(canvasGo.transform, "PlayerPanel", Vector2.zero, size, false);
            panel.anchoredPosition = new Vector2(40f, 40f) + size * 0.5f;   // 40 de margen con las esquinas de la pantalla
            var left = new Vector2(0f, 0.5f);
            Panel(panel, "TeamStripe", left, new Vector2(10f, 0f), new Vector2(6f, size.y - 24f), LocalColor);
            var roleShort = Label(panel, "RoleShort", 64, left, new Vector2(84f, 0f), new Vector2(120f, 90f));
            var divider = StretchedImage(panel, "Divider", HairlineColor).rectTransform;
            divider.anchorMin = divider.anchorMax = left;
            divider.sizeDelta = new Vector2(1f, size.y - 36f);
            divider.anchoredPosition = new Vector2(150f, 0f);

            // Columna de la derecha: de x = 170 a x = 500
            const float x0 = 170f, width = 330f;
            var role = Label(panel, "Role", 20, left, new Vector2(x0 + width * 0.5f, 32f), new Vector2(width, 26f));
            role.alignment = TextAnchor.MiddleLeft;
            role.color = SoftText;
            var energyTitle = Label(panel, "EnergyTitle", 15, left, new Vector2(x0 + width * 0.5f, 6f), new Vector2(width, 22f));
            energyTitle.alignment = TextAnchor.MiddleLeft;
            energyTitle.color = SoftText;
            energyTitle.text = "ENERGIA";
            var energyValue = Label(panel, "EnergyValue", 26, left, new Vector2(x0 + width * 0.5f, 6f), new Vector2(width, 30f));
            energyValue.alignment = TextAnchor.MiddleRight;
            var energyFill = Bar(panel, "EnergyBar", left, new Vector2(x0 + width * 0.5f, -16f), new Vector2(width, 8f), EnergyIdleColor);
            var costMark = CostMark(energyFill, 3f, 4f);
            var highlight = Highlight(energyFill, PanelGlowSpread);

            var staminaTitle = Label(panel, "StaminaTitle", 13, left, new Vector2(x0 + 40f, -38f), new Vector2(80f, 20f));
            staminaTitle.alignment = TextAnchor.MiddleLeft;
            staminaTitle.color = SoftText;
            staminaTitle.text = "ESTAMINA";
            const float staminaX = x0 + 90f, staminaWidth = width - 90f;
            var staminaFill = Bar(panel, "StaminaBar", left, new Vector2(staminaX + staminaWidth * 0.5f, -38f), new Vector2(staminaWidth, 3f), StaminaLineColor);

            var view = canvasGo.AddComponent<PlayerHudView>();
            SetRef(view, "panel", panel.gameObject);
            SetRef(view, "roleShortLabel", roleShort);
            SetRef(view, "roleLabel", role);
            SetRef(view, "energyFill", energyFill);
            SetRef(view, "energyTrack", energyFill.parent.GetComponent<Image>());
            SetRef(view, "energyValueLabel", energyValue);
            SetRef(view, "abilityCostMark", costMark);
            SetRef(view, "energyHighlight", highlight);
            SetRef(view, "staminaFill", staminaFill);
        }

        // Raya blanca en el coste de la habilidad, sobre una barra de energía (devuelta por Bar); sobresale "overhang" px
        // por arriba y por abajo. La pone en su sitio quien la usa (PlayerHudView o PlayerPlate)
        static RectTransform CostMark(RectTransform fill, float width, float overhang)
        {
            var mark = StretchedImage(fill.parent, "AbilityCost", Color.white).rectTransform;
            mark.anchorMin = new Vector2(0.5f, 0f);   // provisional
            mark.anchorMax = new Vector2(0.5f, 1f);
            mark.offsetMin = new Vector2(-width * 0.5f, -overhang);
            mark.offsetMax = new Vector2(width * 0.5f, overhang);
            return mark;
        }

        // Resalte de una barra de energía con la habilidad lista: el relleno pasa a volt y se enciende un resplandor detrás
        static EnergyHighlight Highlight(RectTransform fill, float spread)
        {
            var glow = Glow(fill, spread, EnergyGlowColor);
            var highlight = fill.parent.gameObject.AddComponent<EnergyHighlight>();
            SetRef(highlight, "fill", fill.GetComponent<Image>());
            SetRef(highlight, "glow", glow);
            return highlight;
        }

        // Resumen del partido (se activa en Results): marcador, ganador, una tabla por equipo, récord del usuario y botones
        static void BuildResults(GameObject canvasGo, GameConfigSO c, MenuConfigSO menuConfig, MatchFlow flow, MatchScoring scoring)
        {
            var panel = StretchedImage(canvasGo.transform, "ResultsPanel", PanelColor);
            panel.raycastTarget = true;   // tapa el partido
            var t = panel.transform;
            var score = Label(t, "Score", 84, new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(1600f, 110f));
            var winner = Label(t, "Winner", 52, new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(1400f, 80f));
            winner.color = Volt;
            var record = Label(t, "Record", 40, new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(1600f, 60f));
            var restartButton = StyledButton<Button>(t, "RestartButton", "Tornar a jugar", 44, new Vector2(0.5f, 0.08f), new Vector2(-290f, 0f), new Vector2(520f, 110f));
            var menuButton = StyledButton<Button>(t, "MenuButton", "Menú principal", 44, new Vector2(0.5f, 0.08f), new Vector2(290f, 0f), new Vector2(520f, 110f));

            var results = canvasGo.AddComponent<ResultsScreen>();
            SetRef(results, "config", c);
            SetRef(results, "menuConfig", menuConfig);
            SetRef(results, "match", flow);
            SetRef(results, "scoring", scoring);
            SetRef(results, "panel", panel.gameObject);
            SetRef(results, "scoreLabel", score);
            SetRef(results, "winnerLabel", winner);
            SetRef(results, "recordLabel", record);
            SetRef(results, "restartButton", restartButton);
            SetRef(results, "menuButton", menuButton);
            BuildTeamTable(results, "homeTable", t, new Vector2(0.27f, 0.48f), new Color(0.6f, 0.75f, 1f));
            BuildTeamTable(results, "awayTable", t, new Vector2(0.73f, 0.48f), Color.white);
        }

        // Tabla de un equipo en el resumen: una columna por dato, con una línea por jugador y el total
        static void BuildTeamTable(ResultsScreen results, string field, Transform parent, Vector2 anchor, Color titleColor)
        {
            var box = Box(parent, field, anchor, new Vector2(860f, 480f));
            var title = Label(box, "Title", 44, Top, new Vector2(0f, -20f), new Vector2(800f, 60f));
            title.color = titleColor;
            SetRef(results, field + ".title", title);
            Panel(box, "HeaderStripe", Top, new Vector2(0f, -110f), new Vector2(820f, 44f), new Color(1f, 1f, 1f, 0.07f));   // tras los títulos de columna
            TableColumn(results, field + ".players", box, "Jugador", -270f, 280f);
            TableColumn(results, field + ".goals", box, "Gols", -60f, 100f);
            TableColumn(results, field + ".passes", box, "Pass.", 40f, 100f);
            TableColumn(results, field + ".recoveries", box, "Recup.", 140f, 100f);
            TableColumn(results, field + ".saves", box, "Atur.", 240f, 100f);
            TableColumn(results, field + ".points", box, "Punts", 350f, 100f);
            var abilities = Label(box, "Abilities", 28, Top, new Vector2(0f, -420f), new Vector2(800f, 40f));
            abilities.color = SoftText;
            SetRef(results, field + ".abilities", abilities);
        }

        static void TableColumn(ResultsScreen results, string field, Transform box, string header, float x, float width)
        {
            var title = Label(box, header + "Header", 28, Top, new Vector2(x, -90f), new Vector2(width, 40f));
            title.text = header;
            title.color = Volt;
            var column = Label(box, header + "Column", 32, Top, new Vector2(x, -135f), new Vector2(width, 260f));
            column.alignment = TextAnchor.UpperCenter;
            column.lineSpacing = 1.25f;
            SetRef(results, field, column);
        }

        // Menú de pausa: pausa, opciones (sin récord), controles y confirmación para reiniciar o salir
        static void BuildPauseMenu(GameObject canvasGo, MenuConfigSO menuConfig, MatchFlow flow, MatchPause pause, Button pauseButton)
        {
            var overlay = StretchedImage(canvasGo.transform, "PauseMenu", PanelColor);
            overlay.raycastTarget = true;   // tapa el partido: los clics no llegan al botón II
            var t = overlay.transform;

            var rootPage = StretchedGroup(t, "PausePage");
            var box = Box(rootPage.transform, "Box", Middle, new Vector2(700f, 540f));
            Heading(box, "Pausa");
            var resume = MenuButton<Button>(box, "ContinueButton", "Continuar", BoxButtonFont, Top, new Vector2(0f, -150f), BoxButtonSize);
            var optionsButton = MenuButton<Button>(box, "OptionsButton", "Opcions", BoxButtonFont, Top, new Vector2(0f, -250f), BoxButtonSize);
            var restart = MenuButton<Button>(box, "RestartButton", "Reiniciar partit", BoxButtonFont, Top, new Vector2(0f, -350f), BoxButtonSize);
            var menu = MenuButton<Button>(box, "MenuButton", "Menú principal", BoxButtonFont, Top, new Vector2(0f, -450f), BoxButtonSize);

            var optionsPage = StretchedGroup(t, "OptionsPage");
            var options = Options(optionsPage.transform, Middle, false);
            var controlsPage = StretchedGroup(t, "ControlsPage");
            var controlsBack = Controls(controlsPage.transform, Middle);
            var confirmPage = StretchedGroup(t, "ConfirmPage");
            var confirm = Confirm(confirmPage.transform, Middle, string.Empty, "El partit en curs es perdrà.");

            WireOptions(canvasGo.AddComponent<OptionsMenu>(), menuConfig, options);

            var pauseMenu = canvasGo.AddComponent<PauseMenu>();
            SetRef(pauseMenu, "config", menuConfig);
            SetRef(pauseMenu, "pause", pause);
            SetRef(pauseMenu, "match", flow);
            SetRef(pauseMenu, "overlay", overlay.gameObject);
            SetRef(pauseMenu, "pauseButton", pauseButton);
            SetRef(pauseMenu, "rootPage", rootPage);
            SetRef(pauseMenu, "optionsPage", optionsPage);
            SetRef(pauseMenu, "controlsPage", controlsPage);
            SetRef(pauseMenu, "confirmPage", confirmPage);
            SetRef(pauseMenu, "continueButton", resume);
            SetRef(pauseMenu, "optionsButton", optionsButton);
            SetRef(pauseMenu, "restartButton", restart);
            SetRef(pauseMenu, "menuButton", menu);
            SetRef(pauseMenu, "optionsFirst", options.Master);
            SetRef(pauseMenu, "controlsButton", options.Controls);
            SetRef(pauseMenu, "optionsBackButton", options.Back);
            SetRef(pauseMenu, "controlsBackButton", controlsBack);
            SetRef(pauseMenu, "confirmYesButton", confirm.Yes);
            SetRef(pauseMenu, "confirmNoButton", confirm.No);
            SetRef(pauseMenu, "confirmQuestion", confirm.Question);

            foreach (var page in new[] { rootPage, optionsPage, controlsPage, confirmPage })
                page.gameObject.SetActive(false);
            overlay.gameObject.SetActive(false);
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

        static Camera BuildCamera(GameConfigSO c, PoolBounds pool, BallController ball)
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
            SetRef(matchCamera, "ball", ball);
            matchCamera.SnapToTarget();
            return cam;
        }

        // ---------- Utilidades ----------

        internal static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider, bool castShadows = true)
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

        internal static void SetFlag(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Waterpulse] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Waterpulse] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetRef(Object target, string field, Object value)
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

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // Materiales URP generados (se actualizan en cada ejecución, no se duplican)
        internal class Materials
        {
            public readonly Material Water, Floor, Wall, LineWhite, LineRed, LineYellow, GoalFrame, Net, Body, Nose;
            public readonly Material HomeCap, AwayCap, GoalkeeperCap, HomeSuit, AwaySuit, Ball, Trail, Splash;

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
