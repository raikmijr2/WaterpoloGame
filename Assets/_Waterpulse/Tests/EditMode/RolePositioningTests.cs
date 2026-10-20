using NUnit.Framework;
using UnityEngine;
using Waterpulse.AI;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Tests.EditMode
{
    // Colocación por rol con los valores por defecto de GameConfigSO (piscina 20x12)
    public class RolePositioningTests
    {
        GameConfigSO cfg;
        RolePositioning.Pitch pitch;

        [SetUp]
        public void SetUp()
        {
            cfg = ScriptableObject.CreateInstance<GameConfigSO>();
            pitch = new RolePositioning.Pitch { Center = Vector3.zero, HalfLength = cfg.poolLength * 0.5f, HalfWidth = cfg.poolWidth * 0.5f };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(cfg);

        Vector3 Target(PlayerRole role, TeamSide team, bool hasBall, Vector3 ball)
        {
            return RolePositioning.TargetFor(role, team, hasBall, ball, pitch, cfg);
        }

        [Test]
        public void WithBall_AttackerIsAheadOfBall_DefenderBehind()
        {
            var ball = new Vector3(-2f, 0f, 0f);
            Assert.Greater(Target(PlayerRole.Attacker, TeamSide.Home, true, ball).x, ball.x);
            Assert.Less(Target(PlayerRole.Defender, TeamSide.Home, true, ball).x, ball.x);
        }

        [Test]
        public void WithoutBall_AllFieldRolesAreBetweenBallAndOwnGoal()
        {
            var ball = new Vector3(2f, 0f, 1f);
            foreach (var role in new[] { PlayerRole.Attacker, PlayerRole.Playmaker, PlayerRole.Defender })
                Assert.Less(Target(role, TeamSide.Home, false, ball).x, ball.x, role.ToString());
        }

        [Test]
        public void AwayTeam_IsMirrorOfHome()
        {
            var ball = new Vector3(3f, 0f, 2f);
            var mirroredBall = new Vector3(-3f, 0f, -2f);
            foreach (var role in new[] { PlayerRole.Attacker, PlayerRole.Playmaker, PlayerRole.Defender })
            {
                Vector3 home = Target(role, TeamSide.Home, true, ball);
                Vector3 away = Target(role, TeamSide.Away, true, mirroredBall);
                Assert.AreEqual(home.x, -away.x, 1e-4f, role + " x");
                Assert.AreEqual(home.z, -away.z, 1e-4f, role + " z");
            }
        }

        [Test]
        public void Targets_StayInsideFieldMargins()
        {
            var corners = new[] { new Vector3(9.5f, 0f, 5.5f), new Vector3(-9.5f, 0f, -5.5f), new Vector3(9.5f, 0f, -5.5f) };
            foreach (var ball in corners)
            foreach (var role in new[] { PlayerRole.Attacker, PlayerRole.Playmaker, PlayerRole.Defender })
            foreach (var hasBall in new[] { true, false })
            {
                Vector3 t = Target(role, TeamSide.Home, hasBall, ball);
                Assert.LessOrEqual(Mathf.Abs(t.z), pitch.HalfWidth - cfg.formationEdgeMargin + 1e-4f);
                Assert.LessOrEqual(t.x, pitch.HalfLength - cfg.formationMinGoalDistance + 1e-4f);
                Assert.GreaterOrEqual(t.x, -pitch.HalfLength + cfg.formationEdgeMargin - 1e-4f);
            }
        }

        [Test]
        public void MarkPosition_IsGoalSideAtMarkDistance()
        {
            var rival = new Vector3(2f, 0f, 1f);
            Vector3 mark = RolePositioning.MarkPosition(rival, TeamSide.Home, pitch, cfg);
            Assert.AreEqual(cfg.markDistance, Vector3.Distance(mark, rival), 1e-3f);
            Assert.Less(mark.x, rival.x, "el local defiende la portería de -X: debe quedar entre el rival y ella");
        }

        [Test]
        public void MarkPosition_AwayDefendsPositiveX()
        {
            var rival = new Vector3(-2f, 0f, -1f);
            Vector3 mark = RolePositioning.MarkPosition(rival, TeamSide.Away, pitch, cfg);
            Assert.Greater(mark.x, rival.x);
        }

        [Test]
        public void MarkPosition_StaysInsideField()
        {
            var rival = new Vector3(-9.8f, 0f, 5.8f);
            Vector3 mark = RolePositioning.MarkPosition(rival, TeamSide.Home, pitch, cfg);
            Assert.GreaterOrEqual(mark.x, -pitch.HalfLength + cfg.formationEdgeMargin - 1e-4f);
            Assert.LessOrEqual(Mathf.Abs(mark.z), pitch.HalfWidth - cfg.formationEdgeMargin + 1e-4f);
        }
    }
}
