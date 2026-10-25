using NUnit.Framework;
using UnityEngine;
using Waterpulse.Gameplay;

namespace Waterpulse.Tests.EditMode
{
    // Robo solo desde delante del poseedor (arco de 180°): de espaldas protege la pelota
    public class RecoverGeometryTests
    {
        static readonly Vector3 Holder = Vector3.zero;
        static readonly Vector3 FacingRight = Vector3.right;
        const float Arc = 180f;

        [Test]
        public void InFront_CanSteal() => Assert.IsTrue(RecoverGeometry.IsInFront(Holder, FacingRight, new Vector3(1f, 0f, 0f), Arc));

        [Test]
        public void Behind_CannotSteal() => Assert.IsFalse(RecoverGeometry.IsInFront(Holder, FacingRight, new Vector3(-1f, 0f, 0f), Arc));

        [Test]
        public void SlightlyBehindTheSide_CannotSteal() =>
            Assert.IsFalse(RecoverGeometry.IsInFront(Holder, FacingRight, new Vector3(-0.2f, 0f, 1f), Arc));

        [Test]
        public void SlightlyAheadOfTheSide_CanSteal() =>
            Assert.IsTrue(RecoverGeometry.IsInFront(Holder, FacingRight, new Vector3(0.2f, 0f, 1f), Arc));

        [Test]
        public void NarrowerArc_RejectsTheSide() =>
            Assert.IsFalse(RecoverGeometry.IsInFront(Holder, FacingRight, new Vector3(0.2f, 0f, 1f), 90f));

        [Test]
        public void IgnoresHeight() =>
            Assert.IsTrue(RecoverGeometry.IsInFront(Holder, FacingRight, new Vector3(1f, 5f, 0f), Arc));
    }
}
