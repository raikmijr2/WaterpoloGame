using UnityEngine;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Ajustes guardados con PlayerPrefs. En el build Web persisten en el navegador al recargar la página
    public class PlayerPrefsSettingsStore : ISettingsStore
    {
        public bool TryGetFloat(string key, out float value)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                value = 0f;
                return false;
            }
            value = PlayerPrefs.GetFloat(key);
            return true;
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();   // en Web hay que guardar explícitamente
        }
    }
}
