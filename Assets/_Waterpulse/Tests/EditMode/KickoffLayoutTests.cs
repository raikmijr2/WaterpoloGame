using NUnit.Framework;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Tests.EditMode
{
    // En el saque cada equipo está en su campo y el visitante es el local reflejado
    public class KickoffLayoutTests
    {
        GameConfigSO cfg;
        float halfLength;

        [SetUp]
        public void SetUp()
        {
            cfg = ScriptableObject.CreateInstance<GameConfigSO>();
            halfLength = cfg.poolLength * 0.5f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(cfg);

        static readonly PlayerRole[] FieldRoles = { PlayerRole.Attacker, PlayerRole.Playmaker, PlayerRole.Defender };

        [Test]
        public void FieldPlayers_StartInOwnHalf()
        {
            foreach (var role in FieldRoles)
            {
                Assert.Less(KickoffLayout.Position(role, TeamSide.Home, Vector3.zero, halfLength, cfg).x, 0f, "local " + role);
                Assert.Greater(KickoffLayout.Position(role, TeamSide.Away, Vector3.zero, halfLength, cfg).x, 0f, "visitante " + role);
            }
        }

        [Test]
        public void Away_IsMirrorOfHome()
        {
            foreach (var role in new[] { PlayerRole.Attacker, PlayerRole.Playmaker, PlayerRole.Defender, PlayerRole.Goalkeeper })
            {
                Vector3 home = KickoffLayout.Position(role, TeamSide.Home, Vector3.zero, halfLength, cfg);
                Vector3 away = KickoffLayout.Position(role, TeamSide.Away, Vector3.zero, halfLength, cfg);
                Assert.AreEqual(-home.x, away.x, 1e-4f, role.ToString());
                Assert.AreEqual(-home.z, away.z, 1e-4f, role.ToString());
            }
        }

        [Test]
        public void Goalkeeper_StartsInFrontOfOwnGoal()
        {
            Vector3 home = KickoffLayout.Position(PlayerRole.Goalkeeper, TeamSide.Home, Vector3.zero, halfLength, cfg);
            Assert.AreEqual(-halfLength + cfg.goalkeeperDepth, home.x, 1e-4f);
            Assert.AreEqual(0f, home.z, 1e-4f);
        }

        [Test]
        public void Facing_PointsTowardAttackedGoal()
        {
            Assert.AreEqual(Vector3.right, KickoffLayout.Facing(TeamSide.Home));
            Assert.AreEqual(Vector3.left, KickoffLayout.Facing(TeamSide.Away));
        }
    }
}
