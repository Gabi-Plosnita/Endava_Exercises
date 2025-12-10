namespace AirportTool.Application;

public interface IRepository<TDomain, TKey> where TDomain : class
{
    Task<TDomain?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TDomain>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TDomain domainModel, CancellationToken cancellationToken = default);

    Task UpdateAsync(TDomain domainModel, CancellationToken cancellationToken = default);

    Task RemoveAsync(TDomain domainModel, CancellationToken cancellationToken = default);
}
