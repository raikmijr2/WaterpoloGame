using UnityEngine;

namespace Waterpulse.Config
{
    // Usuarios (solo nombre, sin contraseña) y rànquing de mejores puntuaciones. Se guardan en el navegador
    [CreateAssetMenu(menuName = "Waterpulse/Users Config", fileName = "UsersConfig")]
    public class UsersConfigSO : ScriptableObject
    {
        [Tooltip("Clave con la que se guarda la lista de usuarios en el navegador")]
        public string usersKey = "waterpulse.users";
        [Tooltip("Máximo de usuarios en este navegador")]
        [Min(1)] public int maxUsers = 10;
        [Tooltip("Longitud máxima del nombre (caracteres)")]
        [Min(1)] public int maxNameLength = 12;
        [Tooltip("Líneas que muestra el rànquing")]
        [Min(1)] public int rankingSize = 10;
    }
}
