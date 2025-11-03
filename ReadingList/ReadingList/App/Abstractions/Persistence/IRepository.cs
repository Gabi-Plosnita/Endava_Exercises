namespace ReadingList.App;

public interface IRepository<T, TKey> where TKey : notnull
{
    bool Add(T item);              
    bool Update(T item);              
    bool Contains(TKey key);
    T? GetById(TKey key);
    IEnumerable<T> GetAll();
}

