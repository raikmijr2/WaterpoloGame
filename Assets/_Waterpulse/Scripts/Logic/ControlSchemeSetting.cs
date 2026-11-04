namespace Waterpulse.Logic
{
    // Controles elegidos en Opcions: solo teclado, o teclado más ratón (clic izquierdo tira, clic derecho pasa,
    // doble clic súper xut). Se guarda con el resto de ajustes; mientras no se haya elegido nunca, ratón
    public class ControlSchemeSetting
    {
        const float KeyboardValue = 0f;
        const float MouseValue = 1f;

        readonly ISettingsStore store;
        readonly string key;

        public ControlSchemeSetting(ISettingsStore store, string key)
        {
            this.store = store;
            this.key = key;
        }

        public bool UseMouse => !store.TryGetFloat(key, out float value) || value >= MouseValue;

        public void Set(bool useMouse) => store.SetFloat(key, useMouse ? MouseValue : KeyboardValue);
    }
}
