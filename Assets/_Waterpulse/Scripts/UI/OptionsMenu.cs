using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Gameplay;
using Waterpulse.Logic;

namespace Waterpulse.UI
{
    // Opciones del menú: volumen general (se guarda y se aplica a todo el audio del juego), pantalla completa
    // y récord personal (se ve y se puede borrar). Textos visibles en catalán
    public class OptionsMenu : MonoBehaviour
    {
        [SerializeField] MenuConfigSO config;
        [SerializeField] ScoringConfigSO scoringConfig;
        [SerializeField] Slider volumeSlider;
        [SerializeField] Text volumeLabel;
        [SerializeField] Button fullscreenButton;
        [SerializeField] Text fullscreenLabel;
        [SerializeField] Text recordLabel;
        [SerializeField] Button resetRecordButton;

        [Header("Textos")]
        [SerializeField] string volumeFormat = "Volum general: {0} %";
        [SerializeField] string fullscreenOnText = "Pantalla completa: Sí";
        [SerializeField] string fullscreenOffText = "Pantalla completa: No";
        [SerializeField] string recordFormat = "Rècord personal: {0} punts";
        [SerializeField] string noRecordText = "Encara no hi ha rècord";

        VolumeSetting volume;
        RecordStorage record;
        float saveTimer = -1f;   // < 0: no hay ningún cambio de volumen pendiente de guardar

        void Awake()
        {
            volume = new VolumeSetting(new PlayerPrefsSettingsStore(), config.volumeKey, config.defaultVolume);
            record = new RecordStorage(new PlayerPrefsRecordStore(), scoringConfig.recordKey);

            AudioListener.volume = volume.Value;   // el menú es la primera escena: aquí se aplica el volumen guardado
            volumeSlider.SetValueWithoutNotify(volume.Value);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            fullscreenButton.onClick.AddListener(ToggleFullscreen);
            RefreshVolume(volume.Value);
            RefreshRecord();
        }

        void Update()
        {
            // El navegador puede salir de la pantalla completa por su cuenta (Esc): el texto sigue el estado real
            fullscreenLabel.text = Screen.fullScreen ? fullscreenOnText : fullscreenOffText;

            if (saveTimer < 0f) return;
            saveTimer -= Time.unscaledDeltaTime;
            if (saveTimer < 0f) volume.Set(volumeSlider.value);
        }

        // Al salir del menú no se pierde el último cambio
        void OnDestroy()
        {
            if (saveTimer >= 0f) volume.Set(volumeSlider.value);
        }

        public void ResetRecord()
        {
            record.Clear();
            RefreshRecord();
        }

        // El volumen cambia al momento; se guarda cuando el deslizador lleva un rato quieto
        void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            RefreshVolume(value);
            saveTimer = config.volumeSaveDelay;
        }

        void ToggleFullscreen() => Screen.fullScreen = !Screen.fullScreen;

        void RefreshVolume(float value) => volumeLabel.text = string.Format(volumeFormat, Mathf.RoundToInt(value * 100f));

        void RefreshRecord()
        {
            recordLabel.text = record.HasRecord ? string.Format(recordFormat, record.Best) : noRecordText;
            resetRecordButton.interactable = record.HasRecord;
        }
    }
}
