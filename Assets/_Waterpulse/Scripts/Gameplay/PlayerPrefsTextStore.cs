using UnityEngine;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Textos guardados con PlayerPrefs (la lista de usuarios). En el build Web persisten en el navegador
    public class PlayerPrefsTextStore : ITextStore
    {
        public bool TryGetString(string key, out string value)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                value = null;
                return false;
            }
            value = PlayerPrefs.GetString(key);
            return true;
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();   // en Web hay que guardar explícitamente
        }
    }
}
