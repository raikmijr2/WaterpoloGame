using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Waterpulse.AI;
using Waterpulse.Brains;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.EditorTools;
using Waterpulse.Gameplay;

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

        static void AssertRef(Object target, string field, Object expected)
        {
            Assert.IsNotNull(target, $"falta {field}");
            var value = new SerializedObject(target).FindProperty(field).objectReferenceValue;
            Assert.AreEqual(expected, value, $"{target.GetType().Name}.{field}");
        }
    }
}
