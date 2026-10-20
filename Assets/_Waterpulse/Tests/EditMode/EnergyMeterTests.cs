using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    // Energía: se suma hasta el máximo y solo se gasta si alcanza
    public class EnergyMeterTests
    {
        [Test]
        public void StartsAtInitial_ClampedToMax()
        {
            Assert.AreEqual(50f, new EnergyMeter(100f, 50f).Current);
            Assert.AreEqual(100f, new EnergyMeter(100f, 150f).Current);
            Assert.AreEqual(0f, new EnergyMeter(100f, -10f).Current);
        }

        [Test]
        public void Add_NeverExceedsMax()
        {
            var meter = new EnergyMeter(100f, 95f);
            meter.Add(10f);
            Assert.AreEqual(100f, meter.Current);
        }

        [Test]
        public void Add_IgnoresNegativeAmounts()
        {
            var meter = new EnergyMeter(100f, 50f);
            meter.Add(-20f);
            Assert.AreEqual(50f, meter.Current);
        }

        [Test]
        public void TrySpend_WithEnoughEnergy_Spends()
        {
            var meter = new EnergyMeter(100f, 50f);
            Assert.IsTrue(meter.TrySpend(35f));
            Assert.AreEqual(15f, meter.Current, 1e-5f);
        }

        [Test]
        public void TrySpend_WithoutEnoughEnergy_DoesNothing()
        {
            var meter = new EnergyMeter(100f, 20f);
            Assert.IsFalse(meter.TrySpend(25f));
            Assert.AreEqual(20f, meter.Current);
        }

        [Test]
        public void TrySpend_ExactAmount_LeavesZero()
        {
            var meter = new EnergyMeter(100f, 25f);
            Assert.IsTrue(meter.TrySpend(25f));
            Assert.AreEqual(0f, meter.Current);
        }

        [Test]
        public void Normalized_IsFractionOfMax()
        {
            Assert.AreEqual(0.4f, new EnergyMeter(100f, 40f).Normalized, 1e-5f);
        }

        [Test]
        public void RechargesFromGameplayRewards()
        {
            // Valores del GDD: pase +5, recuperación +10, gol +10
            var meter = new EnergyMeter(100f, 0f);
            meter.Add(5f);
            meter.Add(10f);
            meter.Add(10f);
            Assert.AreEqual(25f, meter.Current);
            Assert.IsTrue(meter.CanSpend(25f), "ya alcanza para un Torpede Blau");
            Assert.IsFalse(meter.CanSpend(35f), "aún no alcanza para un Xut del Tràngol");
        }
    }
}
