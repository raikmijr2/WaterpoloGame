using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Waterpulse.AI;

namespace Waterpulse.Tests.EditMode
{
    // Decisiones de la IA con pelota (atacando hacia +X, portería en x = 10)
    public class AttackDecisionTests
    {
        static readonly Vector3 Goal = new Vector3(10f, 0f, 0f);
        static readonly AttackDecision.Settings S = new AttackDecision.Settings { ShootRange = 6f, PressureRadius = 1.5f, LaneWidth = 0.8f };
        static readonly List<Vector3> None = new List<Vector3>();

        static AttackChoice Decide(Vector3 self, List<Vector3> mates, List<Vector3> fieldOpp, List<Vector3> allOpp = null)
        {
            return AttackDecision.Decide(self, 1f, Goal, mates, fieldOpp, allOpp ?? fieldOpp, S);
        }

        [Test]
        public void ShootsWhenCloseAndLaneClear()
        {
            Assert.AreEqual(AttackAction.Shoot, Decide(new Vector3(6f, 0f, 0f), None, None).Action);
        }

        [Test]
        public void DoesNotShootFromFar()
        {
            Assert.AreEqual(AttackAction.Advance, Decide(new Vector3(0f, 0f, 0f), None, None).Action);
        }

        [Test]
        public void DoesNotShootWhenDefenderInLane()
        {
            var blocker = new List<Vector3> { new Vector3(8f, 0f, 0.2f) };
            Assert.AreNotEqual(AttackAction.Shoot, Decide(new Vector3(6f, 0f, 0f), None, blocker).Action);
        }

        [Test]
        public void GoalkeeperDoesNotBlockShot()
        {
            // El portero solo está en "todos los rivales", no en los de campo
            var keeper = new List<Vector3> { new Vector3(9.2f, 0f, 0f) };
            Assert.AreEqual(AttackAction.Shoot, Decide(new Vector3(6f, 0f, 0f), None, None, keeper).Action);
        }

        [Test]
        public void PassesWhenPressured_ToClearTeammate()
        {
            var self = new Vector3(0f, 0f, 0f);
            var presser = new List<Vector3> { new Vector3(1f, 0f, 0f) };
            var mates = new List<Vector3> { new Vector3(-2f, 0f, 4f) };
            var choice = Decide(self, mates, presser);
            Assert.AreEqual(AttackAction.Pass, choice.Action);
            Assert.AreEqual(0, choice.PassIndex);
        }

        [Test]
        public void WhenPressured_PrefersMoreAdvancedTeammate()
        {
            var self = new Vector3(0f, 0f, 0f);
            var presser = new List<Vector3> { new Vector3(-1f, 0f, -1f) };
            var mates = new List<Vector3> { new Vector3(-3f, 0f, 3f), new Vector3(3f, 0f, 3f) };
            Assert.AreEqual(1, Decide(self, mates, presser).PassIndex);
        }

        [Test]
        public void WhenPressured_SkipsTeammateWithBlockedLane()
        {
            var self = new Vector3(0f, 0f, 0f);
            var opponents = new List<Vector3> { new Vector3(1f, 0f, 0f), new Vector3(2f, 0f, 2f) };
            var mates = new List<Vector3> { new Vector3(4f, 0f, 4f), new Vector3(-3f, 0f, 3f) };
            Assert.AreEqual(1, Decide(self, mates, opponents).PassIndex);
        }

        [Test]
        public void AdvancesWhenNotPressured()
        {
            var farOpponent = new List<Vector3> { new Vector3(-5f, 0f, 5f) };
            var mates = new List<Vector3> { new Vector3(3f, 0f, 3f) };
            Assert.AreEqual(AttackAction.Advance, Decide(new Vector3(0f, 0f, 0f), mates, farOpponent).Action);
        }
    }
}
