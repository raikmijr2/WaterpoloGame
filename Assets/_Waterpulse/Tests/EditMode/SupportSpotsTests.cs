using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Waterpulse.AI;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Tests.EditMode
{
    // Juego en triángulo con los valores por defecto de GameConfigSO (el local ataca hacia +X)
    public class SupportSpotsTests
    {
        GameConfigSO cfg;
        RolePositioning.Pitch pitch;
        static readonly List<Vector3> NoDefenders = new List<Vector3>();

        [SetUp]
        public void SetUp()
        {
            cfg = ScriptableObject.CreateInstance<GameConfigSO>();
            pitch = new RolePositioning.Pitch { Center = Vector3.zero, HalfLength = cfg.poolLength * 0.5f, HalfWidth = cfg.poolWidth * 0.5f };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(cfg);

        Vector3 Goal(TeamSide team) => new Vector3(team.AttackSign() * pitch.HalfLength, 0f, 0f);

        [Test]
        public void Point_IsAheadOfHolder_OnTheOpenSide()
        {
            var holder = new Vector3(0f, 0f, 3f);
            var point = SupportSpots.SlotPosition(SupportSlot.Point, holder, TeamSide.Home, pitch, cfg);
            Assert.Greater(point.x, holder.x);
            Assert.Less(point.z, 0f, "al lado contrario de la pelota");
        }

        [Test]
        public void Wing_IsBehindHolder_OpenTowardsTheFreeSide()
        {
            var holder = new Vector3(0f, 0f, 3f);
            var wing = SupportSpots.SlotPosition(SupportSlot.Wing, holder, TeamSide.Home, pitch, cfg);
            Assert.Less(wing.x, holder.x);
            Assert.Less(wing.z, holder.z);
        }

        [Test]
        public void Away_PointIsAheadTowardsMinusX()
        {
            var holder = new Vector3(0f, 0f, 0f);
            Assert.Less(SupportSpots.SlotPosition(SupportSlot.Point, holder, TeamSide.Away, pitch, cfg).x, holder.x);
        }

        [Test]
        public void Point_NeverGetsCloserToGoalThanTheFormationLimit()
        {
            var holder = new Vector3(pitch.HalfLength - 3f, 0f, 0f);
            var point = SupportSpots.SlotPosition(SupportSlot.Point, holder, TeamSide.Home, pitch, cfg);
            Assert.LessOrEqual(point.x, pitch.HalfLength - cfg.formationMinGoalDistance + 0.001f);
        }

        [Test]
        public void BestSpot_MovesAwayFromTheDefenderSittingOnTheSlot()
        {
            var holder = new Vector3(0f, 0f, 0f);
            var slot = SupportSpots.SlotPosition(SupportSlot.Point, holder, TeamSide.Home, pitch, cfg);
            var defenders = new List<Vector3> { slot };
            var best = SupportSpots.BestSpot(slot, slot, holder, Goal(TeamSide.Home), TeamSide.Home, defenders, pitch, cfg);
            Assert.Greater(SupportSpots.Openness(best, defenders), SupportSpots.Openness(slot, defenders));
        }

        [Test]
        public void BestSpot_AvoidsBlockedPassingLane()
        {
            var holder = new Vector3(0f, 0f, 0f);
            var slot = new Vector3(4f, 0f, 0f);
            var defenders = new List<Vector3> { new Vector3(2f, 0f, 0f) };   // tapa el pase al hueco
            var best = SupportSpots.BestSpot(slot, slot, holder, Goal(TeamSide.Home), TeamSide.Home, defenders, pitch, cfg);
            Assert.IsTrue(AttackDecision.IsLaneClear(holder, best, defenders, cfg.aiLaneWidth));
        }

        [Test]
        public void BestSpot_KeepsCurrentSpotWhenNothingIsClearlyBetter()
        {
            var holder = new Vector3(0f, 0f, 0f);
            var slot = SupportSpots.SlotPosition(SupportSlot.Point, holder, TeamSide.Home, pitch, cfg);
            var current = slot + new Vector3(0.3f, 0f, 0f);
            var best = SupportSpots.BestSpot(slot, current, holder, Goal(TeamSide.Home), TeamSide.Home, NoDefenders, pitch, cfg);
            Assert.AreEqual(current, best);
        }
    }
}
