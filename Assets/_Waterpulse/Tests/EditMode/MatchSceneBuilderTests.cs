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
        public void Build_AssignsAbilitiesByRole()
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
                    Assert.IsNull(p.Ability, p.name);
                    Assert.IsNull(p.GetComponent<AbilityBehaviour>(), p.name);
                    continue;
                }

                var energy = p.GetComponent<PlayerEnergy>();
                Assert.IsNotNull(energy, p.name);
                AssertRef(energy, "config", energyConfig);

                bool attacker = p.Role == PlayerRole.Attacker;
                Assert.AreEqual(attacker ? xut : torpede, p.Ability, p.name);
                Assert.AreEqual(attacker, p.GetComponent<XutTragolAbility>() != null, p.name + " Xut");
                Assert.AreEqual(!attacker, p.GetComponent<TorpedeBlauAbility>() != null, p.name + " Torpede");
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
            AssertRef(scorings[0], "ball", Object.FindFirstObjectByType<BallController>());

            var results = Object.FindFirstObjectByType<ResultsScreen>();
            AssertRef(results, "scoring", scorings[0]);
            foreach (var field in new[] { "homeColumn", "awayColumn", "recordLabel" })
                Assert.IsNotNull(new SerializedObject(results).FindProperty(field).objectReferenceValue, field);
        }

        static void AssertRef(Object target, string field, Object expected)
        {
            Assert.IsNotNull(target, $"falta {field}");
            var value = new SerializedObject(target).FindProperty(field).objectReferenceValue;
            Assert.AreEqual(expected, value, $"{target.GetType().Name}.{field}");
        }
    }
}
