using System.Collections.Generic;
using NUnit.Framework;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    public class ComboMultiplierTests
    {
        [Test]
        public void StartsAtOne_GrowsByStep_CapsAtMax()
        {
            var combo = new ComboMultiplier(0.1f, 2f);
            Assert.AreEqual(1f, combo.Value, 1e-5f);
            combo.Advance();
            combo.Advance();
            Assert.AreEqual(1.2f, combo.Value, 1e-5f);
            for (int i = 0; i < 30; i++) combo.Advance();
            Assert.AreEqual(2f, combo.Value, 1e-5f);
        }

        [Test]
        public void Reset_GoesBackToOne()
        {
            var combo = new ComboMultiplier(0.1f, 2f);
            combo.Advance();
            combo.Reset();
            Assert.AreEqual(1f, combo.Value, 1e-5f);
            Assert.AreEqual(0, combo.Steps);
        }
    }

    public class MatchStatsTests
    {
        [Test]
        public void CountsPassesAndAbilities_PerPlayerAndTeam()
        {
            var stats = new MatchStats();
            stats.Register(0, 0);
            stats.Register(1, 0);
            stats.Register(2, 1);
            stats.OnPassCompleted(0);
            stats.OnPassCompleted(1);
            stats.OnPassCompleted(2);
            stats.OnAbilityUsed(0);
            stats.OnAbilityUsed(0);
            stats.OnAbilitySucceeded(0);

            Assert.AreEqual(1, stats.PassesOf(0));
            Assert.AreEqual(2, stats.TeamPasses(0));
            Assert.AreEqual(1, stats.TeamPasses(1));
            Assert.AreEqual(2, stats.TeamAbilitiesUsed(0));
            Assert.AreEqual(1, stats.TeamAbilitiesSucceeded(0));
            Assert.AreEqual(0, stats.TeamAbilitiesSucceeded(1));
        }

        [Test]
        public void UnknownPlayer_IsIgnored()
        {
            var stats = new MatchStats();
            stats.OnPassCompleted(5);
            Assert.AreEqual(0, stats.PassesOf(5));
        }
    }

    public class RecordStorageTests
    {
        // Almacén en memoria en lugar de PlayerPrefs
        class MemoryStore : IRecordStore
        {
            public readonly Dictionary<string, int> Values = new Dictionary<string, int>();
            public bool TryGet(string key, out int value) => Values.TryGetValue(key, out value);
            public void Set(string key, int value) => Values[key] = value;
        }

        const string Key = "test.record";

        [Test]
        public void FirstMatch_IsAlwaysARecord()
        {
            var record = new RecordStorage(new MemoryStore(), Key);
            Assert.IsFalse(record.HasRecord);
            Assert.IsTrue(record.Submit(120));
            Assert.AreEqual(120, record.Best);
        }

        [Test]
        public void HigherScore_ReplacesRecord()
        {
            var record = new RecordStorage(new MemoryStore(), Key);
            record.Submit(120);
            Assert.IsTrue(record.Submit(200));
            Assert.AreEqual(200, record.Best);
        }

        [Test]
        public void LowerOrEqualScore_KeepsRecord()
        {
            var record = new RecordStorage(new MemoryStore(), Key);
            record.Submit(200);
            Assert.IsFalse(record.Submit(150));
            Assert.IsFalse(record.Submit(200));
            Assert.AreEqual(200, record.Best);
        }

        [Test]
        public void Record_PersistsAcrossInstances()
        {
            // Simula recargar la página: otra instancia sobre el mismo almacén
            var store = new MemoryStore();
            new RecordStorage(store, Key).Submit(300);
            Assert.AreEqual(300, new RecordStorage(store, Key).Best);
        }
    }
}
