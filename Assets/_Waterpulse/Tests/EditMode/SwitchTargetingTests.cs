using NUnit.Framework;
using UnityEngine;
using Waterpulse.Gameplay;

namespace Waterpulse.Tests.EditMode
{
    // Cambio de jugador (Q y automático al defender) y su vista previa en el HUD: el más cercano a la pelota
    public class SwitchTargetingTests
    {
        [Test]
        public void PicksClosestToBall()
        {
            var candidates = new[] { new Vector3(5f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(-3f, 0f, 0f) };
            Assert.AreEqual(1, SwitchTargeting.Nearest(Vector3.zero, candidates));
        }

        [Test]
        public void SkipsExcludedCandidate()
        {
            // El excluido (el que ya controlas) es el más cercano: va al siguiente
            var candidates = new[] { new Vector3(5f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(-3f, 0f, 0f) };
            Assert.AreEqual(2, SwitchTargeting.Nearest(Vector3.zero, candidates, 1));
        }

        [Test]
        public void ReturnsMinusOneWhenNoCandidateLeft()
        {
            Assert.AreEqual(-1, SwitchTargeting.Nearest(Vector3.zero, new Vector3[0]));
            Assert.AreEqual(-1, SwitchTargeting.Nearest(Vector3.zero, new[] { Vector3.one }, 0));
        }
    }
}
