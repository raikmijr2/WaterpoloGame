using UnityEngine;

namespace Waterpulse.Logic
{
    // Almacén clave-valor de los ajustes. En el juego lo implementa PlayerPrefs (persiste en el navegador);
    // en los tests, uno en memoria
    public interface ISettingsStore
    {
        bool TryGetFloat(string key, out float value);
        void SetFloat(string key, float value);
    }

    // Volumen general entre 0 y 1. Mientras no se haya guardado nunca, vale el de por defecto
    public class VolumeSetting
    {
        readonly ISettingsStore store;
        readonly string key;
        readonly float defaultValue;

        public VolumeSetting(ISettingsStore store, string key, float defaultValue)
        {
            this.store = store;
            this.key = key;
            this.defaultValue = Mathf.Clamp01(defaultValue);
        }

        public float Value => store.TryGetFloat(key, out float value) ? Mathf.Clamp01(value) : defaultValue;

        public void Set(float value) => store.SetFloat(key, Mathf.Clamp01(value));
    }
}
