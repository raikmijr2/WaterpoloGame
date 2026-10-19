namespace Waterpulse.Core
{
    // Equipo de un jugador. El local (Home) ataca hacia +X todo el partido
    public enum TeamSide { Home, Away }

    public static class TeamSideExtensions
    {
        public static TeamSide Opponent(this TeamSide side) => side == TeamSide.Home ? TeamSide.Away : TeamSide.Home;

        // Signo del eje X hacia el que ataca el equipo
        public static float AttackSign(this TeamSide side) => side == TeamSide.Home ? 1f : -1f;
    }
}
