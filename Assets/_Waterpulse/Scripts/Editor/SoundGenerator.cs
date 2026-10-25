using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Waterpulse.Config;
using static Waterpulse.EditorTools.MatchSceneBuilder;

namespace Waterpulse.EditorTools
{
    // Asigna los sonidos de Audio/ en AudioConfig.asset. Los sonidos son grabaciones reales CC0 (.ogg, .wav o .mp3 con el
    // nombre de cada entrada; créditos en docs/audio-credits.md); solo si falta alguno se sintetiza uno provisional en .wav.
    // Solo rellena los clips vacíos del config: un clip puesto a mano no se pisa. Lo llaman también los generadores de escena
    public static class SoundGenerator
    {
        public const string AudioDir = RootDir + "/Audio";
        public const string ConfigPath = RootDir + "/Config/AudioConfig.asset";
        public const string XutTragolFile = "ability_xut_tragol";
        public const string TorpedeBlauFile = "ability_torpede_blau";
        static readonly string[] Extensions = { ".ogg", ".wav", ".mp3" };

        // Volumen de cada sonido respecto a los demás (pico, 0-1)
        const float Loud = 0.9f;
        const float Medium = 0.7f;
        const float Soft = 0.5f;

        class Sound
        {
            public string File;
            public Func<float[]> Make;
            public Func<AudioConfigSO, AudioClip> Get;
            public Action<AudioConfigSO, AudioClip> Set;
        }

        // Todos los sonidos: archivo, receta y campo de AudioConfigSO (las habilidades van aparte, en abilitySounds)
        static readonly Sound[] Sounds =
        {
            S("music_menu", MenuMusic, c => c.menuMusic, (c, a) => c.menuMusic = a),
            S("music_match", MatchMusic, c => c.matchMusic, (c, a) => c.matchMusic = a),
            S("jingle_win", WinJingle, c => c.winJingle, (c, a) => c.winJingle = a),
            S("jingle_lose", LoseJingle, c => c.loseJingle, (c, a) => c.loseJingle = a),
            S("whistle_short", () => Whistle(1, 0.3f), c => c.whistleShort, (c, a) => c.whistleShort = a),
            S("whistle_long", () => Whistle(2, 0.95f), c => c.whistleLong, (c, a) => c.whistleLong = a),
            S("period_buzzer", Buzzer, c => c.periodBuzzer, (c, a) => c.periodBuzzer = a),
            S("pool_loop", PoolLoop, c => c.poolLoop, (c, a) => c.poolLoop = a),
            S("crowd_loop", CrowdLoop, c => c.crowdLoop, (c, a) => c.crowdLoop = a),
            S("crowd_cheer", Cheer, c => c.crowdCheer, (c, a) => c.crowdCheer = a),
            S("crowd_ooh", Ooh, c => c.crowdOoh, (c, a) => c.crowdOoh = a),
            S("splash_pass", () => Splash(3, 0.3f, 0.8f, Soft, false), c => c.passSplash, (c, a) => c.passSplash = a),
            S("splash_shot", () => Splash(4, 0.45f, 1.3f, Loud, true), c => c.shotSplash, (c, a) => c.shotSplash = a),
            S("catch", Catch, c => c.catchSlap, (c, a) => c.catchSlap = a),
            S("steal", Steal, c => c.steal, (c, a) => c.steal = a),
            S("steal_fail", StealFail, c => c.stealFail, (c, a) => c.stealFail = a),
            S("dash", Dash, c => c.dash, (c, a) => c.dash = a),
            S("bounce", Bounce, c => c.bounce, (c, a) => c.bounce = a),
            S("save", Save, c => c.save, (c, a) => c.save = a),
            S("ui_move", UiMove, c => c.uiMove, (c, a) => c.uiMove = a),
            S("ui_confirm", UiConfirm, c => c.uiConfirm, (c, a) => c.uiConfirm = a),
            S(XutTragolFile, XutTragol, null, null),
            S(TorpedeBlauFile, TorpedeBlau, null, null),
        };

        static Sound S(string file, Func<float[]> make, Func<AudioConfigSO, AudioClip> get, Action<AudioConfigSO, AudioClip> set)
            => new Sound { File = file, Make = make, Get = get, Set = set };

