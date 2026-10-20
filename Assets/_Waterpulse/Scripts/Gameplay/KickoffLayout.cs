using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Posición de cada jugador en un saque: los de campo según su rol en su propio campo y el portero
    // delante de su portería. Lo usan el generador de escena y MatchFlow
    public static class KickoffLayout
    {
        public static Vector3 Position(PlayerRole role, TeamSide team, Vector3 center, float halfLength, GameConfigSO cfg)
        {
            float sign = team.AttackSign();
            if (role == PlayerRole.Goalkeeper)
                return center + Vector3.right * (sign * (cfg.goalkeeperDepth - halfLength));

            Vector2 slot = role == PlayerRole.Attacker ? cfg.attackerKickoff
                         : role == PlayerRole.Playmaker ? cfg.playmakerKickoff
                         : cfg.defenderKickoff;
            // El visitante es el local reflejado respecto al centro
            return center + new Vector3(slot.x * sign, 0f, slot.y * sign);
        }

        public static Vector3 Facing(TeamSide team) => Vector3.right * team.AttackSign();
    }
}
