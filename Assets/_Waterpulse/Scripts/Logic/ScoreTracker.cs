using System;
using System.Collections.Generic;
using UnityEngine;

namespace Waterpulse.Logic
{
    // Qué ha dado puntos o ha cambiado el multiplicador (para los avisos en pantalla)
    public enum ScoreKind { Pass, Recovery, Save, Streak, Goal, CounterAttack, MultiplierLost }

    public struct ScoreEvent
    {
        public ScoreKind Kind;
        public int Player;          // -1 en MultiplierLost
        public int Team;
        public int Points;          // puntos ganados (0 si solo cambia el multiplicador)
        public float Multiplier;    // multiplicador del equipo después (en Goal y MultiplierLost, el que se ha gastado o perdido)
        public int Streak;          // pases seguidos (Pass, Streak)
        public int GoalBase;        // Goal: puntos antes de multiplicar (gol, o gol con habilidad)
        public bool WithAbility;    // Goal: con el Xut del Tràngol
    }

    // Puntos por jugador (GDD sección 8). Los puntos miden quién juega mejor; nunca deciden el resultado.
    // Multiplicador de posesión por equipo (decidido por el usuario): empieza en ×1 y sube con cada pase completado, con
    // las rachas de pases y al recuperar la pelota, hasta un máximo. Solo multiplica el gol: es "lo que vale el próximo gol".
    // Al perder la pelota vuelve a ×1, y al marcar se gasta. Alternativa: el contraataque (marcar poco después de
    // recuperar) da un extra fijo. Jugadores = identificadores enteros; equipos = 0 (local) y 1 (visitante)
    public class ScoreTracker
    {
        public struct Rules
        {
            public int Goal;
            public int AbilityGoalBonus;
            public int Save;
            public int Interception;
            public int Recovery;
            public int Pass;
            public float PassMultiplier;        // lo que sube el multiplicador por pase completado
            public float RecoveryMultiplier;    // lo que sube al recuperar (intercepción o robo)
            public float MultiplierMax;
            public int ShortStreak;             // pases seguidos de la primera racha
            public float ShortStreakBonus;
            public int LongStreak;              // pases seguidos de la segunda racha
            public float LongStreakBonus;
            public float CounterAttackWindow;   // s desde la recuperación (o la parada)
            public int CounterAttackBonus;
        }

        const int TeamCount = 2;

        readonly Rules rules;
        readonly Dictionary<int, int> points = new Dictionary<int, int>();
        readonly Dictionary<int, int> teamOf = new Dictionary<int, int>();
        readonly List<int> order = new List<int>();
        readonly float[] multiplier = new float[TeamCount];
        readonly int[] passStreak = new int[TeamCount];
        readonly float[] recoveredAt = new float[TeamCount];
        int possessionTeam = -1;

        // Cada vez que alguien gana puntos o cambia el multiplicador (lo escuchan los avisos del HUD)
        public event Action<ScoreEvent> Scored;

        public ScoreTracker(Rules rules)
        {
            this.rules = rules;
            for (int i = 0; i < TeamCount; i++) ResetTeam(i);
        }

        public void Register(int player, int team)
        {
            if (teamOf.ContainsKey(player)) return;
            teamOf[player] = team;
            points[player] = 0;
            order.Add(player);
        }

        public int PointsOf(int player) => points.TryGetValue(player, out int p) ? p : 0;
        public float MultiplierOf(int team) => multiplier[team];
        public int PassStreakOf(int team) => passStreak[team];

        public int TeamPoints(int team)
        {
            int total = 0;
            foreach (var pair in teamOf)
                if (pair.Value == team) total += points[pair.Key];
            return total;
        }

        // Jugador con más puntos (en empate, el primero registrado). -1 si no hay jugadores
        public int Mvp()
        {
            int best = -1, bestPoints = int.MinValue;
            foreach (int player in order)
            {
                if (points[player] <= bestPoints) continue;
                bestPoints = points[player];
                best = player;
            }
            return best;
        }

