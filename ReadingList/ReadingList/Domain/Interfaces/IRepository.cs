namespace ReadingList.Domain;

public interface IRepository<T, TKey> where TKey : notnull
{
    bool Add(T item);              
    bool Upsert(T item);              
    bool Contains(TKey key);
    bool TryGet(TKey key, out T? item);
    IEnumerable<T> All();
}

