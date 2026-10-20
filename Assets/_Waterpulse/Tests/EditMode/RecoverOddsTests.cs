using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    // Probabilidad de recuperar: máxima pegado, 50 % al límite del alcance, 0 fuera de él
    public class RecoverOddsTests
    {
        const float Close = 0.8f;
        const float Range = 1.2f;
        const float ChanceClose = 0.75f;
        const float ChanceFar = 0.5f;

        static float Chance(float d) => RecoverOdds.Chance(d, Close, Range, ChanceClose, ChanceFar);

        [Test]
        public void Close_IsMaximum() => Assert.AreEqual(ChanceClose, Chance(0.5f), 1e-5f);

        [Test]
        public void AtRange_IsFarChance() => Assert.AreEqual(ChanceFar, Chance(Range), 1e-5f);

        [Test]
        public void OutOfRange_IsZero() => Assert.AreEqual(0f, Chance(Range + 0.01f), 1e-5f);

        [Test]
        public void Halfway_IsInterpolated()
        {
            float halfway = (Close + Range) * 0.5f;
            Assert.AreEqual((ChanceClose + ChanceFar) * 0.5f, Chance(halfway), 1e-5f);
        }

        [Test]
        public void IncreasesWhenCloser() => Assert.Greater(Chance(0.9f), Chance(1.1f));
    }
}
