using System;
using System.Collections.Generic;
using System.Text;

namespace Waterpulse.Logic
{
    // Almacén clave-texto. En el juego lo implementa PlayerPrefs (persiste en el navegador); en los tests, uno en memoria
    public interface ITextStore
    {
        bool TryGetString(string key, out string value);
        void SetString(string key, string value);
    }

    // Usuario del juego: solo un nombre, sin contraseña. El id no cambia nunca y no se reutiliza al borrar
    public class UserProfile
    {
        public int Id { get; }
        public string Name { get; }

        public UserProfile(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    // Resultado de comprobar un nombre nuevo
    public enum NameCheck { Ok, Full, Empty, TooLong, InvalidCharacters, Duplicate }

    // Lista de usuarios guardada en un único texto: la primera línea es el próximo id y cada una de las demás,
    // "id<TAB>nombre". Los nombres no pueden tener caracteres de control, así que nunca rompen el formato.
    // Nombre válido: de 1 a maxNameLength caracteres (sin contar espacios al principio y al final) y sin repetir,
    // sin distinguir mayúsculas
    public class UserRegistry
    {
        const char LineSeparator = '\n';
        const char FieldSeparator = '\t';

        readonly ITextStore store;
        readonly string key;
        readonly int maxUsers;
        readonly int maxNameLength;
        readonly List<UserProfile> users = new List<UserProfile>();
        int nextId;

        public UserRegistry(ITextStore store, string key, int maxUsers, int maxNameLength)
        {
            this.store = store;
            this.key = key;
            this.maxUsers = maxUsers;
            this.maxNameLength = maxNameLength;
            Load();
        }

        public IReadOnlyList<UserProfile> Users => users;
        public bool IsFull => users.Count >= maxUsers;

        // El nombre tal como se guardará: sin espacios al principio ni al final
        public static string Clean(string name) => name == null ? string.Empty : name.Trim();

        public NameCheck Check(string name)
        {
            if (IsFull) return NameCheck.Full;
            string clean = Clean(name);
            if (clean.Length == 0) return NameCheck.Empty;
            if (clean.Length > maxNameLength) return NameCheck.TooLong;
            foreach (char c in clean)
                if (!IsValidCharacter(c)) return NameCheck.InvalidCharacters;
            if (Find(clean) != null) return NameCheck.Duplicate;
            return NameCheck.Ok;
        }

        // Crea y guarda el usuario si el nombre es válido. "user" queda a null si no se ha podido
        public NameCheck TryCreate(string name, out UserProfile user)
        {
            user = null;
            var check = Check(name);
            if (check != NameCheck.Ok) return check;
            user = new UserProfile(nextId++, Clean(name));
            users.Add(user);
            Save();
            return NameCheck.Ok;
        }

        public bool Remove(int id)
        {
            int index = users.FindIndex(u => u.Id == id);
            if (index < 0) return false;
            users.RemoveAt(index);
            Save();
            return true;
        }

        // Sin caracteres de control (romperían el formato guardado) ni "<" ">" (el nombre sale en textos con formato de color)
        static bool IsValidCharacter(char c) => !char.IsControl(c) && c != '<' && c != '>';

        public UserProfile Get(int id) => users.Find(u => u.Id == id);

        // Busca sin distinguir mayúsculas: "Jordi" y "JORDI" son el mismo usuario
        public UserProfile Find(string name)
        {
            string clean = Clean(name);
            return users.Find(u => string.Equals(u.Name, clean, StringComparison.OrdinalIgnoreCase));
        }

        void Load()
        {
            users.Clear();
            nextId = 0;
            if (!store.TryGetString(key, out string data) || string.IsNullOrEmpty(data)) return;

            string[] lines = data.Split(LineSeparator);
            int.TryParse(lines[0], out nextId);
            for (int i = 1; i < lines.Length; i++)
            {
                // Las líneas mal formadas o repetidas se ignoran: un dato roto no tumba la lista entera
                int tab = lines[i].IndexOf(FieldSeparator);
                if (tab <= 0 || !int.TryParse(lines[i].Substring(0, tab), out int id)) continue;
                string name = Clean(lines[i].Substring(tab + 1));
                if (name.Length == 0 || Get(id) != null || Find(name) != null) continue;
                users.Add(new UserProfile(id, name));
                nextId = Math.Max(nextId, id + 1);
            }
        }

        void Save()
        {
            var text = new StringBuilder();
            text.Append(nextId);
            foreach (var u in users)
                text.Append(LineSeparator).Append(u.Id).Append(FieldSeparator).Append(u.Name);
            store.SetString(key, text.ToString());
        }
    }
}
