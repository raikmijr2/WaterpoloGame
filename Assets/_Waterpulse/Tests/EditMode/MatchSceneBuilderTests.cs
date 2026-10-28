using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Waterpulse.Abilities;
using Waterpulse.AI;
using Waterpulse.Brains;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.EditorTools;
using Waterpulse.Gameplay;
using Waterpulse.UI;

namespace Waterpulse.Tests.EditMode
{
    // El generador debe poder ejecutarse varias veces sin duplicar objetos ni assets
    public class MatchSceneBuilderTests
    {
        [Test]
        public void Build_Twice_DoesNotDuplicateObjects()
        {
            MatchSceneBuilder.Build();
            MatchSceneBuilder.Build();

            // Fase 3: dos equipos de 3 jugadores de campo + portero
            Assert.AreEqual(8, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length, "jugadores");
            Assert.AreEqual(6, Object.FindObjectsByType<AIBrain>(FindObjectsSortMode.None).Length, "IA de campo");
            Assert.AreEqual(2, Object.FindObjectsByType<GoalkeeperController>(FindObjectsSortMode.None).Length, "porteros");
            Assert.AreEqual(1, Object.FindObjectsByType<BallController>(FindObjectsSortMode.None).Length, "pelotas");
            Assert.AreEqual(1, Object.FindObjectsByType<PoolBounds>(FindObjectsSortMode.None).Length, "piscinas");
            Assert.AreEqual(1, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, "cámaras");
            Assert.AreEqual(2, Object.FindObjectsByType<Goal>(FindObjectsSortMode.None).Length, "triggers de gol");
            int goals = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("Goal_"));
            Assert.AreEqual(2, goals, "porterías");
        }

        [Test]
        public void Build_Twice_KeepsSingleConfigAsset()
        {
            MatchSceneBuilder.Build();
            MatchSceneBuilder.Build();

            Assert.AreEqual(1, AssetDatabase.FindAssets("t:" + nameof(GameConfigSO)).Length);
        }

        [Test]
        public void Build_WiresConfigReferences()
        {
            MatchSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(MatchSceneBuilder.ConfigPath);
            Assert.IsNotNull(config);
            var ball = Object.FindFirstObjectByType<BallController>();
            foreach (var player in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                AssertRef(player, "config", config);
                AssertRef(player, "ball", ball);
            }
            var pool = Object.FindFirstObjectByType<PoolBounds>();
            foreach (var brain in Object.FindObjectsByType<AIBrain>(FindObjectsSortMode.None))
            {
                AssertRef(brain, "config", config);
                AssertRef(brain, "ball", ball);
                AssertRef(brain, "pool", pool);
            }
            foreach (var keeper in Object.FindObjectsByType<GoalkeeperController>(FindObjectsSortMode.None))
            {
                AssertRef(keeper, "config", config);
                AssertRef(keeper, "ball", ball);
                AssertRef(keeper, "pool", pool);
            }
            AssertRef(ball, "config", config);
            AssertRef(pool, "config", config);
            AssertRef(Object.FindFirstObjectByType<MatchCamera>(), "config", config);
            AssertRef(Object.FindFirstObjectByType<MatchCamera>(), "pool", Object.FindFirstObjectByType<PoolBounds>());
        }

        [Test]
        public void Build_GoalsDefendedByOppositeTeams()
        {
            MatchSceneBuilder.Build();

            var defenders = Object.FindObjectsByType<Goal>(FindObjectsSortMode.None).Select(g => g.DefendingTeam).ToList();
            CollectionAssert.AreEquivalent(new[] { TeamSide.Home, TeamSide.Away }, defenders);
        }

