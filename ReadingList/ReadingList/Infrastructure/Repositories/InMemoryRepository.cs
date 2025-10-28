using ReadingList.Domain;
using System.Collections.Concurrent;

namespace ReadingList.Infrastructure;

public class InMemoryRepository<T, TKey> : IRepository<T, TKey> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, T> _store = new();
    private readonly Func<T, TKey> _keySelector;

    public InMemoryRepository(Func<T, TKey> keySelector) => _keySelector = keySelector;

    public bool Add(T item) => _store.TryAdd(_keySelector(item), item);           

    public bool Update(T item)
    {
        var key = _keySelector(item);

        if (_store.TryGetValue(key, out var existingValue))
        {
            return _store.TryUpdate(key, item, existingValue);
        }

        return false;
    }

    public bool Contains(TKey key) => _store.ContainsKey(key);

    public bool TryGet(TKey key, out T? item) => _store.TryGetValue(key, out item);

    public IEnumerable<T> All() => _store.Values;
}
