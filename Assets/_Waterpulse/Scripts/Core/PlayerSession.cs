using UnityEngine;

namespace Waterpulse.Core
{
    // Quién juega en esta sesión (hasta recargar la página): un usuario registrado o un convidat.
    // Se mantiene entre escenas: el menú lo elige ("Qui juga?") y el partido lo lee para guardar el récord
    public static class PlayerSession
    {
        public const int GuestId = -1;

        public static int UserId { get; private set; } = GuestId;
        public static string UserName { get; private set; }
        public static bool HasChosen { get; private set; }
        public static bool IsGuest => UserId == GuestId;

        public static void SignIn(int userId, string userName)
        {
            UserId = userId;
            UserName = userName;
            HasChosen = true;
        }

        public static void SignInAsGuest()
        {
            UserId = GuestId;
            UserName = null;
            HasChosen = true;
        }

        // Sin nadie elegido (p. ej. tras borrar el usuario con el que se jugaba)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void SignOut()
        {
            UserId = GuestId;
            UserName = null;
            HasChosen = false;
        }
    }
}
