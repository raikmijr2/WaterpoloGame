using System.Collections.Generic;
using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    // Puntos del GDD: gol 100, gol con habilidad +50, parada 25, intercepción y robo 20, pase 10.
    // Multiplicador de posesión (solo multiplica el gol): +0,1 por pase, +0,2 al recuperar, rachas de 3 y 5 pases
    // (+0,2 y +0,3), máximo ×3; vuelve a ×1 al perder la pelota y se gasta al marcar.
    // Contraataque: +50 si se marca a menos de 5 s de recuperar
    public class ScoreTrackerTests
    {
        const int Home = 0, Away = 1;
        const int HomeA = 0, HomeB = 1, HomeGk = 2, AwayA = 3, AwayGk = 4;

        ScoreTracker tracker;
        List<ScoreEvent> events;

        [SetUp]
        public void SetUp()
        {
            tracker = new ScoreTracker(new ScoreTracker.Rules
            {
                Goal = 100, AbilityGoalBonus = 50, Save = 25, Interception = 20, Recovery = 20, Pass = 10,
                PassMultiplier = 0.1f, RecoveryMultiplier = 0.2f, MultiplierMax = 3f,
                ShortStreak = 3, ShortStreakBonus = 0.2f, LongStreak = 5, LongStreakBonus = 0.3f,
                CounterAttackWindow = 5f, CounterAttackBonus = 50,
            });
            tracker.Register(HomeA, Home);
            tracker.Register(HomeB, Home);
            tracker.Register(HomeGk, Home);
            tracker.Register(AwayA, Away);
            tracker.Register(AwayGk, Away);
            tracker.OnPossessionChanged(Home);
            events = new List<ScoreEvent>();
            tracker.Scored += events.Add;
        }

        [Test]
        public void Passes_GiveFixedPoints_AndGrowTheMultiplier()
        {
            tracker.OnPassCompleted(HomeA);
            tracker.OnPassCompleted(HomeA);
            Assert.AreEqual(20, tracker.PointsOf(HomeA), "los pases no se multiplican");
            Assert.AreEqual(1.2f, tracker.MultiplierOf(Home), 1e-5f);
        }

        [Test]
        public void Streaks_AddExtraMultiplier_AtThreeAndFivePasses()
        {
            for (int i = 0; i < 3; i++) tracker.OnPassCompleted(HomeA);
            Assert.AreEqual(1.5f, tracker.MultiplierOf(Home), 1e-5f, "3 pases (+0,3) y la racha de 3 (+0,2)");
            tracker.OnPassCompleted(HomeA);
            tracker.OnPassCompleted(HomeA);
            Assert.AreEqual(2f, tracker.MultiplierOf(Home), 1e-5f, "5 pases (+0,5), racha de 3 (+0,2) y de 5 (+0,3)");
            Assert.AreEqual(2, events.FindAll(e => e.Kind == ScoreKind.Streak).Count);
        }

        [Test]
        public void Multiplier_IsCapped()
        {
            for (int i = 0; i < 40; i++) tracker.OnPassCompleted(HomeA);
            Assert.AreEqual(3f, tracker.MultiplierOf(Home), 1e-5f);
        }

        [Test]
        public void Goal_IsMultiplied_AndSpendsTheMultiplier()
        {
            tracker.OnPassCompleted(HomeB);
            tracker.OnPassCompleted(HomeB);
            tracker.OnGoal(HomeA, false, 10f);   // 100 × 1,2
            Assert.AreEqual(120, tracker.PointsOf(HomeA));
            Assert.AreEqual(1f, tracker.MultiplierOf(Home), 1e-5f, "al marcar se gasta");
            var goal = events.Find(e => e.Kind == ScoreKind.Goal);
            Assert.AreEqual(100, goal.GoalBase);
            Assert.AreEqual(1.2f, goal.Multiplier, 1e-5f);
        }

        [Test]
        public void AbilityGoal_MultipliesTheBonusToo()
        {
            for (int i = 0; i < 3; i++) tracker.OnPassCompleted(HomeB);   // ×1,5
            tracker.OnGoal(HomeA, true, 10f);                             // 150 × 1,5 = 225
            Assert.AreEqual(225, tracker.PointsOf(HomeA));
        }

        [Test]
        public void LosingTheBall_ResetsMultiplierAndStreak_AndWarns()
        {
            tracker.OnPassCompleted(HomeA);
            tracker.OnPassCompleted(HomeB);
            tracker.OnPossessionChanged(Away);
            Assert.AreEqual(1f, tracker.MultiplierOf(Home), 1e-5f);
            Assert.AreEqual(0, tracker.PassStreakOf(Home));
            var lost = events.Find(e => e.Kind == ScoreKind.MultiplierLost);
            Assert.AreEqual(Home, lost.Team);
            Assert.AreEqual(1.2f, lost.Multiplier, 1e-5f);
        }

        [Test]
        public void LosingTheBall_AtTimesOne_DoesNotWarn()
        {
            tracker.OnPossessionChanged(Away);
            Assert.IsFalse(events.Exists(e => e.Kind == ScoreKind.MultiplierLost));
        }

        [Test]
        public void Recovery_GivesPointsAndMultiplier()
        {
            tracker.OnPossessionChanged(Away);
            tracker.OnRecovery(AwayA, 0f);
            Assert.AreEqual(20, tracker.PointsOf(AwayA));
            Assert.AreEqual(1.2f, tracker.MultiplierOf(Away), 1e-5f);
        }

        [Test]
        public void CounterAttack_QuickGoalAfterRecovering_GivesBonus()
        {
            tracker.OnPossessionChanged(Away);
            tracker.OnInterception(AwayA, 10f);   // 20, ×1,2
            tracker.OnGoal(AwayA, false, 14f);    // 120 y contraataque +50
            Assert.AreEqual(20 + 120 + 50, tracker.PointsOf(AwayA));
            Assert.IsTrue(events.Exists(e => e.Kind == ScoreKind.CounterAttack));
        }

        [Test]
        public void SlowGoalAfterRecovering_HasNoCounterAttack()
        {
            tracker.OnPossessionChanged(Away);
            tracker.OnRecovery(AwayA, 10f);
            tracker.OnGoal(AwayA, false, 16f);
            Assert.AreEqual(20 + 120, tracker.PointsOf(AwayA));
        }

        [Test]
        public void GoalWithoutRecovering_HasNoCounterAttack()
        {
            tracker.OnGoal(HomeA, false, 1f);
            Assert.AreEqual(100, tracker.PointsOf(HomeA));
        }

        [Test]
        public void Save_GivesPoints_AndOpensCounterAttack()
        {
            tracker.OnPossessionChanged(Away);
            tracker.OnPossessionChanged(Home);
            tracker.OnSave(HomeGk, 20f);
            tracker.OnGoal(HomeA, false, 23f);
            Assert.AreEqual(25, tracker.PointsOf(HomeGk));
            Assert.AreEqual(150, tracker.PointsOf(HomeA), "gol a ×1 y contraataque");
        }

        [Test]
        public void TeamPoints_SumsOnlyThatTeam()
        {
            tracker.OnPassCompleted(HomeA);
            tracker.OnGoal(HomeB, false, 5f);   // 100 × 1,1 = 110
            tracker.OnPossessionChanged(Away);
            tracker.OnRecovery(AwayA, 6f);
            Assert.AreEqual(120, tracker.TeamPoints(Home));
            Assert.AreEqual(20, tracker.TeamPoints(Away));
        }

        [Test]
        public void Mvp_IsPlayerWithMostPoints()
        {
            tracker.OnPassCompleted(HomeA);
            tracker.OnPossessionChanged(Away);
            tracker.OnGoal(AwayA, false, 1f);
            Assert.AreEqual(AwayA, tracker.Mvp());
        }

        [Test]
        public void UnknownPlayer_IsIgnored()
        {
            tracker.OnPassCompleted(99);
            tracker.OnGoal(99, true, 0f);
            Assert.AreEqual(0, tracker.PointsOf(99));
            Assert.AreEqual(0, tracker.TeamPoints(Home));
            Assert.AreEqual(0, events.Count);
        }
    }
}
