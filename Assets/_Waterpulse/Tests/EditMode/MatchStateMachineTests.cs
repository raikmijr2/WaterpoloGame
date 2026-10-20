using System.Collections.Generic;
using NUnit.Framework;
using Waterpulse.Core;

namespace Waterpulse.Tests.EditMode
{
    // Flujo: Kickoff → Playing → GoalScored → Kickoff … → FullTime → Results, con gol de oro si hay empate
    public class MatchStateMachineTests
    {
        const float Duration = 10f;
        const float Delay = 1f;

        MatchStateMachine machine;
        List<MatchState> history;

        [SetUp]
        public void SetUp()
        {
            machine = new MatchStateMachine(new MatchStateMachine.Settings
            {
                MatchDuration = Duration, KickoffDelay = Delay, GoalDelay = Delay, FullTimeDelay = Delay,
            });
            history = new List<MatchState>();
            machine.StateChanged += s => history.Add(s);
        }

        void StartPlaying()
        {
            machine.Begin(TeamSide.Home);
            machine.Tick(Delay + 0.01f);
        }

        [Test]
        public void Begins_InKickoff_ForGivenTeam()
        {
            machine.Begin(TeamSide.Home);
            Assert.AreEqual(MatchState.Kickoff, machine.State);
            Assert.AreEqual(TeamSide.Home, machine.KickoffTeam);
        }

        [Test]
        public void Kickoff_TurnsIntoPlaying_AfterDelay()
        {
            machine.Begin(TeamSide.Home);
            machine.Tick(Delay * 0.5f);
            Assert.AreEqual(MatchState.Kickoff, machine.State);
            machine.Tick(Delay);
            Assert.AreEqual(MatchState.Playing, machine.State);
        }

        [Test]
        public void Clock_DoesNotRunDuringKickoff()
        {
            machine.Begin(TeamSide.Home);
            machine.Tick(Delay * 0.5f);
            Assert.AreEqual(Duration, machine.Timer.Remaining, 1e-5f);
        }

        [Test]
        public void Goal_CountsAndGivesKickoffToConcedingTeam()
        {
            StartPlaying();
            Assert.IsTrue(machine.RegisterGoal(TeamSide.Home));
            Assert.AreEqual(1, machine.HomeGoals);
            Assert.AreEqual(MatchState.GoalScored, machine.State);

            machine.Tick(Delay + 0.01f);
            Assert.AreEqual(MatchState.Kickoff, machine.State);
            Assert.AreEqual(TeamSide.Away, machine.KickoffTeam);
        }

        [Test]
        public void Clock_StopsAfterGoal()
        {
            StartPlaying();
            machine.Tick(2f);
            machine.RegisterGoal(TeamSide.Away);
            float remaining = machine.Timer.Remaining;
            machine.Tick(Delay * 0.5f);
            Assert.AreEqual(remaining, machine.Timer.Remaining, 1e-5f);
        }

        [Test]
        public void Goals_OutsidePlaying_AreIgnored()
        {
            machine.Begin(TeamSide.Home);
            Assert.IsFalse(machine.RegisterGoal(TeamSide.Home));
            Assert.AreEqual(0, machine.HomeGoals);
        }

        [Test]
        public void TimeUp_WithLeader_GoesToFullTimeThenResults()
        {
            StartPlaying();
            machine.RegisterGoal(TeamSide.Away);
            machine.Tick(Delay + 0.01f);   // saque
            machine.Tick(Delay + 0.01f);   // juego
            machine.Tick(Duration);        // se acaba el tiempo
            Assert.AreEqual(MatchState.FullTime, machine.State);
            machine.Tick(Delay + 0.01f);
            Assert.AreEqual(MatchState.Results, machine.State);
            Assert.AreEqual(TeamSide.Away, machine.Winner);
        }

        [Test]
        public void TimeUp_WithDraw_StartsGoldenGoal_AndKeepsPlaying()
        {
            int goldenGoalEvents = 0;
            machine.GoldenGoalStarted += () => goldenGoalEvents++;
            StartPlaying();
            machine.Tick(Duration);
            Assert.IsTrue(machine.IsGoldenGoal);
            Assert.AreEqual(MatchState.Playing, machine.State);
            Assert.AreEqual(1, goldenGoalEvents);
        }

        [Test]
        public void GoldenGoal_NextGoalEndsTheMatch()
        {
            StartPlaying();
            machine.Tick(Duration);
            machine.RegisterGoal(TeamSide.Home);
            machine.Tick(Delay + 0.01f);
            Assert.AreEqual(MatchState.FullTime, machine.State);
            Assert.AreEqual(TeamSide.Home, machine.Winner);
        }

        [Test]
        public void GoalThatLevels_AtTheBuzzer_LeadsToGoldenGoal()
        {
            StartPlaying();
            machine.RegisterGoal(TeamSide.Home);
            machine.Tick(Delay + 0.01f);   // saque del visitante
            machine.Tick(Delay + 0.01f);   // juego
            machine.Tick(Duration - 0.5f);
            machine.RegisterGoal(TeamSide.Away);   // empate cuando quedan 0,5 s
            machine.Tick(0.5f);                    // el reloj está parado: no se agota
            Assert.IsFalse(machine.IsGoldenGoal);
            machine.Tick(Delay);                   // saque
            Assert.AreEqual(MatchState.Kickoff, machine.State);
        }

        [Test]
        public void EmitsStatesInOrder()
        {
            StartPlaying();
            machine.RegisterGoal(TeamSide.Home);
            machine.Tick(Delay + 0.01f);
            CollectionAssert.AreEqual(
                new[] { MatchState.Kickoff, MatchState.Playing, MatchState.GoalScored, MatchState.Kickoff },
                history);
        }
    }
}