        [Test]
        public void Build_EachTeamHasOneOfEachRole()
        {
            MatchSceneBuilder.Build();

            var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var team in new[] { TeamSide.Home, TeamSide.Away })
            foreach (var role in new[] { PlayerRole.Attacker, PlayerRole.Playmaker, PlayerRole.Defender, PlayerRole.Goalkeeper })
                Assert.AreEqual(1, players.Count(p => p.Team == team && p.Role == role), $"{team} {role}");
        }

        [Test]
        public void Build_TeamAI_AggressiveRival_PassiveHome()
        {
            MatchSceneBuilder.Build();

            var teamAIs = Object.FindObjectsByType<TeamAI>(FindObjectsSortMode.None);
            Assert.AreEqual(2, teamAIs.Length, "un TeamAI por equipo");
            var home = teamAIs.Single(t => t.Team == TeamSide.Home);
            var away = teamAIs.Single(t => t.Team == TeamSide.Away);
            Assert.IsFalse(home.IsAggressive, "compañeros del humano: solo marcajes");
            Assert.IsTrue(away.IsAggressive, "rival: IA completa");

            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultySO>(MatchSceneBuilder.DifficultyPath);
            Assert.IsNotNull(difficulty, "asset de dificultad");
            AssertRef(home, "difficulty", difficulty);
            AssertRef(away, "difficulty", difficulty);

            // Cada jugador de campo usa el TeamAI de su equipo
            foreach (var brain in Object.FindObjectsByType<AIBrain>(FindObjectsSortMode.None))
            {
                var team = brain.GetComponent<PlayerController>().Team;
                AssertRef(brain, "teamAI", team == TeamSide.Home ? home : away);
            }
        }

        [Test]
        public void Build_CreatesMatchFlowAndHud_Once()
        {
            MatchSceneBuilder.Build();
            MatchSceneBuilder.Build();

            var flows = Object.FindObjectsByType<MatchFlow>(FindObjectsSortMode.None);
            Assert.AreEqual(1, flows.Length, "MatchFlow");
            Assert.AreEqual(1, Object.FindObjectsByType<ScoreboardView>(FindObjectsSortMode.None).Length, "marcador");
            Assert.AreEqual(1, Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length, "EventSystem");

            var results = Object.FindObjectsByType<ResultsScreen>(FindObjectsSortMode.None);
            Assert.AreEqual(1, results.Length, "pantalla final");
            AssertRef(results[0], "match", flows[0]);

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(MatchSceneBuilder.ConfigPath);
            AssertRef(flows[0], "config", config);
            AssertRef(flows[0], "ball", Object.FindFirstObjectByType<BallController>());
            AssertRef(flows[0], "pool", Object.FindFirstObjectByType<PoolBounds>());
        }

        [Test]
        public void Build_PlayersStartAtKickoffLayout()
        {
            MatchSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(MatchSceneBuilder.ConfigPath);
            var pool = Object.FindFirstObjectByType<PoolBounds>();
            foreach (var p in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                Vector3 expected = KickoffLayout.Position(p.Role, p.Team, pool.Center, pool.HalfLength, config);
                Assert.AreEqual(expected.x, p.transform.position.x, 1e-3f, p.name);
                Assert.AreEqual(expected.z, p.transform.position.z, 1e-3f, p.name);
            }
        }

        [Test]
        public void Build_EveryFieldPlayerHasBothAbilities()
        {
            MatchSceneBuilder.Build();

            var energyConfig = AssetDatabase.LoadAssetAtPath<EnergyConfigSO>(MatchSceneBuilder.EnergyConfigPath);
            var xut = AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(MatchSceneBuilder.XutTragolPath);
            var torpede = AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(MatchSceneBuilder.TorpedeBlauPath);
            Assert.IsNotNull(energyConfig, "EnergyConfig");
            Assert.AreEqual(AbilityKind.PowerShot, xut.kind);
            Assert.AreEqual(AbilityKind.Boost, torpede.kind);

            foreach (var p in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (p.IsGoalkeeper)
                {
                    Assert.IsNull(p.PowerShot, p.name);
                    Assert.IsNull(p.Boost, p.name);
                    Assert.IsNull(p.GetComponent<AbilityBehaviour>(), p.name);
                    continue;
                }

                var energy = p.GetComponent<PlayerEnergy>();
                Assert.IsNotNull(energy, p.name);
                AssertRef(energy, "config", energyConfig);

                // Todos, sea cual sea su rol: L con la pelota = Xut del Tràngol, sin ella = Torpede Blau
                Assert.AreEqual(xut, p.PowerShot, p.name);
                Assert.AreEqual(torpede, p.Boost, p.name);
                Assert.IsNotNull(p.GetComponent<XutTragolAbility>(), p.name + " Xut");
                Assert.IsNotNull(p.GetComponent<TorpedeBlauAbility>(), p.name + " Torpede");
            }

            Assert.AreEqual(1, Object.FindObjectsByType<EnergyRewards>(FindObjectsSortMode.None).Length, "EnergyRewards");
        }

        [Test]
        public void Build_WiresScoringAndResults()
        {
            MatchSceneBuilder.Build();

            var scoringConfig = AssetDatabase.LoadAssetAtPath<ScoringConfigSO>(MatchSceneBuilder.ScoringConfigPath);
            Assert.IsNotNull(scoringConfig, "ScoringConfig");
            Assert.AreEqual(100, scoringConfig.goalPoints, "puntos por gol del GDD");

            var scorings = Object.FindObjectsByType<MatchScoring>(FindObjectsSortMode.None);
            Assert.AreEqual(1, scorings.Length, "MatchScoring");
            AssertRef(scorings[0], "config", scoringConfig);
            AssertRef(scorings[0], "users", AssetDatabase.LoadAssetAtPath<UsersConfigSO>(MatchSceneBuilder.UsersConfigPath));
            AssertRef(scorings[0], "ball", Object.FindFirstObjectByType<BallController>());

            // El resumen: tablas de los dos equipos, récord y botones, sin referencias vacías
            var results = Object.FindFirstObjectByType<ResultsScreen>();
            AssertRef(results, "scoring", scorings[0]);
            AssertNoMissingReferences(results);
        }

        [Test]
        public void Build_HasMatchAudio_AndSoundOnEveryButton()
        {
            MatchSceneBuilder.Build();

            var directors = Object.FindObjectsByType<Waterpulse.Audio.AudioDirector>(FindObjectsSortMode.None);
            Assert.AreEqual(1, directors.Length, "AudioDirector");
            AssertRef(directors[0], "config", AssetDatabase.LoadAssetAtPath<AudioConfigSO>(SoundGenerator.ConfigPath));
            Assert.AreEqual((int)Waterpulse.Audio.AudioScene.Match, new SerializedObject(directors[0]).FindProperty("scene").intValue);

            foreach (var selectable in Object.FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (selectable is UnityEngine.UI.InputField) continue;
                Assert.IsNotNull(selectable.GetComponent<Waterpulse.Audio.UiSound>(), selectable.name);
            }
        }

        [Test]
        public void Build_PauseMenuIsWiredAndHidden()
        {
            MatchSceneBuilder.Build();

            var pauses = Object.FindObjectsByType<MatchPause>(FindObjectsSortMode.None);
            Assert.AreEqual(1, pauses.Length, "MatchPause");
            AssertRef(pauses[0], "match", Object.FindFirstObjectByType<MatchFlow>());
            AssertRef(pauses[0], "config", AssetDatabase.LoadAssetAtPath<GameConfigSO>(MatchSceneBuilder.ConfigPath));

            var menus = Object.FindObjectsByType<PauseMenu>(FindObjectsSortMode.None);
            Assert.AreEqual(1, menus.Length, "PauseMenu");
            AssertRef(menus[0], "pause", pauses[0]);
            AssertNoMissingReferences(menus[0]);
            Assert.AreEqual(1, Object.FindObjectsByType<OptionsMenu>(FindObjectsSortMode.None).Length, "opciones de la pausa");

            var so = new SerializedObject(menus[0]);
            Assert.IsFalse(((GameObject)so.FindProperty("overlay").objectReferenceValue).activeSelf, "la pausa empieza cerrada");
            var pauseButton = (UnityEngine.UI.Button)so.FindProperty("pauseButton").objectReferenceValue;
            Assert.AreEqual(UnityEngine.UI.Navigation.Mode.None, pauseButton.navigation.mode, "el botón II no se queda seleccionado");
        }

        [Test]
        public void Build_ResultsCanGoBackToMenu()
        {
            MatchSceneBuilder.Build();

            var results = Object.FindFirstObjectByType<ResultsScreen>();
            AssertRef(results, "menuConfig", AssetDatabase.LoadAssetAtPath<MenuConfigSO>(MenuSceneBuilder.ConfigPath));
            Assert.IsNotNull(new SerializedObject(results).FindProperty("menuButton").objectReferenceValue, "botón Menú principal");
            Assert.IsNotNull(new SerializedObject(results).FindProperty("restartButton").objectReferenceValue, "botón Tornar a jugar");
        }

        [Test]
        public void Build_StaminaAndCamera()
        {
            MatchSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(MatchSceneBuilder.ConfigPath);
            foreach (var p in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                var stamina = p.GetComponent<PlayerStamina>();
                Assert.AreEqual(!p.IsGoalkeeper, stamina != null, p.name + " estamina (solo jugadores de campo)");
                if (stamina != null) AssertRef(stamina, "config", config);
            }

            AssertRef(Object.FindFirstObjectByType<MatchCamera>(), "ball", Object.FindFirstObjectByType<BallController>());
            AssertRef(Object.FindFirstObjectByType<HumanBrain>(), "config", config);

            Assert.IsNull(GameObject.Find("GoalIndicator"), "sin flecha de portería (quitada a petición del usuario)");
        }

        [Test]
        public void Build_ModelOnlyOnHomeFieldPlayers()
        {
            MatchSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(MatchSceneBuilder.ConfigPath);
            bool hasModel = config.homePlayerModel != null
                            || AssetDatabase.LoadAssetAtPath<GameObject>(MatchSceneBuilder.DefaultPlayerModelPath) != null;
            foreach (var p in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                var animator = p.GetComponentInChildren<PlayerAnimator>();
                bool expected = hasModel && p.Team == TeamSide.Home && !p.IsGoalkeeper;
                Assert.AreEqual(expected, animator != null, p.name + " modelo animado (solo los de campo locales)");
                if (animator != null) AssertRef(animator, "player", p);
            }
        }

        [Test]
        public void BuildWithoutRivals_OnlyHomeTeam()
        {
            MatchSceneBuilder.Build(false);

            var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            Assert.AreEqual(4, players.Length, "solo el equipo local (3 de campo + portero)");
            Assert.IsTrue(players.All(p => p.Team == TeamSide.Home));

            MatchSceneBuilder.Build();   // deja la escena normal para el resto de tests
            Assert.AreEqual(8, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
        }

        [Test]
        public void Build_BallHasSeparateVisual()
        {
            MatchSceneBuilder.Build();

            var ball = Object.FindFirstObjectByType<BallController>();
            Assert.IsNull(ball.GetComponent<MeshRenderer>(), "la pelota física no se ve");
            var visual = ball.GetComponentInChildren<BallVisual>();
            Assert.IsNotNull(visual, "esfera visible hija");
            Assert.IsNotNull(visual.GetComponent<MeshRenderer>());
            Assert.IsNull(visual.GetComponent<Collider>(), "la esfera visible no choca");
            AssertRef(visual, "ball", ball);
        }

        [Test]
        public void Build_ShotMeterView()
        {
            MatchSceneBuilder.Build();

            var views = Object.FindObjectsByType<ShotMeterView>(FindObjectsSortMode.None);
            Assert.AreEqual(1, views.Length, "barra de tiro");
            AssertRef(views[0], "ball", Object.FindFirstObjectByType<BallController>());
            AssertRef(views[0], "viewCamera", Object.FindFirstObjectByType<Camera>());
            AssertNoMissingReferences(views[0]);
        }

        [Test]
        public void Build_PlayerMarkersAreWired()
        {
            MatchSceneBuilder.Build();

            var views = Object.FindObjectsByType<PlayerMarkersView>(FindObjectsSortMode.None);
            Assert.AreEqual(1, views.Length, "marcas sobre los jugadores");
            AssertRef(views[0], "ball", Object.FindFirstObjectByType<BallController>());
            AssertNoMissingReferences(views[0]);
            var plate = Object.FindFirstObjectByType<PlayerPlate>(FindObjectsInactive.Include);
            Assert.IsNotNull(plate, "plantilla de la mini placa");
            Assert.IsFalse(plate.gameObject.activeSelf, "la plantilla no se ve: se copia una por jugador");
            AssertNoMissingReferences(plate);

            // Arcos en el agua bajo el jugador controlado (sustituyen al triángulo rojo)
            var marker = GameObject.Find("ControlMarker");
            Assert.IsNotNull(marker);
            var arcs = marker.GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(arcs, "sprite de los arcos");
            Assert.IsNotNull(arcs.sprite);
            Assert.IsNull(marker.GetComponent<MeshRenderer>(), "ya no es el triángulo");
        }

        [Test]
        public void Build_WaterCoversOnlyTheField()
        {
            MatchSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(MatchSceneBuilder.ConfigPath);
            var pool = Object.FindFirstObjectByType<PoolBounds>().transform;
            foreach (string part in new[] { "Water", "Floor" })
            {
                var scale = pool.Find(part).localScale;
                Assert.AreEqual(config.poolLength, scale.x, 1e-4f, part + ": largo del campo");
                Assert.AreEqual(config.poolWidth, scale.z, 1e-4f, part + ": ancho del campo");
            }
        }

        [Test]
        public void Build_BroadcastHudIsWired()
        {
            MatchSceneBuilder.Build();

            foreach (var view in new MonoBehaviour[] { Object.FindFirstObjectByType<ScoreboardView>(), Object.FindFirstObjectByType<PlayerHudView>(),
                                                       Object.FindFirstObjectByType<AbilityAnnouncer>() })
            {
                Assert.IsNotNull(view);
                AssertNoMissingReferences(view);
            }

            // Seleccionado, el botón se vuelve amarillo: el texto tiene que cambiar a oscuro
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Assert.IsNotNull(button.GetComponent<SelectionTextColor>(), button.name);
        }

        static void AssertRef(Object target, string field, Object expected)
        {
            Assert.IsNotNull(target, $"falta {field}");
            var value = new SerializedObject(target).FindProperty(field).objectReferenceValue;
            Assert.AreEqual(expected, value, $"{target.GetType().Name}.{field}");
        }

        // Ninguna referencia del inspector se queda vacía (también las de las clases anidadas, como las tablas del resumen)
        static void AssertNoMissingReferences(Object target)
        {
            var property = new SerializedObject(target).GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script") continue;
                Assert.IsNotNull(property.objectReferenceValue, $"{target.GetType().Name}.{property.propertyPath}");
            }
        }
    }
}
