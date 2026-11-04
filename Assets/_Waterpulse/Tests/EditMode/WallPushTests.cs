using NUnit.Framework;
using UnityEngine;
using Waterpulse.Gameplay;

namespace Waterpulse.Tests.EditMode
{
    // Onada del Mur de Ferro: portería en x = -15 (el campo queda hacia +X)
    public class WallPushTests
    {
        static readonly Vector3 Goal = new Vector3(-15f, 0f, 0f);
        static readonly Vector3 Inward = Vector3.right;
        const float Radius = 5f, Margin = 2.5f, Duration = 0.6f;

        static Vector3 Push(Vector3 player) => WallPush.VelocityFor(player, Goal, Inward, Radius, Margin, Duration);

        [Test]
        public void OutsideTheRadius_IsNotPushed()
        {
            Assert.AreEqual(Vector3.zero, Push(new Vector3(-9f, 0f, 0f)));
        }

        [Test]
        public void InsideTheRadius_EndsJustPastIt()
        {
            var player = new Vector3(-13f, 0f, 1f);
            Vector3 v = Push(player);
            // Frena de forma lineal hasta 0: recorre velocidad × duración / 2
            Vector3 end = player + v * (Duration * 0.5f);
            Assert.AreEqual(Radius + Margin, Vector3.Distance(end, Goal), 0.01f);
        }

        [Test]
        public void PushesAwayFromTheGoal()
        {
            var player = new Vector3(-12f, 0f, -2f);
            Vector3 v = Push(player);
            Assert.Greater(Vector3.Dot(v, player - Goal), 0f);
            Assert.AreEqual(0f, v.y);
        }

        [Test]
        public void OnTheGoalCenter_PushesTowardsTheField()
        {
            Vector3 v = Push(Goal);
            Assert.Greater(v.x, 0f);
            Assert.AreEqual(0f, v.z, 0.0001f);
        }
    }
}
