using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    // Intercepción: 90 % si la pelota llega al cuerpo, baja al 30 % en el borde (0,8 m) y 0 fuera
    public class InterceptOddsTests
    {
        const float Body = 0.55f, Reach = 0.8f, AtBody = 0.9f, AtEdge = 0.3f;

        static float Chance(float d) => InterceptOdds.Chance(d, Body, Reach, AtBody, AtEdge);

        [Test]
        public void ThroughTheBody_IsHigh() => Assert.AreEqual(AtBody, Chance(0.2f), 1e-5f);

        [Test]
        public void AtBodyEdge_IsHigh() => Assert.AreEqual(AtBody, Chance(Body), 1e-5f);

        [Test]
        public void AtReachEdge_IsLow() => Assert.AreEqual(AtEdge, Chance(Reach), 1e-5f);

        [Test]
        public void OutOfReach_IsZero() => Assert.AreEqual(0f, Chance(Reach + 0.01f), 1e-5f);

        [Test]
        public void CloserIsLikelier() => Assert.Greater(Chance(0.6f), Chance(0.75f));
    }
}
