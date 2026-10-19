using UnityEngine;

namespace Waterpulse.Core
{
    // Lo que un jugador quiere hacer este frame, venga de un humano o de la IA
    public struct PlayerIntent
    {
        public Vector2 Move;   // dirección en el plano del agua (x = X del mundo, y = Z del mundo), magnitud 0-1
        public bool Pass;
        public bool Shoot;
        public bool Ability;

        public static PlayerIntent None => default;
    }
}
