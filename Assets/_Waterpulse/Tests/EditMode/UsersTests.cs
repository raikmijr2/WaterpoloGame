using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Waterpulse.Core;
using Waterpulse.Logic;

namespace Waterpulse.Tests.EditMode
{
    public class UserRegistryTests
    {
        // Almacén en memoria en lugar de PlayerPrefs
        class MemoryStore : ITextStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool TryGetString(string key, out string value) => Values.TryGetValue(key, out value);
            public void SetString(string key, string value) => Values[key] = value;
        }

        const string Key = "test.users";
        const int MaxUsers = 3;
        const int MaxNameLength = 12;

        static UserRegistry NewRegistry(MemoryStore store = null) => new UserRegistry(store ?? new MemoryStore(), Key, MaxUsers, MaxNameLength);

        [Test]
        public void Create_TrimsName_AndPersistsAcrossInstances()
        {
            // Simula recargar la página: otra instancia sobre el mismo almacén
            var store = new MemoryStore();
            Assert.AreEqual(NameCheck.Ok, NewRegistry(store).TryCreate("  Núria  ", out var user));
            Assert.AreEqual("Núria", user.Name);

            var reloaded = NewRegistry(store);
            Assert.AreEqual(1, reloaded.Users.Count);
            Assert.AreEqual("Núria", reloaded.Users[0].Name);
            Assert.AreEqual(user.Id, reloaded.Users[0].Id);
        }

        [Test]
        public void InvalidNames_AreRejected()
        {
            var registry = NewRegistry();
            Assert.AreEqual(NameCheck.Empty, registry.TryCreate("   ", out var none));
            Assert.IsNull(none);
            Assert.AreEqual(NameCheck.Empty, registry.TryCreate(null, out _));
            Assert.AreEqual(NameCheck.TooLong, registry.TryCreate(new string('a', MaxNameLength + 1), out _));
            Assert.AreEqual(NameCheck.Ok, registry.TryCreate(new string('a', MaxNameLength), out _), "justo en el límite");
            Assert.AreEqual(NameCheck.InvalidCharacters, registry.TryCreate("Jor\tdi", out _), "carácter de control");
            Assert.AreEqual(NameCheck.InvalidCharacters, registry.TryCreate("<b>Jo", out _), "rompería el formato de los textos");
            Assert.AreEqual(1, registry.Users.Count, "solo se ha creado el válido");
        }

        [Test]
        public void Duplicate_IgnoresCase()
        {
            var registry = NewRegistry();
            registry.TryCreate("Jordi", out _);
            Assert.AreEqual(NameCheck.Duplicate, registry.TryCreate("JORDI", out _));
            Assert.AreEqual(NameCheck.Duplicate, registry.TryCreate(" jordi ", out _));
            registry.TryCreate("Núria", out _);
            Assert.AreEqual(NameCheck.Duplicate, registry.TryCreate("NÚRIA", out _), "también con accents");
            Assert.IsNotNull(registry.Find("jOrDi"));
        }

        [Test]
        public void Full_RejectsNewUsers()
        {
            var registry = NewRegistry();
            for (int i = 0; i < MaxUsers; i++) registry.TryCreate("Usuari" + i, out _);
            Assert.IsTrue(registry.IsFull);
            Assert.AreEqual(NameCheck.Full, registry.TryCreate("Un més", out _));
        }

        [Test]
        public void Remove_FreesTheName_ButNeverReusesTheId()
        {
            var store = new MemoryStore();
            var registry = NewRegistry(store);
            registry.TryCreate("Jordi", out var first);
            Assert.IsTrue(registry.Remove(first.Id));
            Assert.IsFalse(registry.Remove(first.Id), "ya no está");
            Assert.IsNull(registry.Get(first.Id));

            registry.TryCreate("Jordi", out var again);
            Assert.AreNotEqual(first.Id, again.Id, "el récord del usuario borrado no pasa al nuevo");
            Assert.AreNotEqual(first.Id, NewRegistry(store).Users.Single().Id, "tampoco tras recargar");
        }

        [Test]
        public void Load_IgnoresBrokenLines()
        {
            var store = new MemoryStore();
            store.Values[Key] = "5\n1\tJordi\nbasura\n2\t\n3\tjordi\n4\tNúria";
            var registry = NewRegistry(store);
            CollectionAssert.AreEqual(new[] { "Jordi", "Núria" }, registry.Users.Select(u => u.Name).ToArray());
            registry.TryCreate("Pau", out var user);
            Assert.AreEqual(5, user.Id, "sigue a partir del próximo id guardado");
        }
    }

    public class LeaderboardTests
    {
        [Test]
        public void Rank_SortsByPoints_TiesShareThePositionInAlphabeticalOrder()
        {
            var ranking = Leaderboard.Rank(new[]
            {
                new RankingEntry(1, "Pau", 300),
                new RankingEntry(2, "Anna", 500),
                new RankingEntry(3, "Jordi", 300),
                new RankingEntry(4, "Marc", 100),
            });

            CollectionAssert.AreEqual(new[] { "Anna", "Jordi", "Pau", "Marc" }, ranking.Select(e => e.Name).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 2, 4 }, ranking.Select(e => e.Position).ToArray());
        }

        [Test]
        public void PositionOf_UserWithoutRecordIsZero()
        {
            var ranking = Leaderboard.Rank(new[] { new RankingEntry(1, "Pau", 300), new RankingEntry(2, "Anna", 500) });
            Assert.AreEqual(2, Leaderboard.PositionOf(ranking, 1));
            Assert.AreEqual(1, Leaderboard.PositionOf(ranking, 2));
            Assert.AreEqual(0, Leaderboard.PositionOf(ranking, 9));
            Assert.AreEqual(0, Leaderboard.Rank(new RankingEntry[0]).Count);
        }
    }

    public class PlayerSessionTests
    {
        [TearDown]
        public void SignOut() => PlayerSession.SignOut();

        [Test]
        public void SignIn_Guest_AndSignOut()
        {
            PlayerSession.SignOut();
            Assert.IsFalse(PlayerSession.HasChosen);

            PlayerSession.SignIn(3, "Jordi");
            Assert.IsTrue(PlayerSession.HasChosen);
            Assert.IsFalse(PlayerSession.IsGuest);
            Assert.AreEqual("Jordi", PlayerSession.UserName);

            PlayerSession.SignInAsGuest();
            Assert.IsTrue(PlayerSession.HasChosen);
            Assert.IsTrue(PlayerSession.IsGuest);
        }
    }
}
