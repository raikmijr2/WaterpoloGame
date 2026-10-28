using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    public class ShotMeterTests
    {
        static ShotMeter Meter() => new ShotMeter(1f, 0.85f, 0.08f, 0.7f);

        [Test]
        public void Fills_OverDuration_AndReportsFull()
        {
            var meter = Meter();
            meter.Begin();
            Assert.IsFalse(meter.Tick(0.5f));
            Assert.AreEqual(0.5f, meter.Fill, 1e-5f);
            Assert.IsTrue(meter.Tick(0.6f), "llena: hay que tirar");
            Assert.AreEqual(1f, meter.Fill, 1e-5f);
        }

        [Test]
        public void NotCharging_DoesNotFill()
        {
            var meter = Meter();
            Assert.IsFalse(meter.Tick(2f));
            Assert.AreEqual(0f, meter.Fill, 1e-5f);
        }

        [Test]
        public void Green_IsPerfect()
        {
            var meter = Meter();
            Assert.AreEqual(1f, meter.Quality(0.85f), 1e-5f);
            Assert.AreEqual(1f, meter.Quality(0.9f), 1e-5f);
            Assert.AreEqual(1f, meter.Quality(0.93f), 1e-5f);
        }

        [Test]
        public void OffGreen_DropsWithDistance()
        {
            var meter = Meter();
            Assert.AreEqual(0f, meter.Quality(0f), 1e-5f);
            Assert.Less(meter.Quality(0.4f), meter.Quality(0.8f));
            Assert.LessOrEqual(meter.Quality(0.84f), 0.7f);
            Assert.Less(meter.Quality(0.99f), meter.Quality(0.95f));
        }

        [Test]
        public void Release_ReturnsQuality_AndStops()
        {
            var meter = Meter();
            meter.Begin();
            meter.Tick(0.9f);
            Assert.AreEqual(1f, meter.Release(), 1e-5f);
            Assert.IsFalse(meter.IsCharging);
        }

        [Test]
        public void Overfilled_IsWorstQuality()
        {
            var meter = Meter();
            meter.Begin();
            meter.Tick(5f);
            Assert.AreEqual(0f, meter.Release(), 1e-5f);
        }

        [Test]
        public void PowerAndError_FollowQuality()
        {
            Assert.AreEqual(1f, ShotMeter.PowerFactor(1f, 0.6f), 1e-5f);
            Assert.AreEqual(0.6f, ShotMeter.PowerFactor(0f, 0.6f), 1e-5f);
            Assert.AreEqual(0f, ShotMeter.ErrorAngle(1f, 12f), 1e-5f);
            Assert.AreEqual(12f, ShotMeter.ErrorAngle(0f, 12f), 1e-5f);
        }
    }
}
