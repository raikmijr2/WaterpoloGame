using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.EditorTools;

namespace Waterpulse.Tests.EditMode
{
    public class SoundSynthTests
    {
        [Test]
        public void Wav_HasPcmHeaderAndTwoBytesPerSample()
        {
            byte[] wav = SoundSynth.ToWav(new float[100], 44100);
            Assert.AreEqual(44 + 200, wav.Length);
            Assert.AreEqual("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
            Assert.AreEqual("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 22), "mono");
            Assert.AreEqual(44100, BitConverter.ToInt32(wav, 24), "frecuencia de muestreo");
            Assert.AreEqual(16, BitConverter.ToInt16(wav, 34), "16 bits");
            Assert.AreEqual(200, BitConverter.ToInt32(wav, 40), "tamaño de los datos");
        }

        [Test]
        public void Wav_ClipsOutOfRangeSamples()
        {
            byte[] wav = SoundSynth.ToWav(new[] { 2f, -2f });
            Assert.AreEqual(short.MaxValue, BitConverter.ToInt16(wav, 44));
            Assert.AreEqual(-short.MaxValue, BitConverter.ToInt16(wav, 46));
        }

        [Test]
        public void Normalize_SetsThePeak()
        {
            var buffer = new[] { 0.1f, -0.4f, 0.2f };
            SoundSynth.Normalize(buffer, 0.8f);
            Assert.AreEqual(0.8f, buffer.Max(Mathf.Abs), 1e-5f);
        }

        [Test]
        public void Loop_HasTheRequestedLength()
        {
            var buffer = SoundSynth.Buffer(1.5f);
            Assert.AreEqual(SoundSynth.SampleRate, SoundSynth.Loop(buffer, 1f).Length);
        }
    }

    public class SoundGeneratorTests
    {
        [Test]
        public void EnsureSounds_EverySoundHasAFile_AndRealOnesAreNeverOverwritten()
        {
            SoundGenerator.EnsureSounds();
            var paths = SoundGenerator.SoundNames().Select(SoundGenerator.ExistingPath).ToArray();
            foreach (var (name, path) in SoundGenerator.SoundNames().Zip(paths, (n, p) => (n, p)))
                Assert.IsNotNull(path, name);

            var times = paths.Select(File.GetLastWriteTimeUtc).ToArray();
            Assert.AreEqual(0, SoundGenerator.EnsureSounds(), "la segunda vez no crea nada");
            CollectionAssert.AreEqual(times, paths.Select(File.GetLastWriteTimeUtc).ToArray(), "no pisa los que ya existen");
        }
        [Test]
        public void MatchBuild_FillsEveryClipOfTheAudioConfig()
        {
            MatchSceneBuilder.Build();

            var config = AssetDatabase.LoadAssetAtPath<AudioConfigSO>(SoundGenerator.ConfigPath);
            Assert.IsNotNull(config, "AudioConfig.asset");
            var property = new SerializedObject(config).GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script") continue;
                Assert.IsNotNull(property.objectReferenceValue, property.propertyPath);
            }
            var xut = AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(MatchSceneBuilder.XutTragolPath);
            var torpede = AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(MatchSceneBuilder.TorpedeBlauPath);
            CollectionAssert.AreEquivalent(new[] { xut, torpede }, config.abilitySounds.Select(a => a.ability).ToArray(), "un so per habilitat");
        }
    }

    public class AudioVolumesTests
    {
        [TearDown]
        public void Restore() => AudioVolumes.Set(1f, 1f);

        [Test]
        public void Set_ClampsAndNotifies()
        {
            int calls = 0;
            void OnChanged() => calls++;
            AudioVolumes.Changed += OnChanged;
            AudioVolumes.Set(1.5f, -1f);
            AudioVolumes.Changed -= OnChanged;

            Assert.AreEqual(1f, AudioVolumes.Music, 1e-5f);
            Assert.AreEqual(0f, AudioVolumes.Effects, 1e-5f);
            Assert.AreEqual(1, calls);
        }
    }
}