        // Al cambiar de equipo la posesión, el que la pierde pierde su multiplicador y su racha
        public void OnPossessionChanged(int team)
        {
            if (team == possessionTeam) return;
            if (possessionTeam >= 0)
            {
                float lost = multiplier[possessionTeam];
                if (lost > 1f) Raise(new ScoreEvent { Kind = ScoreKind.MultiplierLost, Player = -1, Team = possessionTeam, Multiplier = lost });
                ResetTeam(possessionTeam);
            }
            possessionTeam = team;
        }

        public void OnPassCompleted(int passer)
        {
            if (!teamOf.TryGetValue(passer, out int team)) return;
            passStreak[team]++;
            points[passer] += rules.Pass;
            Grow(team, rules.PassMultiplier);
            Raise(new ScoreEvent { Kind = ScoreKind.Pass, Player = passer, Team = team, Points = rules.Pass, Multiplier = multiplier[team], Streak = passStreak[team] });

            float streakBonus = passStreak[team] == rules.LongStreak ? rules.LongStreakBonus
                              : passStreak[team] == rules.ShortStreak ? rules.ShortStreakBonus : 0f;
            if (streakBonus <= 0f) return;
            Grow(team, streakBonus);
            Raise(new ScoreEvent { Kind = ScoreKind.Streak, Player = passer, Team = team, Multiplier = multiplier[team], Streak = passStreak[team] });
        }

        public void OnInterception(int player, float time) => Recover(player, rules.Interception, time);
        public void OnRecovery(int player, float time) => Recover(player, rules.Recovery, time);

        // La parada también abre el contraataque, pero no sube el multiplicador
        public void OnSave(int goalkeeper, float time)
        {
            if (!teamOf.TryGetValue(goalkeeper, out int team)) return;
            points[goalkeeper] += rules.Save;
            recoveredAt[team] = time;
            Raise(new ScoreEvent { Kind = ScoreKind.Save, Player = goalkeeper, Team = team, Points = rules.Save, Multiplier = multiplier[team] });
        }

        // Gol (no en propia puerta): (base + extra si fue con habilidad) × multiplicador, y el extra del contraataque
        // si llega poco después de recuperar. El multiplicador se gasta. Devuelve los puntos del gol (los que cuentan
        // para ganar el partido), o 0 si el jugador no existe
        public int OnGoal(int scorer, bool withAbility, float time)
        {
            if (!teamOf.TryGetValue(scorer, out int team)) return 0;
            int goalBase = rules.Goal + (withAbility ? rules.AbilityGoalBonus : 0);
            float used = multiplier[team];
            int goalPoints = Mathf.RoundToInt(goalBase * used);
            points[scorer] += goalPoints;
            Raise(new ScoreEvent { Kind = ScoreKind.Goal, Player = scorer, Team = team, Points = goalPoints, Multiplier = used,
                                   GoalBase = goalBase, WithAbility = withAbility });

            int total = goalPoints;
            if (time - recoveredAt[team] <= rules.CounterAttackWindow)
            {
                points[scorer] += rules.CounterAttackBonus;
                total += rules.CounterAttackBonus;
                Raise(new ScoreEvent { Kind = ScoreKind.CounterAttack, Player = scorer, Team = team, Points = rules.CounterAttackBonus, Multiplier = used });
            }
            ResetTeam(team);
            return total;
        }

        void Recover(int player, int reward, float time)
        {
            if (!teamOf.TryGetValue(player, out int team)) return;
            points[player] += reward;
            recoveredAt[team] = time;
            Grow(team, rules.RecoveryMultiplier);
            Raise(new ScoreEvent { Kind = ScoreKind.Recovery, Player = player, Team = team, Points = reward, Multiplier = multiplier[team] });
        }

        void Grow(int team, float amount) => multiplier[team] = Mathf.Min(rules.MultiplierMax, multiplier[team] + amount);

        void ResetTeam(int team)
        {
            multiplier[team] = 1f;
            passStreak[team] = 0;
            recoveredAt[team] = float.NegativeInfinity;
        }

        void Raise(ScoreEvent e) => Scored?.Invoke(e);
    }
}
