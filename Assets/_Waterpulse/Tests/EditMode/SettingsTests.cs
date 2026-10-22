using System.Collections.Generic;
using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    public class VolumeSettingTests
    {
        // Almacén en memoria en lugar de PlayerPrefs
        class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void SetFloat(string key, float value) => Values[key] = value;
        }

        const string Key = "test.volume";

        [Test]
        public void NeverSaved_UsesDefault()
        {
            var volume = new VolumeSetting(new MemoryStore(), Key, 0.8f);
            Assert.AreEqual(0.8f, volume.Value, 1e-5f);
        }

        [Test]
        public void Set_PersistsAcrossInstances()
        {
            // Simula recargar la página: otra instancia sobre el mismo almacén
            var store = new MemoryStore();
            new VolumeSetting(store, Key, 1f).Set(0.35f);
            Assert.AreEqual(0.35f, new VolumeSetting(store, Key, 1f).Value, 1e-5f);
        }

        [Test]
        public void Set_ClampsBetweenZeroAndOne()
        {
            var store = new MemoryStore();
            var volume = new VolumeSetting(store, Key, 1f);
            volume.Set(1.7f);
            Assert.AreEqual(1f, volume.Value, 1e-5f);
            volume.Set(-0.2f);
            Assert.AreEqual(0f, volume.Value, 1e-5f);
            Assert.AreEqual(0f, store.Values[Key], 1e-5f, "se guarda ya recortado");
        }

        [Test]
        public void OutOfRangeValues_AreClamped()
        {
            var store = new MemoryStore();
            store.Values[Key] = 3f;   // valor guardado fuera de rango (a mano o de otra versión)
            Assert.AreEqual(1f, new VolumeSetting(store, Key, 0.5f).Value, 1e-5f, "valor guardado");
            Assert.AreEqual(1f, new VolumeSetting(new MemoryStore(), Key, 2f).Value, 1e-5f, "valor por defecto");
        }
    }
}
