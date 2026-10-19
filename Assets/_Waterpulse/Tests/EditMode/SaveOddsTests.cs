using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    // Probabilidad de parada: máxima al cuerpo, 0 a partir del alcance
    public class SaveOddsTests
    {
        const float Body = 0.4f;
        const float Reach = 1.2f;
        const float AtBody = 0.8f;

        [Test]
        public void AtBody_IsMaximum()
        {
            Assert.AreEqual(AtBody, SaveOdds.Chance(0f, Body, Reach, AtBody), 1e-5f);
            Assert.AreEqual(AtBody, SaveOdds.Chance(Body, Body, Reach, AtBody), 1e-5f);
        }

        [Test]
        public void AtReach_IsZero()
        {
            Assert.AreEqual(0f, SaveOdds.Chance(Reach, Body, Reach, AtBody), 1e-5f);
            Assert.AreEqual(0f, SaveOdds.Chance(Reach + 3f, Body, Reach, AtBody), 1e-5f);
        }

        [Test]
        public void Halfway_IsHalf()
        {
            float halfway = (Body + Reach) * 0.5f;
            Assert.AreEqual(AtBody * 0.5f, SaveOdds.Chance(halfway, Body, Reach, AtBody), 1e-5f);
        }

        [Test]
        public void DecreasesWithDistance()
        {
            float near = SaveOdds.Chance(0.6f, Body, Reach, AtBody);
            float far = SaveOdds.Chance(1f, Body, Reach, AtBody);
            Assert.Greater(near, far);
        }

        [Test]
        public void NeverAboveOne()
        {
            Assert.LessOrEqual(SaveOdds.Chance(0f, Body, Reach, 5f), 1f);
        }
    }
}
