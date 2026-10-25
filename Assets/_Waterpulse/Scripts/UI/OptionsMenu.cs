using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;
using Waterpulse.Logic;

namespace Waterpulse.UI
{
    // Opciones (en el menú principal y en la pausa): volúmenes general, de música y de efectos, que se guardan,
    // y pantalla completa. El general es AudioListener.volume; música y efectos van a AudioVolumes, que lee el sonido.
    // Textos visibles en catalán
    public class OptionsMenu : MonoBehaviour
    {
        [SerializeField] MenuConfigSO config;
        [SerializeField] Slider masterSlider;
        [SerializeField] Text masterLabel;
        [SerializeField] Slider musicSlider;
        [SerializeField] Text musicLabel;
        [SerializeField] Slider effectsSlider;
        [SerializeField] Text effectsLabel;
        [SerializeField] Button fullscreenButton;
        [SerializeField] Text fullscreenLabel;

        [Header("Textos")]
        [SerializeField] string masterFormat = "General: {0} %";
        [SerializeField] string musicFormat = "Música: {0} %";
        [SerializeField] string effectsFormat = "Efectes: {0} %";
        [SerializeField] string fullscreenOnText = "Pantalla completa: Sí";
        [SerializeField] string fullscreenOffText = "Pantalla completa: No";

        // Un volumen: su deslizador, su texto y lo guardado. Cambia al momento y se guarda cuando el deslizador
        // lleva un rato quieto, para no escribir en el navegador a cada paso
        class VolumeRow
        {
            public Slider Slider;
            public Text Label;
            public string Format;
            public VolumeSetting Setting;
            public float SaveTimer = -1f;   // < 0: nada pendiente de guardar

            public void Refresh() => Label.text = string.Format(Format, Mathf.RoundToInt(Slider.value * 100f));

            public void Tick(float dt)
            {
                if (SaveTimer < 0f) return;
                SaveTimer -= dt;
                if (SaveTimer < 0f) Setting.Set(Slider.value);
            }

            public void Flush()
            {
                if (SaveTimer >= 0f) Setting.Set(Slider.value);
            }
        }

        VolumeRow master;
        VolumeRow music;
        VolumeRow effects;

        void Awake()
        {
            var store = new PlayerPrefsSettingsStore();
            master = CreateRow(masterSlider, masterLabel, masterFormat, new VolumeSetting(store, config.volumeKey, config.defaultVolume));
            music = CreateRow(musicSlider, musicLabel, musicFormat, new VolumeSetting(store, config.musicVolumeKey, config.defaultMusicVolume));
            effects = CreateRow(effectsSlider, effectsLabel, effectsFormat, new VolumeSetting(store, config.effectsVolumeKey, config.defaultEffectsVolume));
            Apply();   // se aplican los volúmenes guardados al abrir la escena
            fullscreenButton.onClick.AddListener(ToggleFullscreen);
        }

        void Update()
        {
            // El navegador puede salir de la pantalla completa por su cuenta (Esc): el texto sigue el estado real
            fullscreenLabel.text = Screen.fullScreen ? fullscreenOnText : fullscreenOffText;

            // Tiempo sin escalar: en la pausa del partido el tiempo está parado
            float dt = Time.unscaledDeltaTime;
            master.Tick(dt);
            music.Tick(dt);
            effects.Tick(dt);
        }

        // Al salir de la escena no se pierde el último cambio
        void OnDestroy()
        {
            master?.Flush();
            music?.Flush();
            effects?.Flush();
        }

        VolumeRow CreateRow(Slider slider, Text label, string format, VolumeSetting setting)
        {
            var row = new VolumeRow { Slider = slider, Label = label, Format = format, Setting = setting };
            slider.SetValueWithoutNotify(setting.Value);
            slider.onValueChanged.AddListener(_ =>
            {
                row.SaveTimer = config.volumeSaveDelay;
                row.Refresh();
                Apply();
            });
            row.Refresh();
            return row;
        }

        void Apply()
        {
            AudioListener.volume = master.Slider.value;
            AudioVolumes.Set(music.Slider.value, effects.Slider.value);
        }

        void ToggleFullscreen() => Screen.fullScreen = !Screen.fullScreen;
    }
}
