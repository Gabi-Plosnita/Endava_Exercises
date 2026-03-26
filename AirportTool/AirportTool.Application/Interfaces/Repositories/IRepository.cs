namespace AirportTool.Application;

public interface IRepository<TDomain, TKey> where TDomain : class
{
    Task<TDomain?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TDomain>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(TDomain domainModel, CancellationToken cancellationToken);

    Task UpdateAsync(TDomain domainModel, CancellationToken cancellationToken);

    Task RemoveAsync(TDomain domainModel, CancellationToken cancellationToken);
}
