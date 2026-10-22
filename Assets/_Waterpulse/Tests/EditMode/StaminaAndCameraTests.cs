using NUnit.Framework;
using UnityEngine;
using Waterpulse.Gameplay;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    // Estamina: empieza llena, el dash gasta 30 y deja una espera, se recarga 20 por segundo
    public class StaminaMeterTests
    {
        const float Cost = 30f, Cooldown = 0.5f;

        [Test]
        public void StartsFull()
        {
            var s = new StaminaMeter(100f, 20f);
            Assert.AreEqual(100f, s.Current);
        }

        [Test]
        public void Dash_SpendsAndStartsCooldown()
        {
            var s = new StaminaMeter(100f, 20f);
            Assert.IsTrue(s.TrySpend(Cost, Cooldown));
            Assert.AreEqual(70f, s.Current, 1e-4f);
            Assert.IsTrue(s.IsCoolingDown);
            Assert.IsFalse(s.TrySpend(Cost, Cooldown), "durante la espera no hay otro dash");
        }

        [Test]
        public void AfterCooldown_CanDashAgain()
        {
            var s = new StaminaMeter(100f, 0f);
            s.TrySpend(Cost, Cooldown);
            s.Tick(Cooldown + 0.01f);
            Assert.IsTrue(s.TrySpend(Cost, Cooldown));
        }

        [Test]
        public void WithoutEnoughStamina_NoDash()
        {
            var s = new StaminaMeter(100f, 0f);
            s.TrySpend(Cost, 0f);
            s.TrySpend(Cost, 0f);
            s.TrySpend(Cost, 0f);   // quedan 10
            Assert.IsFalse(s.TrySpend(Cost, 0f));
            Assert.AreEqual(10f, s.Current, 1e-4f);
        }

        [Test]
        public void Regenerates_UpToMax()
        {
            var s = new StaminaMeter(100f, 20f);
            s.TrySpend(Cost, 0f);
            s.Tick(1f);
            Assert.AreEqual(90f, s.Current, 1e-4f);
            s.Tick(10f);
            Assert.AreEqual(100f, s.Current, 1e-4f);
        }
    }

    // Dash: más fuerte cuanto más lento vas; a velocidad máxima no se nota
    public class DashPowerTests
    {
        const float Max = 4f, Peak = 3f;

        [Test]
        public void Stopped_IsFullStrength_AndPeakSpeed()
        {
            Assert.AreEqual(1f, DashPower.Strength(0f, Max), 1e-5f);
            Assert.AreEqual(Peak, DashPower.SpeedMultiplier(0f, Max, Peak), 1e-5f);
        }

        [Test]
        public void AtMaxSpeed_HasNoEffect()
        {
            Assert.AreEqual(0f, DashPower.Strength(Max, Max), 1e-5f);
            Assert.AreEqual(1f, DashPower.SpeedMultiplier(Max, Max, Peak), 1e-5f, "sigue a su velocidad actual");
        }

        [Test]
        public void HalfSpeed_IsHalfway()
        {
            Assert.AreEqual(0.5f, DashPower.Strength(2f, Max), 1e-5f);
            Assert.AreEqual((0.5f + Peak) * 0.5f, DashPower.SpeedMultiplier(2f, Max, Peak), 1e-5f);
        }

        [Test]
        public void SlowerMeansStronger()
        {
            Assert.Greater(DashPower.SpeedMultiplier(1f, Max, Peak), DashPower.SpeedMultiplier(3f, Max, Peak));
        }

        [Test]
        public void AboveMaxSpeed_IsZero()
        {
            Assert.AreEqual(0f, DashPower.Strength(6f, Max), 1e-5f);
        }
    }

    // Cámara que sigue la jugada sin salirse de la piscina
    public class CameraFramingTests
    {
        const float HalfLength = 15f, HalfWidth = 10f, Margin = 1.5f;

        [Test]
        public void WiderScreen_NeedsLessDistance_ForSameViewLength()
        {
            float wide = CameraFraming.DistanceForViewLength(17f, 40f, 16f / 9f);
            float narrow = CameraFraming.DistanceForViewLength(17f, 40f, 4f / 3f);
            Assert.Less(wide, narrow);
        }

        [Test]
        public void FocusInsideLimits_IsUnchanged()
        {
            var focus = new Vector3(2f, 0f, 1f);
            Assert.AreEqual(focus, CameraFraming.ClampFocus(focus, Vector3.zero, HalfLength, HalfWidth, Margin, 8.5f, 5f));
        }

        [Test]
        public void FocusNearGoal_IsClampedSoViewEndsAtPoolEdge()
        {
            var focus = new Vector3(15f, 0f, 0f);
            Vector3 clamped = CameraFraming.ClampFocus(focus, Vector3.zero, HalfLength, HalfWidth, Margin, 8.5f, 5f);
            Assert.AreEqual(HalfLength + Margin - 8.5f, clamped.x, 1e-4f);
        }

        [Test]
        public void ViewBiggerThanPool_StaysCentered()
        {
            var focus = new Vector3(5f, 0f, 5f);
            Vector3 clamped = CameraFraming.ClampFocus(focus, Vector3.zero, HalfLength, HalfWidth, Margin, 30f, 20f);
            Assert.AreEqual(0f, clamped.x, 1e-4f);
            Assert.AreEqual(0f, clamped.z, 1e-4f);
        }

        [Test]
        public void HalfViewDepth_GrowsWithDistance()
        {
            Assert.Greater(CameraFraming.HalfViewDepth(30f, 40f, 60f), CameraFraming.HalfViewDepth(20f, 40f, 60f));
        }
    }
}
