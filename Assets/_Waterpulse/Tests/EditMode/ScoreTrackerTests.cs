using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    // Puntos del GDD: gol 100, gol con habilidad +50, combo de 3 pases +30, parada 25, intercepción 20, pase 10.
    // Multiplicador: ×1 + 0,1 por acción positiva encadenada (máx. ×2), vuelve a ×1 al perder la posesión
    public class ScoreTrackerTests
    {
        const int Home = 0, Away = 1;
        const int HomeA = 0, HomeB = 1, HomeGk = 2, AwayA = 3, AwayGk = 4;

        ScoreTracker tracker;

        [SetUp]
        public void SetUp()
        {
            tracker = new ScoreTracker(new ScoreTracker.Rules
            {
                Goal = 100, AbilityGoalBonus = 50, ComboPassesRequired = 3, ComboPassesBonus = 30,
                Save = 25, Interception = 20, Recovery = 20, Pass = 10, ComboStep = 0.1f, ComboMax = 2f,
            });
            tracker.Register(HomeA, Home);
            tracker.Register(HomeB, Home);
            tracker.Register(HomeGk, Home);
            tracker.Register(AwayA, Away);
            tracker.Register(AwayGk, Away);
            tracker.OnPossessionChanged(Home);
        }

        [Test]
        public void FirstPass_GivesBasePoints()
        {
            tracker.OnPassCompleted(HomeA);
            Assert.AreEqual(10, tracker.PointsOf(HomeA));
        }

        [Test]
        public void ChainedPasses_GrowWithCombo()
        {
            tracker.OnPassCompleted(HomeA);   // 10 × 1.0
            tracker.OnPassCompleted(HomeB);   // 10 × 1.1 = 11
            tracker.OnPassCompleted(HomeA);   // 10 × 1.2 = 12
            Assert.AreEqual(22, tracker.PointsOf(HomeA));
            Assert.AreEqual(11, tracker.PointsOf(HomeB));
        }

        [Test]
        public void Combo_IsCapped()
        {
            for (int i = 0; i < 20; i++) tracker.OnPassCompleted(HomeA);
            Assert.AreEqual(2f, tracker.ComboOf(Home), 1e-5f);
        }

        [Test]
        public void LosingPossession_ResetsComboAndStreak()
        {
            tracker.OnPassCompleted(HomeA);
            tracker.OnPassCompleted(HomeB);
            tracker.OnPossessionChanged(Away);
            Assert.AreEqual(1f, tracker.ComboOf(Home), 1e-5f);
            Assert.AreEqual(0, tracker.PassStreakOf(Home));
        }

        [Test]
        public void Goal_GivesBasePoints()
        {
            tracker.OnGoal(HomeA, false);
            Assert.AreEqual(100, tracker.PointsOf(HomeA));
        }

        [Test]
        public void Goal_WithAbility_AddsBonus()
        {
            tracker.OnGoal(HomeA, true);
            Assert.AreEqual(150, tracker.PointsOf(HomeA));
        }

        [Test]
        public void Goal_AfterThreePasses_AddsComboBonus_WithMultiplier()
        {
            tracker.OnPassCompleted(HomeB);
            tracker.OnPassCompleted(HomeB);
            tracker.OnPassCompleted(HomeB);
            tracker.OnGoal(HomeA, false);   // (100 + 30) × 1.3 = 169
            Assert.AreEqual(169, tracker.PointsOf(HomeA));
        }

        [Test]
        public void Goal_AfterTwoPasses_HasNoComboBonus()
        {
            tracker.OnPassCompleted(HomeB);
            tracker.OnPassCompleted(HomeB);
            tracker.OnGoal(HomeA, false);   // 100 × 1.2 = 120
            Assert.AreEqual(120, tracker.PointsOf(HomeA));
        }

        [Test]
        public void Interception_Recovery_And_Save()
        {
            tracker.OnPossessionChanged(Away);
            tracker.OnInterception(AwayA);   // 20 × 1.0
            tracker.OnRecovery(AwayA);       // 20 × 1.1 = 22
            tracker.OnPossessionChanged(Home);
            tracker.OnSave(HomeGk);          // 25 × 1.0
            Assert.AreEqual(42, tracker.PointsOf(AwayA));
            Assert.AreEqual(25, tracker.PointsOf(HomeGk));
        }

        [Test]
        public void TeamPoints_SumsOnlyThatTeam()
        {
            tracker.OnPassCompleted(HomeA);
            tracker.OnGoal(HomeB, false);   // 100 × 1.1 = 110
            tracker.OnPossessionChanged(Away);
            tracker.OnRecovery(AwayA);
            Assert.AreEqual(120, tracker.TeamPoints(Home));
            Assert.AreEqual(20, tracker.TeamPoints(Away));
        }

        [Test]
        public void Mvp_IsPlayerWithMostPoints()
        {
            tracker.OnPassCompleted(HomeA);
            tracker.OnPossessionChanged(Away);
            tracker.OnGoal(AwayA, false);
            Assert.AreEqual(AwayA, tracker.Mvp());
        }

        [Test]
        public void UnknownPlayer_IsIgnored()
        {
            tracker.OnPassCompleted(99);
            tracker.OnGoal(99, true);
            Assert.AreEqual(0, tracker.PointsOf(99));
            Assert.AreEqual(0, tracker.TeamPoints(Home));
        }
    }
}