        public static IEnumerable<string> SoundNames()
        {
            foreach (var sound in Sounds) yield return sound.File;
        }

        // El archivo que hay para ese sonido (cualquiera de las extensiones), o null si no hay ninguno
        public static string ExistingPath(string file)
        {
            foreach (string extension in Extensions)
            {
                string path = $"{AudioDir}/{file}{extension}";
                if (File.Exists(path)) return path;
            }
            return null;
        }

        [MenuItem("Waterpulse/Generate Sounds", priority = 2)]
        static void GenerateFromMenu()
        {
            var xut = AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(XutTragolPath);
            var torpede = AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(TorpedeBlauPath);
            int created = EnsureSounds();
            EnsureConfig(xut, torpede);
            Debug.Log($"[Waterpulse] Sonidos asignados en {ConfigPath}. Provisionales sintetizados (faltaba el archivo): {created}.");
        }

        // Crea los sonidos que faltan y el config, y rellena sus clips vacíos
        public static AudioConfigSO EnsureAudio(AbilityDefinitionSO xutTragol, AbilityDefinitionSO torpedeBlau)
        {
            EnsureSounds();
            return EnsureConfig(xutTragol, torpedeBlau);
        }

        // Sintetiza un .wav provisional para cada sonido que no tenga archivo. Devuelve cuántos ha creado
        public static int EnsureSounds()
        {
            EnsureFolder(AudioDir);
            int created = 0;
            foreach (var sound in Sounds)
            {
                if (ExistingPath(sound.File) != null) continue;
                string path = $"{AudioDir}/{sound.File}.wav";
                File.WriteAllBytes(path, SoundSynth.ToWav(sound.Make()));
                AssetDatabase.ImportAsset(path);
                created++;
            }
            return created;
        }

        // Las habilidades sin definición (p. ej. si aún no se ha generado el partido) se quedan para la próxima vez
        static AudioConfigSO EnsureConfig(AbilityDefinitionSO xutTragol, AbilityDefinitionSO torpedeBlau)
        {
            EnsureFolder(RootDir + "/Config");
            var config = LoadOrCreate<AudioConfigSO>(ConfigPath);
            foreach (var sound in Sounds)
                if (sound.Get != null && sound.Get(config) == null) sound.Set(config, Clip(sound.File));

            var abilities = new List<AudioConfigSO.AbilitySound>(config.abilitySounds ?? new AudioConfigSO.AbilitySound[0]);
            AssignAbility(abilities, xutTragol, XutTragolFile);
            AssignAbility(abilities, torpedeBlau, TorpedeBlauFile);
            config.abilitySounds = abilities.ToArray();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        static void AssignAbility(List<AudioConfigSO.AbilitySound> list, AbilityDefinitionSO ability, string file)
        {
            if (ability == null) return;
            var entry = list.Find(e => e.ability == ability);
            if (entry == null)
            {
                entry = new AudioConfigSO.AbilitySound { ability = ability };
                list.Add(entry);
            }
            if (entry.clip == null) entry.clip = Clip(file);
        }

        static AudioClip Clip(string file)
        {
            string path = ExistingPath(file);
            return path != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(path) : null;
        }

        // ---------- Recetas ----------

        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        // Silbato del árbitro: tono agudo con el trino de la bolita
        static float[] Whistle(int seed, float duration)
        {
            var s = new SoundSynth(seed);
            var buffer = SoundSynth.Buffer(duration);
            const float trillHz = 28f, release = 0.04f;
            double phase = 0.0;
            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)SoundSynth.SampleRate;
                float trill = Mathf.Sin(2f * Mathf.PI * trillHz * t);
                phase += 2950.0 * (1.0 + 0.012 * trill) / SoundSynth.SampleRate;
                float tone = SoundSynth.Oscillator(Wave.Sine, phase) + 0.2f * SoundSynth.Oscillator(Wave.Sine, phase * 2.0);
                float env = Mathf.Clamp01(t / 0.012f) * Mathf.Clamp01((duration - t) / release);
                buffer[i] = env * (0.6f + 0.4f * trill) * tone;
            }
            s.Noise(buffer, 0f, duration, 2000f, 6000f, 6000f, 0.15f, 0.01f, duration);   // soplido
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }

