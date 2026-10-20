using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        const string PhysicsDir = RootDir + "/Art/Physics";

        // Posición inicial en campo propio (x = avance hacia la portería rival, y = lateral), por rol.
        // Durante el juego manda RolePositioning; esto es solo dónde aparecen
        static readonly Vector2 AttackerStart = new Vector2(-1.5f, 2f);
        static readonly Vector2 PlaymakerStart = new Vector2(-4f, -2.5f);
        static readonly Vector2 DefenderStart = new Vector2(-7f, 0f);

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

            // TeamAI agresivo para el rival (persigue, presiona, corta, pasa y tira); pasivo para los compañeros del humano (solo marcajes)
            BuildTeam(config, mats, ball, pool, TeamSide.Home, mats.HomeCap, mats.HomeSuit, BuildTeamAI(TeamSide.Home, ball, difficulty, false));
            BuildTeam(config, mats, ball, pool, TeamSide.Away, mats.AwayCap, mats.AwaySuit, BuildTeamAI(TeamSide.Away, ball, difficulty, true));

            var score = BuildMatchSystems(config, ball, pool);
            BuildHumanControl(config, ball, mats);
            BuildCamera(config, pool);
            BuildScoreboard(score);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        static GameConfigSO LoadOrCreateConfig() => LoadOrCreate<GameConfigSO>(ConfigPath);

        // Solo crea el asset si no existe: nunca pisa los valores ajustados
        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<T>(path);
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
            return ball;
        }

        // Un equipo: Atacant, Organitzador y Defensor con AIBrain, y el portero con GoalkeeperController
        static void BuildTeam(GameConfigSO c, Materials mats, BallController ball, PoolBounds pool, TeamSide team,
                              Material cap, Material suit, TeamAI teamAI)
        {
            var root = new GameObject("Team_" + team).transform;
            float sign = team.AttackSign();
            Vector3 facing = Vector3.right * sign;

            BuildPlayer(c, mats, ball, pool, root, team, PlayerRole.Attacker, StartPosition(pool, AttackerStart, sign), facing, cap, suit, teamAI);
            BuildPlayer(c, mats, ball, pool, root, team, PlayerRole.Playmaker, StartPosition(pool, PlaymakerStart, sign), facing, cap, suit, teamAI);
            BuildPlayer(c, mats, ball, pool, root, team, PlayerRole.Defender, StartPosition(pool, DefenderStart, sign), facing, cap, suit, teamAI);

            Vector3 goalkeeper = pool.AttackedGoalCenter(team.Opponent()) + facing * c.goalkeeperDepth;
            BuildPlayer(c, mats, ball, pool, root, team, PlayerRole.Goalkeeper, goalkeeper, facing, mats.GoalkeeperCap, suit, null);
        }

        // Las posiciones del visitante son las del local reflejadas
        static Vector3 StartPosition(PoolBounds pool, Vector2 start, float sign)
        {
            return pool.Center + new Vector3(start.x * sign, 0f, start.y * sign);
        }

        static void BuildPlayer(GameConfigSO c, Materials mats, BallController ball, PoolBounds pool, Transform parent, TeamSide team,
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
        }

        static MatchScore BuildMatchSystems(GameConfigSO c, BallController ball, PoolBounds pool)
        {
            var go = new GameObject("Match");
            var score = go.AddComponent<MatchScore>();
            var restart = go.AddComponent<GoalRestart>();
            SetRef(restart, "config", c);
            SetRef(restart, "ball", ball);
            SetRef(restart, "pool", pool);
            return score;
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

        static void BuildScoreboard(MatchScore score)
        {
            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var textGo = new GameObject("Scoreboard", typeof(RectTransform), typeof(Text), typeof(Outline));
            textGo.transform.SetParent(canvasGo.transform, false);
            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(900f, 80f);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            textGo.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

            var view = canvasGo.AddComponent<ScoreboardView>();
            SetRef(view, "score", score);
            SetRef(view, "label", text);
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
            public readonly Material HomeCap, AwayCap, GoalkeeperCap, HomeSuit, AwaySuit, Ball, Marker;

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
        }
    }
}
