namespace Waterpulse.Logic
{
    // Almacén clave-valor del récord. En el juego lo implementa PlayerPrefs (persiste en el navegador);
    // en los tests, uno en memoria
    public interface IRecordStore
    {
        bool TryGet(string key, out int value);
        void Set(string key, int value);
        void Delete(string key);
    }

    // Récord personal: los puntos totales del equipo del jugador en un partido
    public class RecordStorage
    {
        readonly IRecordStore store;
        readonly string key;

        public RecordStorage(IRecordStore store, string key)
        {
            this.store = store;
            this.key = key;
        }

        public bool HasRecord => store.TryGet(key, out _);
        public int Best => store.TryGet(key, out int value) ? value : 0;

        // Guarda la puntuación si supera el récord. Devuelve true si es un récord nuevo
        public bool Submit(int points)
        {
            if (HasRecord && points <= Best) return false;
            store.Set(key, points);
            return true;
        }

        // Borra el récord (opción del menú): el próximo partido vuelve a ser récord
        public void Clear() => store.Delete(key);
    }
}
