namespace Waterpulse.Core
{
    // Fuente de intenciones de un jugador (humano o IA). PlayerController no sabe cuál es
    public interface IPlayerBrain
    {
        PlayerIntent GetIntent();
    }
}
