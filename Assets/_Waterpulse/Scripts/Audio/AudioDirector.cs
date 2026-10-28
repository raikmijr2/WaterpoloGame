using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Audio
{
    public enum AudioScene { Menu, Match }

    public enum UiSoundKind { Move, Confirm }

    // Sonido del juego: música en bucle (menú o partido), agua de la piscina y público de fondo en el partido, efectos al
    // escuchar GameEvents (silbatos, bocina del marcador, agua, goles, paradas, habilidades…), jingle del resumen y sonidos
    // de la interfaz (UiSound). Como en el waterpolo: el árbitro pita los goles y la bocina marca el final del tiempo.
    // Volumen = mezcla de AudioConfigSO × volumen de Opcions (AudioVolumes); el general es AudioListener.volume.
    // En la pausa se paran el público y los efectos; la música y la interfaz siguen. Sonido 2D: la cámara es cenital
    public class AudioDirector : MonoBehaviour
    {
        [SerializeField] AudioConfigSO config;
        [SerializeField] AudioScene scene;
        [Tooltip("Equipo del jugador: el público celebra sus goles y se lamenta de los del rival")]
        [SerializeField] TeamSide playerTeam = TeamSide.Home;

        static AudioDirector current;

        AudioSource music;
        AudioSource ui;
        AudioSource comment;
        CrossfadeLoop pool;
        CrossfadeLoop crowd;
        AudioSource[] voices;
        float[] voiceLimit;       // duración máxima de lo que suena en cada voz (0 = entero)
        float[] voiceGain;        // baja hasta 0 al apagar lo que pasa de su duración máxima
        int nextVoice;
        MatchState lastState;
        GameObject controlled;    // jugador que controla el humano
        int playerGoals;
        int rivalGoals;
        float musicFade = 1f;     // 1 = sin fundido; baja hasta 0 al acabar el partido
        bool fadingOut;
        float lastUiSound = float.NegativeInfinity;

        void Awake()
        {
            AudioListener.pause = false;
            music = CreateSource("Music", true, true);
            ui = CreateSource("Interface", false, true);
            comment = CreateSource("Comment", false, false);
            pool = new CrossfadeLoop(CreateSource("Pool0", false, false), CreateSource("Pool1", false, false));
            crowd = new CrossfadeLoop(CreateSource("Crowd0", false, false), CreateSource("Crowd1", false, false));
            voices = new AudioSource[Mathf.Max(1, config.effectVoices)];
            voiceLimit = new float[voices.Length];
            voiceGain = new float[voices.Length];
            for (int i = 0; i < voices.Length; i++) voices[i] = CreateSource("Effect" + i, false, false);
        }

        void OnEnable()
        {
            current = this;
            AudioVolumes.Changed += ApplyVolumes;
            GameEvents.MatchStateChanged += OnMatchStateChanged;
            GameEvents.Goal += OnGoal;
            GameEvents.Save += OnSave;
            GameEvents.GoldenGoalStarted += OnGoldenGoal;
            GameEvents.PassThrown += OnPassThrown;
            GameEvents.ShotTaken += OnShotTaken;
            GameEvents.PassCompleted += OnPassCompleted;
            GameEvents.Interception += OnInterception;
            GameEvents.BallRecovered += OnBallRecovered;
            GameEvents.RecoverFailed += OnRecoverFailed;
            GameEvents.DashUsed += OnDashUsed;
            GameEvents.BallBounced += OnBallBounced;
            GameEvents.AbilityUsed += OnAbilityUsed;
            GameEvents.AbilityNoEnergy += OnAbilityNoEnergy;
            GameEvents.ControlledPlayerChanged += OnControlledChanged;
            GameEvents.PauseChanged += OnPauseChanged;
        }

        void OnDisable()
        {
            if (current == this) current = null;
            AudioVolumes.Changed -= ApplyVolumes;
            GameEvents.MatchStateChanged -= OnMatchStateChanged;
            GameEvents.Goal -= OnGoal;
            GameEvents.Save -= OnSave;
            GameEvents.GoldenGoalStarted -= OnGoldenGoal;
            GameEvents.PassThrown -= OnPassThrown;
            GameEvents.ShotTaken -= OnShotTaken;
            GameEvents.PassCompleted -= OnPassCompleted;
            GameEvents.Interception -= OnInterception;
            GameEvents.BallRecovered -= OnBallRecovered;
            GameEvents.RecoverFailed -= OnRecoverFailed;
            GameEvents.DashUsed -= OnDashUsed;
            GameEvents.BallBounced -= OnBallBounced;
            GameEvents.AbilityUsed -= OnAbilityUsed;
            GameEvents.AbilityNoEnergy -= OnAbilityNoEnergy;
            GameEvents.ControlledPlayerChanged -= OnControlledChanged;
            GameEvents.PauseChanged -= OnPauseChanged;
            AudioListener.pause = false;   // si se sale de la escena desde la pausa, la siguiente no debe empezar muda
        }

        void Start()
        {
            ApplyVolumes();
            var track = scene == AudioScene.Menu ? config.menuMusic : config.matchMusic;
            if (track != null)
            {
                music.clip = track;
                music.Play();
            }
            if (scene == AudioScene.Match)
            {
                pool.Play(config.poolLoop);
                crowd.Play(config.crowdLoop);
            }
        }

        void Update()
        {
            pool.Update(config.ambienceCrossfade);
            crowd.Update(config.ambienceCrossfade);
            UpdateVoices();
            if (!fadingOut) return;
            musicFade = config.musicFadeOut > 0f ? Mathf.Max(0f, musicFade - Time.unscaledDeltaTime / config.musicFadeOut) : 0f;
            ApplyVolumes();
            if (musicFade > 0f) return;
            music.Stop();
            fadingOut = false;
        }

        // Sonido de la interfaz (lo piden los UiSound de los botones)
        public static void PlayUi(UiSoundKind kind)
        {
            if (current != null) current.PlayInterface(kind);
        }

        void PlayInterface(UiSoundKind kind)
        {
            if (Time.unscaledTime - lastUiSound < config.uiMinInterval) return;
            var clip = kind == UiSoundKind.Move ? config.uiMove : config.uiConfirm;
            if (clip == null) return;
            lastUiSound = Time.unscaledTime;
            ui.PlayOneShot(clip);
        }

        void ApplyVolumes()
        {
            music.volume = config.musicLevel * AudioVolumes.Music * musicFade;
            ui.volume = config.uiLevel * AudioVolumes.Effects;
            comment.volume = config.commentLevel * AudioVolumes.Effects;
            pool.SetLevel(config.poolLevel * AudioVolumes.Effects);
            crowd.SetLevel(config.crowdLevel * AudioVolumes.Effects);
            for (int i = 0; i < voices.Length; i++) ApplyVoiceVolume(i);
        }

        void ApplyVoiceVolume(int i) => voices[i].volume = config.effectsLevel * AudioVolumes.Effects * voiceGain[i];

        // Las voces con duración máxima se apagan con un fundido al pasarla (el tiempo del clip se para en la pausa)
        void UpdateVoices()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voiceLimit[i] <= 0f || !voices[i].isPlaying || voices[i].time < voiceLimit[i]) continue;
                float fade = Mathf.Max(0.01f, config.crowdReactionFadeOut);
                voiceGain[i] = Mathf.Clamp01(1f - (voices[i].time - voiceLimit[i]) / fade);
                ApplyVoiceVolume(i);
                if (voiceGain[i] > 0f) continue;
                voices[i].Stop();
                voiceLimit[i] = 0f;
            }
        }

        // ---------- Partido ----------

        void OnMatchStateChanged(MatchState state)
        {
            switch (state)
            {
                case MatchState.Playing:
                    if (lastState == MatchState.Kickoff) Effect(config.whistleShort);   // empieza el juego tras "Preparats…"
                    break;
                case MatchState.FullTime:
                    Effect(config.periodBuzzer, false);
                    Comment(config.fullTimeComments);
                    fadingOut = true;
                    break;
                case MatchState.Results:
                    if (playerGoals != rivalGoals) Effect(playerGoals > rivalGoals ? config.winJingle : config.loseJingle, false);
                    break;
            }
            lastState = state;
        }

        void OnGoal(TeamSide scoringTeam, GameObject scorer)
        {
            bool ours = scoringTeam == playerTeam;
            if (ours) playerGoals++;
            else rivalGoals++;
            Effect(config.whistleShort);   // el árbitro pita el gol
            CrowdReaction(ours ? config.crowdCheer : config.crowdOoh);
            Comment(config.goalComments);
        }

        void OnSave(GameObject goalkeeper)
        {
            Effect(config.save);
            CrowdReaction(config.crowdOoh);
            Comment(config.saveComments);
        }

        // Se acaba el tiempo con empate: bocina y, al poco, silbato largo para el gol de oro
        void OnGoldenGoal()
        {
            Effect(config.periodBuzzer, false);
            Effect(config.whistleLong, true, config.goldenGoalWhistleDelay);
            Comment(config.goldenGoalComments);
        }

        void OnPassThrown(GameObject passer) => Effect(config.passSplash);
        void OnShotTaken(GameObject shooter) => Effect(config.shotSplash);
        void OnPassCompleted(GameObject passer, GameObject receiver) => Effect(config.catchSlap);
        void OnInterception(GameObject interceptor, GameObject passer) => Effect(config.steal);
        void OnBallRecovered(GameObject player) => Effect(config.steal);
        void OnRecoverFailed(GameObject player) => Effect(config.stealFail);
        void OnDashUsed(GameObject player) => Effect(config.dash);
        void OnBallBounced() => Effect(config.bounce);

        void OnAbilityUsed(GameObject player, string abilityName)
        {
            foreach (var entry in config.abilitySounds)
            {
                if (entry.ability == null || entry.ability.displayName != abilityName) continue;
                Effect(entry.clip);
                return;
            }
        }

        void OnControlledChanged(GameObject player) => controlled = player;

        // Solo el del humano: a la IA no hace falta avisarla
        void OnAbilityNoEnergy(GameObject player)
        {
            if (player != null && player == controlled) Effect(config.abilityNoEnergy, false);
        }

        // La música y la interfaz siguen en la pausa (ignoran la pausa del listener); el resto se para
        void OnPauseChanged(bool paused) => AudioListener.pause = paused;

        // ---------- Utilidades ----------

        // Las grabaciones del público son largas: suenan como mucho crowdReactionMaxDuration y se apagan
        void CrowdReaction(AudioClip clip) => Effect(clip, true, 0f, config.crowdReactionMaxDuration);

        // Efecto en la siguiente voz, con un poco de variación de tono (no en lo que tiene melodía: jingles y bocina).
        // "delay" lo retrasa; "maxDuration" (> 0) lo apaga con un fundido al pasar ese tiempo
        void Effect(AudioClip clip, bool varyPitch = true, float delay = 0f, float maxDuration = 0f)
        {
            if (clip == null) return;
            int i = nextVoice;
            nextVoice = (nextVoice + 1) % voices.Length;
            var voice = voices[i];
            voice.pitch = varyPitch ? 1f + Random.Range(-config.pitchVariation, config.pitchVariation) : 1f;
            voice.clip = clip;
            voiceLimit[i] = maxDuration;
            voiceGain[i] = 1f;
            ApplyVoiceVolume(i);
            if (delay > 0f) voice.PlayDelayed(delay);
            else voice.Play();
        }

        void Comment(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;
            comment.clip = clip;
            comment.Play();
        }

        AudioSource CreateSource(string name, bool loop, bool ignorePause)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = ignorePause;
            return source;
        }
    }
}
