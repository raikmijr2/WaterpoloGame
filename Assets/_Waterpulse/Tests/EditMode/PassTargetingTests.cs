using NUnit.Framework;
using UnityEngine;
using Waterpulse.Gameplay;

namespace Waterpulse.Tests.EditMode
{
    // Pase al compañero más cercano dentro del cono alrededor de la dirección del stick
    public class PassTargetingTests
    {
        const float Cone = 60f;

        [Test]
        public void PicksClosestInsideCone()
        {
            var candidates = new[] { new Vector3(6f, 0f, 0f), new Vector3(3f, 0f, 1f) };
            Assert.AreEqual(1, PassTargeting.SelectReceiver(Vector3.zero, Vector3.right, candidates, Cone));
        }

        [Test]
        public void IgnoresCloserCandidateOutsideCone()
        {
            // El de atrás está más cerca, pero fuera del cono
            var candidates = new[] { new Vector3(-1f, 0f, 0f), new Vector3(5f, 0f, 0f) };
            Assert.AreEqual(1, PassTargeting.SelectReceiver(Vector3.zero, Vector3.right, candidates, Cone));
        }

        [Test]
        public void ReturnsMinusOneWhenNobodyInCone()
        {
            var candidates = new[] { new Vector3(-4f, 0f, 0f), new Vector3(0f, 0f, -4f) };
            Assert.AreEqual(-1, PassTargeting.SelectReceiver(Vector3.zero, Vector3.right, candidates, Cone));
        }

        [Test]
        public void ReturnsMinusOneWithoutAim()
        {
            var candidates = new[] { new Vector3(3f, 0f, 0f) };
            Assert.AreEqual(-1, PassTargeting.SelectReceiver(Vector3.zero, Vector3.zero, candidates, Cone));
        }

        [Test]
        public void AcceptsCandidateAtConeEdge()
        {
            // Justo a 60º de la dirección de apuntado
            var onEdge = Quaternion.Euler(0f, 60f, 0f) * Vector3.forward * 4f;
            var candidates = new[] { onEdge };
            Assert.AreEqual(0, PassTargeting.SelectReceiver(Vector3.zero, Vector3.forward, candidates, Cone + 0.01f));
        }

        [Test]
        public void IgnoresHeightDifference()
        {
            var candidates = new[] { new Vector3(4f, 3f, 0f) };
            Assert.AreEqual(0, PassTargeting.SelectReceiver(Vector3.zero, Vector3.right, candidates, Cone));
        }
    }
}
