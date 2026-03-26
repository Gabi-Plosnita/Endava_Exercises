using AirportTool.Application;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public abstract class EfRepositoryBase<TDomain, TEntity, TKey>
        : IRepository<TDomain, TKey>
        where TDomain : class
        where TEntity : class
{
    protected readonly AirportDbContext _context;
    protected readonly IMapper _mapper;
    protected readonly DbSet<TEntity> _dbSet;

    protected EfRepositoryBase(AirportDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
        _dbSet = _context.Set<TEntity>();
    }

    public virtual async Task<TDomain?> GetByIdAsync(TKey id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken);
        if (entity == null)
        {
            return null;
        }

        _context.Entry(entity).State = EntityState.Detached;
        return _mapper.Map<TDomain>(entity);
    }

    public virtual async Task<IReadOnlyList<TDomain>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await _dbSet.AsNoTracking()
                                   .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<TDomain>>(entities);
    }

    public virtual async Task AddAsync(TDomain domainModel, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<TEntity>(domainModel);
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual Task UpdateAsync(TDomain domainModel, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<TEntity>(domainModel);
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task RemoveAsync(TDomain domainModel, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<TEntity>(domainModel);
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
