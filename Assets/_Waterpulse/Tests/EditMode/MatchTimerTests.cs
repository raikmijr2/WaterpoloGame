using NUnit.Framework;
using Waterpulse.Core;

namespace Waterpulse.Tests.EditMode
{
    public class MatchTimerTests
    {
        [Test]
        public void DoesNotRunUntilStarted()
        {
            var timer = new MatchTimer(10f);
            timer.Tick(5f);
            Assert.AreEqual(10f, timer.Remaining, 1e-5f);
        }

        [Test]
        public void CountsDownWhileRunning()
        {
            var timer = new MatchTimer(10f);
            timer.Start();
            timer.Tick(3f);
            Assert.AreEqual(7f, timer.Remaining, 1e-5f);
        }

        [Test]
        public void PauseStopsCountdown()
        {
            var timer = new MatchTimer(10f);
            timer.Start();
            timer.Tick(2f);
            timer.Pause();
            timer.Tick(5f);
            Assert.AreEqual(8f, timer.Remaining, 1e-5f);
        }

        [Test]
        public void ReportsExpiryOnlyOnce_AndClampsToZero()
        {
            var timer = new MatchTimer(1f);
            timer.Start();
            Assert.IsTrue(timer.Tick(2f));
            Assert.AreEqual(0f, timer.Remaining);
            Assert.IsTrue(timer.IsExpired);
            Assert.IsFalse(timer.Tick(1f));
        }
    }
}
