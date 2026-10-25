using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Waterpulse.EditorTools
{
    public enum Wave { Sine, Triangle, Square, Pulse25, Saw }

    // Síntesis sencilla para los sonidos provisionales del juego: osciladores, ruido filtrado (filtros de un polo),
    // envolventes y escritura en WAV de 16 bits mono. Determinista: la misma semilla da el mismo sonido
    public class SoundSynth
    {
        public const int SampleRate = 44100;
        const float EdgeFade = 0.003f;   // fundido mínimo al final de cada nota, para que no haga "clic"

        readonly System.Random rng;

        public SoundSynth(int seed) => rng = new System.Random(seed);

        public static float[] Buffer(float seconds) => new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate))];

        public float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        float WhiteNoise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        public static float LowPassCoefficient(float cutoff) => 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / SampleRate);

        // Valor de la onda en una fase medida en ciclos
        public static float Oscillator(Wave wave, double phase)
        {
            float p = (float)(phase - Math.Floor(phase));
            switch (wave)
            {
                case Wave.Sine: return Mathf.Sin(2f * Mathf.PI * p);
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(p - 0.5f);
                case Wave.Square: return p < 0.5f ? 1f : -1f;
                case Wave.Pulse25: return p < 0.25f ? 1f : -1f;
                default: return 2f * p - 1f;   // diente de sierra
            }
        }

        // Subida lineal durante "attack" y caída exponencial (constante "decay"); se apaga del todo al final de "duration"
        public static float Envelope(float t, float duration, float attack, float decay)
        {
            if (t < 0f || t >= duration) return 0f;
            float env = t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);
            float tail = duration - t;
            return tail < EdgeFade ? env * tail / EdgeFade : env;
        }

        // Nota que se desliza de "from" a "to" Hz. Con wrap, lo que pasa del final vuelve al principio (para los bucles)
        public void Tone(float[] buffer, float start, float duration, float from, float to, float amplitude, Wave wave,
                         float attack, float decay, bool wrap = false, float vibratoHz = 0f, float vibratoDepth = 0f)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            double step = Math.Pow(to / from, 1.0 / Math.Max(1, count));
            double freq = from;
            double phase = 0.0;
            for (int k = 0; k < count; k++)
            {
                int index = first + k;
                if (wrap) index %= buffer.Length;
                else if (index >= buffer.Length) break;
                float t = k / (float)SampleRate;
                double f = vibratoDepth > 0f ? freq * (1.0 + vibratoDepth * Math.Sin(2.0 * Math.PI * vibratoHz * t)) : freq;
                phase += f / SampleRate;
                freq *= step;
                buffer[index] += amplitude * Envelope(t, duration, attack, decay) * Oscillator(wave, phase);
            }
        }

        // Ruido entre "lowCut" y un corte alto que se desliza de "highCut" a "highCutEnd" (Hz)
        public void Noise(float[] buffer, float start, float duration, float lowCut, float highCut, float highCutEnd,
                          float amplitude, float attack, float decay, bool wrap = false)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            float lowA = LowPassCoefficient(lowCut);
            float low = 0f, high = 0f;
            for (int k = 0; k < count; k++)
            {
                int index = first + k;
                if (wrap) index %= buffer.Length;
                else if (index >= buffer.Length) break;
                float t = k / (float)SampleRate;
                float x = WhiteNoise();
                low += lowA * (x - low);
                float cut = highCut * Mathf.Pow(highCutEnd / highCut, t / duration);
                high += LowPassCoefficient(cut) * (x - low - high);
                buffer[index] += amplitude * Envelope(t, duration, attack, decay) * high;
            }
        }

        public static void LowPass(float[] buffer, float cutoff)
        {
            float a = LowPassCoefficient(cutoff), y = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                y += a * (buffer[i] - y);
                buffer[i] = y;
            }
        }

        // Escala para que el pico valga "peak" (0-1): así cada sonido tiene el volumen que le toca respecto a los demás
        public static void Normalize(float[] buffer, float peak)
        {
            float max = 0f;
            foreach (float s in buffer) max = Mathf.Max(max, Mathf.Abs(s));
            if (max <= 0f) return;
            float gain = peak / max;
            for (int i = 0; i < buffer.Length; i++) buffer[i] *= gain;
        }

        // Bucle sin cortes: lo que sobra tras "length" segundos se funde con el principio
        public static float[] Loop(float[] buffer, float length)
        {
            int n = Mathf.Min(buffer.Length, Mathf.RoundToInt(length * SampleRate));
            int fade = Mathf.Min(buffer.Length - n, n);
            var loop = new float[n];
            Array.Copy(buffer, loop, n);
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                loop[i] = buffer[i] * w + buffer[n + i] * (1f - w);
            }
            return loop;
        }

        // WAV PCM de 16 bits, mono. Lo que pase de ±1 se recorta
        public static byte[] ToWav(float[] samples, int sampleRate = SampleRate)
        {
            const int bytesPerSample = 2;
            int dataLength = samples.Length * bytesPerSample;
            using (var stream = new MemoryStream(44 + dataLength))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);                               // tamaño del bloque fmt
                writer.Write((short)1);                         // PCM
                writer.Write((short)1);                         // mono
                writer.Write(sampleRate);
                writer.Write(sampleRate * bytesPerSample);      // bytes por segundo
                writer.Write((short)bytesPerSample);            // bytes por muestra
                writer.Write((short)16);                        // bits por muestra
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);
                foreach (float s in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
                return stream.ToArray();
            }
        }
    }
}
