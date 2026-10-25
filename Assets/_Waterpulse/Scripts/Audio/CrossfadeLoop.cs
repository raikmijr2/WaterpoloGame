using UnityEngine;

namespace Waterpulse.Audio
{
    // Bucle de una grabación que no está pensada para repetirse (el agua o el público de fondo): dos fuentes se turnan
    // y, al acercarse el final, la siguiente arranca desde el principio y se funden, así no se nota el corte.
    // Se guía por el tiempo del clip, que se para con la pausa del listener
    public class CrossfadeLoop
    {
        readonly AudioSource[] sources;
        int current;
        float blend;   // 0 = suena solo la actual; sube hasta 1 durante el fundido con la siguiente
        float level;

        public CrossfadeLoop(AudioSource first, AudioSource second) => sources = new[] { first, second };

        public void Play(AudioClip clip)
        {
            if (clip == null) return;
            foreach (var source in sources)
            {
                source.clip = clip;
                source.loop = false;
            }
            sources[current].Play();
        }

        public void SetLevel(float value)
        {
            level = value;
            Apply();
        }

        public void Update(float crossfade)
        {
            var playing = sources[current];
            if (playing.clip == null || (!playing.isPlaying && blend <= 0f)) return;
            float length = playing.clip.length;
            float fade = Mathf.Clamp(crossfade, 0.01f, length * 0.5f);
            if (blend <= 0f && playing.time < length - fade) return;

            var next = sources[1 - current];
            if (!next.isPlaying)
            {
                next.time = 0f;
                next.Play();
            }
            blend = playing.isPlaying ? Mathf.Clamp01((playing.time - (length - fade)) / fade) : 1f;
            if (blend >= 1f)
            {
                playing.Stop();
                current = 1 - current;
                blend = 0f;
            }
            Apply();
        }

        void Apply()
        {
            sources[current].volume = level * (1f - blend);
            sources[1 - current].volume = level * blend;
        }
    }
}
