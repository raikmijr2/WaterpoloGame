using UnityEngine;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Récord guardado con PlayerPrefs. En el build Web persiste en el navegador al recargar la página
    public class PlayerPrefsRecordStore : IRecordStore
    {
        public bool TryGet(string key, out int value)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                value = 0;
                return false;
            }
            value = PlayerPrefs.GetInt(key);
            return true;
        }

        public void Set(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();   // en Web hay que guardar explícitamente
        }
    }
}