        // Bocina del marcador: zumbido electrónico grave y plano
        static float[] Buzzer()
        {
            var s = new SoundSynth(6);
            var buffer = SoundSynth.Buffer(1.3f);
            s.Tone(buffer, 0f, 1.3f, 120f, 120f, 0.5f, Wave.Square, 0.01f, 40f);
            s.Tone(buffer, 0f, 1.3f, 240f, 240f, 0.2f, Wave.Saw, 0.01f, 40f);
            SoundSynth.LowPass(buffer, 3000f);
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }

        // Agua de la piscina, en bucle de 8 s: chapoteo continuo y brazadas sueltas
        static float[] PoolLoop()
        {
            const float length = 8f, overlap = 1f;
            var s = new SoundSynth(25);
            var buffer = SoundSynth.Buffer(length + overlap);
            s.Noise(buffer, 0f, length + overlap, 200f, 2500f, 2500f, 0.4f, 0.4f, 1000f);
            for (int i = 0; i < 40; i++)
            {
                float d = s.Range(0.15f, 0.4f);
                s.Noise(buffer, s.Range(0f, length + overlap - d), d, 400f, 4000f, 1200f, s.Range(0.3f, 0.7f), 0.01f, d * 0.3f);
            }
            var loop = SoundSynth.Loop(buffer, length);
            SoundSynth.Normalize(loop, Medium);
            return loop;
        }

        // Murmullo del público, en bucle de 8 s (las oleadas lentas dan vueltas enteras en ese tiempo)
        static float[] CrowdLoop()
        {
            const float length = 8f, overlap = 1f;
            var s = new SoundSynth(7);
            var buffer = SoundSynth.Buffer(length + overlap);
            s.Noise(buffer, 0f, length + overlap, 120f, 900f, 900f, 0.6f, 0.4f, 1000f);
            for (int i = 0; i < 70; i++)
            {
                float d = s.Range(0.2f, 0.7f), f = s.Range(500f, 1600f);
                s.Noise(buffer, s.Range(0f, length + overlap - d), d, f * 0.5f, f, f, s.Range(0.15f, 0.4f), d * 0.4f, d * 0.4f);
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)SoundSynth.SampleRate;
                buffer[i] *= 0.8f + 0.12f * Mathf.Sin(2f * Mathf.PI * 0.25f * t) + 0.08f * Mathf.Sin(2f * Mathf.PI * 0.375f * t + 1f);
            }
            var loop = SoundSynth.Loop(buffer, length);
            SoundSynth.Normalize(loop, Medium);
            return loop;
        }

        // Ovación: muchas voces "aaah", bullicio y aplausos que crecen y se apagan
        static float[] Cheer()
        {
            var s = new SoundSynth(8);
            var buffer = SoundSynth.Buffer(3f);
            for (int i = 0; i < 28; i++)
            {
                float f = s.Range(170f, 420f);
                s.Tone(buffer, s.Range(0f, 0.25f), 2.7f, f, f * s.Range(0.98f, 1.06f), 0.05f, Wave.Saw, 0.35f, 1.1f, false, s.Range(4f, 6f), 0.01f);
            }
            SoundSynth.LowPass(buffer, 2200f);
            s.Noise(buffer, 0f, 3f, 800f, 6000f, 6000f, 0.5f, 0.3f, 1.1f);
            for (int i = 0; i < 80; i++)
                s.Noise(buffer, s.Range(0.1f, 2.7f), 0.03f, 1500f, 9000f, 9000f, s.Range(0.15f, 0.35f), 0.001f, 0.006f);
            SoundSynth.Normalize(buffer, Loud);
            return buffer;
        }

        // "Oooh" del público: voces graves que bajan
        static float[] Ooh()
        {
            var s = new SoundSynth(9);
            var buffer = SoundSynth.Buffer(1.6f);
            for (int i = 0; i < 18; i++)
            {
                float f = s.Range(150f, 320f);
                s.Tone(buffer, s.Range(0f, 0.1f), 1.5f, f, f * 0.85f, 0.06f, Wave.Triangle, 0.2f, 0.6f, false, s.Range(4f, 6f), 0.01f);
            }
            SoundSynth.LowPass(buffer, 1200f);
            s.Noise(buffer, 0f, 1.5f, 200f, 900f, 600f, 0.2f, 0.2f, 0.5f);
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }

        // Esquitxada: ruido que se oscurece, gotas que suben y, en el tiro, un golpe grave
        static float[] Splash(int seed, float duration, float brightness, float peak, bool thump)
        {
            var s = new SoundSynth(seed);
            var buffer = SoundSynth.Buffer(duration);
            s.Noise(buffer, 0f, duration, 300f, 2500f * brightness, 900f, 0.8f, 0.004f, duration * 0.25f);
            for (int i = 0; i < 10; i++)
            {
                float f = s.Range(900f, 2600f);
                s.Tone(buffer, s.Range(0.02f, duration * 0.6f), 0.05f, f, f * 1.7f, 0.12f, Wave.Sine, 0.002f, 0.015f);
            }
            if (thump) s.Tone(buffer, 0f, 0.18f, 110f, 50f, 0.6f, Wave.Sine, 0.002f, 0.06f);
            SoundSynth.Normalize(buffer, peak);
            return buffer;
        }

        // Palmada en la pelota al recibir un pase
        static float[] Catch()
        {
            var s = new SoundSynth(10);
            var buffer = SoundSynth.Buffer(0.12f);
            s.Noise(buffer, 0f, 0.05f, 900f, 6000f, 3000f, 0.8f, 0.001f, 0.012f);
            s.Tone(buffer, 0f, 0.08f, 260f, 180f, 0.3f, Wave.Sine, 0.002f, 0.025f);
            SoundSynth.Normalize(buffer, Soft);
            return buffer;
        }

        // Robo: palmada fuerte y algo de agua
        static float[] Steal()
        {
            var s = new SoundSynth(12);
            var buffer = SoundSynth.Buffer(0.3f);
            s.Noise(buffer, 0f, 0.06f, 700f, 7000f, 3000f, 1f, 0.001f, 0.015f);
            s.Noise(buffer, 0.02f, 0.28f, 300f, 3000f, 900f, 0.5f, 0.01f, 0.07f);
            s.Tone(buffer, 0f, 0.1f, 300f, 160f, 0.35f, Wave.Sine, 0.002f, 0.03f);
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }

        // Robo fallido: golpe sordo
        static float[] StealFail()
        {
            var s = new SoundSynth(13);
            var buffer = SoundSynth.Buffer(0.25f);
            s.Tone(buffer, 0f, 0.22f, 150f, 60f, 0.7f, Wave.Sine, 0.002f, 0.07f);
            s.Noise(buffer, 0f, 0.1f, 80f, 500f, 300f, 0.4f, 0.002f, 0.03f);
            SoundSynth.Normalize(buffer, Soft);
            return buffer;
        }

        // Dash: soplo de agua que se aclara
        static float[] Dash()
        {
            var s = new SoundSynth(14);
            var buffer = SoundSynth.Buffer(0.26f);
            s.Noise(buffer, 0f, 0.26f, 300f, 500f, 3500f, 1f, 0.08f, 0.08f);
            SoundSynth.Normalize(buffer, Soft);
            return buffer;
        }

        // "Toc" de la pelota contra el palo o la pared
        static float[] Bounce()
        {
            var s = new SoundSynth(15);
            var buffer = SoundSynth.Buffer(0.15f);
            s.Tone(buffer, 0f, 0.14f, 540f, 520f, 0.6f, Wave.Triangle, 0.001f, 0.03f);
            s.Tone(buffer, 0f, 0.07f, 1080f, 1040f, 0.2f, Wave.Sine, 0.001f, 0.015f);
            s.Noise(buffer, 0f, 0.008f, 2000f, 9000f, 9000f, 0.5f, 0.0005f, 0.002f);
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }

        // Parada: golpe grave con palmada
        static float[] Save()
        {
            var s = new SoundSynth(16);
            var buffer = SoundSynth.Buffer(0.32f);
            s.Tone(buffer, 0f, 0.3f, 120f, 55f, 0.8f, Wave.Sine, 0.002f, 0.08f);
            s.Noise(buffer, 0f, 0.06f, 700f, 7000f, 3000f, 0.7f, 0.001f, 0.015f);
            SoundSynth.Normalize(buffer, Loud);
            return buffer;
        }

        // Xut del Tràngol: zumbido que sube y estallido
        static float[] XutTragol()
        {
            var s = new SoundSynth(17);
            var buffer = SoundSynth.Buffer(0.9f);
            s.Noise(buffer, 0f, 0.34f, 200f, 400f, 7000f, 0.7f, 0.25f, 0.05f);
            s.Tone(buffer, 0f, 0.32f, 220f, 880f, 0.15f, Wave.Saw, 0.25f, 0.05f);
            s.Tone(buffer, 0.3f, 0.55f, 80f, 38f, 0.9f, Wave.Sine, 0.002f, 0.15f);
            s.Noise(buffer, 0.3f, 0.35f, 100f, 1500f, 600f, 0.6f, 0.002f, 0.08f);
            SoundSynth.Normalize(buffer, Loud);
            return buffer;
        }

        // Torpede Blau: chorro de agua lleno de burbujas
        static float[] TorpedeBlau()
        {
            var s = new SoundSynth(18);
            var buffer = SoundSynth.Buffer(1f);
            s.Noise(buffer, 0f, 0.95f, 200f, 1800f, 900f, 0.6f, 0.03f, 0.5f);
            for (int i = 0; i < 26; i++)
            {
                float f = s.Range(300f, 800f);
                s.Tone(buffer, s.Range(0f, 0.8f), s.Range(0.04f, 0.07f), f, f * 2.2f, 0.25f, Wave.Sine, 0.003f, 0.02f);
            }
            SoundSynth.Normalize(buffer, Loud);
            return buffer;
        }

        static float[] UiMove()
        {
            var s = new SoundSynth(19);
            var buffer = SoundSynth.Buffer(0.06f);
            s.Tone(buffer, 0f, 0.05f, 1250f, 1250f, 0.5f, Wave.Sine, 0.002f, 0.015f);
            s.Tone(buffer, 0f, 0.03f, 2500f, 2500f, 0.1f, Wave.Sine, 0.002f, 0.01f);
            SoundSynth.Normalize(buffer, Soft);
            return buffer;
        }

        static float[] UiConfirm()
        {
            var s = new SoundSynth(20);
            var buffer = SoundSynth.Buffer(0.18f);
            s.Tone(buffer, 0f, 0.07f, 880f, 880f, 0.5f, Wave.Triangle, 0.002f, 0.03f);
            s.Tone(buffer, 0.06f, 0.11f, 1320f, 1320f, 0.5f, Wave.Triangle, 0.002f, 0.04f);
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }

        // Victoria: arpegio de Do mayor que acaba en acorde
        static float[] WinJingle()
        {
            var s = new SoundSynth(21);
            var buffer = SoundSynth.Buffer(1.9f);
            int[] notes = { 72, 76, 79 };
            for (int i = 0; i < notes.Length; i++)
                s.Tone(buffer, i * 0.1f, 0.12f, Midi(notes[i]), Midi(notes[i]), 0.3f, Wave.Pulse25, 0.003f, 0.08f);
            foreach (int n in new[] { 72, 76, 79, 84 })
            {
                s.Tone(buffer, 0.3f, 1.55f, Midi(n), Midi(n), 0.15f, Wave.Triangle, 0.01f, 0.6f);
                s.Tone(buffer, 0.3f, 1.55f, Midi(n), Midi(n), 0.06f, Wave.Square, 0.01f, 0.4f);
            }
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }

        // Derrota: "trombón triste" que baja medio tono cada vez
        static float[] LoseJingle()
        {
            var s = new SoundSynth(22);
            var buffer = SoundSynth.Buffer(1.9f);
            int[] notes = { 67, 66, 65 };
            for (int i = 0; i < notes.Length; i++)
                s.Tone(buffer, i * 0.3f, 0.28f, Midi(notes[i]), Midi(notes[i]), 0.3f, Wave.Saw, 0.02f, 0.5f);
            s.Tone(buffer, 0.9f, 0.95f, Midi(64), Midi(64), 0.3f, Wave.Saw, 0.02f, 0.6f, false, 6f, 0.02f);
            SoundSynth.LowPass(buffer, 1500f);
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }

        // Música del menú: tranquila (96 BPM), La menor - Fa - Do - Sol
        static float[] MenuMusic() => Music(23, 96f, new[] { 45, 41, 48, 43 }, new[] { true, false, false, false }, false);

        // Música del partido: enérgica (128 BPM), Do - Sol - La menor - Fa
        static float[] MatchMusic() => Music(24, 128f, new[] { 48, 43, 45, 41 }, new[] { false, false, true, false }, true);

        // Chiptune en bucle de 8 compases: bajo, arpegio, colchón o melodía y batería. Todo se escribe "dando la vuelta",
        // así que la cola de las últimas notas suena al principio y el bucle no se nota
        static float[] Music(int seed, float bpm, int[] roots, bool[] minor, bool energetic)
        {
            const int bars = 8;
            var s = new SoundSynth(seed);
            float beat = 60f / bpm;
            var buffer = SoundSynth.Buffer(bars * 4 * beat);
            for (int bar = 0; bar < bars; bar++)
            {
                int root = roots[bar % roots.Length];
                int[] chord = { 0, minor[bar % roots.Length] ? 3 : 4, 7, 12 };
                float barStart = bar * 4 * beat;

                int bassSteps = energetic ? 8 : 4;
                float bassStep = 4 * beat / bassSteps;
                for (int k = 0; k < bassSteps; k++)
                {
                    float f = Midi(root + (energetic && k % 2 == 1 ? 12 : 0));
                    s.Tone(buffer, barStart + k * bassStep, bassStep * 0.9f, f, f, 0.22f, Wave.Triangle, 0.005f, bassStep * 0.6f, true);
                }

                int arpSteps = energetic ? 16 : 8;
                float arpStep = 4 * beat / arpSteps;
                for (int k = 0; k < arpSteps; k++)
                {
                    float f = Midi(root + 12 + chord[k % chord.Length]);
                    s.Tone(buffer, barStart + k * arpStep, arpStep * 0.85f, f, f, 0.08f, Wave.Pulse25, 0.003f, arpStep * 0.5f, true);
                }

                if (energetic)
                {
                    // Melodía en negras cada dos compases
                    if (bar % 2 == 1)
                        for (int b = 0; b < 4; b++)
                        {
                            float f = Midi(root + 24 + chord[(b * 2 + bar) % 3]);
                            s.Tone(buffer, barStart + b * beat, beat * 0.8f, f, f, 0.07f, Wave.Square, 0.01f, beat * 0.4f, true);
                        }
                }
                else
                {
                    // Colchón suave con el acorde entero
                    for (int i = 0; i < 3; i++)
                    {
                        float f = Midi(root + 12 + chord[i]);
                        s.Tone(buffer, barStart, 4 * beat, f, f, 0.035f, Wave.Triangle, 0.3f, 3f, true);
                    }
                }

                for (int b = 0; b < 4; b++)
                {
                    float t = barStart + b * beat;
                    if (b == 0 || b == 2) s.Tone(buffer, t, 0.14f, 130f, 45f, 0.5f, Wave.Sine, 0.001f, 0.08f, true);              // bombo
                    if (energetic && (b == 1 || b == 3)) s.Noise(buffer, t, 0.12f, 1200f, 7000f, 5000f, 0.35f, 0.001f, 0.05f, true);   // caja
                    for (int h = 0; h < 2; h++)
                        s.Noise(buffer, t + h * beat * 0.5f, 0.04f, 6000f, 14000f, 14000f, energetic ? 0.12f : 0.06f, 0.001f, 0.015f, true); // charles
                }
            }
            SoundSynth.Normalize(buffer, Medium);
            return buffer;
        }
    }
}
