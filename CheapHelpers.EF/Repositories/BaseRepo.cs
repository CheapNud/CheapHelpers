using CheapHelpers.EF.Extensions;
using CheapHelpers.EF.Infrastructure;
using CheapHelpers.Models.Contracts;
using CheapHelpers.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Linq.Expressions;

namespace CheapHelpers.EF.Repositories
{
    public class BaseRepo(IDbContextFactory<CheapContext<CheapUser>> factory) : IDisposable
    {
        private const int DEFAULT_PAGE_SIZE = 10;
        private const int DEFAULT_PAGE_INDEX = 1;

        //TODO: this field should not be public but its used so i left it for now
        public readonly IDbContextFactory<CheapContext<CheapUser>> _factory = factory;

        public void Dispose()
        {
            //dispose of services here
            //_factory = null;
            //_mapper = null;
            GC.SuppressFinalize(this);
        }

        #region Read Operations

        /// <summary>
        /// Gets all entities of type T without tracking
        /// </summary>
        public async Task<List<T>> GetAllAsync<T>(CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Set<T>().AsNoTracking().ToListAsync(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets entities with pagination support
        /// </summary>
        public async Task<PaginatedList<T>> GetAllPaginatedAsync<T>(int? pageIndex = null, int pageSize = DEFAULT_PAGE_SIZE, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                var query = context.Set<T>().AsNoTracking();
                return await query.ToPaginatedListAsync(pageIndex, pageSize, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllPaginatedAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a single entity by its ID
        /// </summary>
        public async Task<T?> GetByIdAsync<T>(int id, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetByIdAsync<{typeof(T).Name}> with ID {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets entities based on a predicate condition
        /// </summary>
        public async Task<List<T>> GetWhereAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Set<T>().AsNoTracking().Where(predicate).ToListAsync(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetWhereAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets paginated entities based on a predicate condition
        /// </summary>
        public async Task<PaginatedList<T>> GetWherePaginatedAsync<T>(Expression<Func<T, bool>> predicate, int? pageIndex = null, int pageSize = DEFAULT_PAGE_SIZE, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                var query = context.Set<T>().AsNoTracking().Where(predicate);
                return await query.ToPaginatedListAsync(pageIndex, pageSize, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetWherePaginatedAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a single entity by its code
        /// </summary>
        public async Task<T?> GetByCodeAsync<T>(string code, CancellationToken token = default) where T : class, IEntityCode
        {
            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetByCodeAsync<{typeof(T).Name}> with code {code}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if an entity exists by ID
        /// </summary>
        public async Task<bool> ExistsAsync<T>(int id, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Set<T>().AnyAsync(x => x.Id == id, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExistsAsync<{typeof(T).Name}> with ID {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if an entity exists by code
        /// </summary>
        public async Task<bool> ExistsByCodeAsync<T>(string code, CancellationToken token = default) where T : class, IEntityCode
        {
            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Set<T>().AnyAsync(x => x.Code == code, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExistsByCodeAsync<{typeof(T).Name}> with code {code}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Create Operations

        /// <summary>
        /// Adds a new entity if the existing entity is null, otherwise returns the existing entity
        /// </summary>
        public async Task<T> AddIfNullAsync<T>(T? existingEntity, T newEntity, CancellationToken token = default) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(newEntity);

            if (existingEntity == null)
            {
                try
                {
                    using var context = _factory.CreateDbContext();
                    context.Set<T>().Add(newEntity);
                    await context.SaveChangesAsync(token);
                    return newEntity;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in AddIfNullAsync<{typeof(T).Name}>: {ex.Message}");
                    throw;
                }
            }

            return existingEntity;
        }

        /// <summary>
        /// Adds a new entity if no entity with the same code exists, otherwise returns the existing entity
        /// </summary>
        public async Task<T> AddIfNotExistsAsync<T>(T entity, CancellationToken token = default) where T : class, IEntityCode
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                using var context = _factory.CreateDbContext();
                var dbSet = context.Set<T>();

                var existingEntity = await dbSet.FirstOrDefaultAsync(x => x.Code == entity.Code, token);

                if (existingEntity == null)
                {
                    if (string.IsNullOrEmpty(entity.Code))
                    {
                        Debug.WriteLine($"Warning: Adding entity with null/empty code: {JsonConvert.SerializeObject(entity)}");
                    }

                    dbSet.Add(entity);
                    await context.SaveChangesAsync(token);
                    return entity;
                }

                return existingEntity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddIfNotExistsAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds a new entity unconditionally
        /// </summary>
        public async Task<T> AddAsync<T>(T entity, CancellationToken token = default) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                using var context = _factory.CreateDbContext();
                context.Set<T>().Add(entity);
                await context.SaveChangesAsync(token);
                return entity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds multiple entities at once
        /// </summary>
        public async Task<List<T>> AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken token = default) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(entities);

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return [];

            try
            {
                using var context = _factory.CreateDbContext();
                context.Set<T>().AddRange(entityList);
                await context.SaveChangesAsync(token);
                return entityList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddRangeAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Update Operations

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        public async Task<T> UpdateAsync<T>(T entity, CancellationToken token = default) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                using var context = _factory.CreateDbContext();
                context.Set<T>().Update(entity);
                await context.SaveChangesAsync(token);
                return entity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates multiple entities at once
        /// </summary>
        public async Task<List<T>> UpdateRangeAsync<T>(IEnumerable<T> entities, CancellationToken token = default) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(entities);

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return [];

            try
            {
                using var context = _factory.CreateDbContext();
                context.Set<T>().UpdateRange(entityList);
                await context.SaveChangesAsync(token);
                return entityList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateRangeAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Delete Operations

        /// <summary>
        /// Deletes an entity by its ID
        /// </summary>
        public async Task<bool> DeleteAsync<T>(int id, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                var entity = await context.Set<T>().FindAsync([id], token);

                if (entity == null)
                    return false;

                context.Set<T>().Remove(entity);
                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteAsync<{typeof(T).Name}> with ID {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes an entity directly
        /// </summary>
        public async Task<bool> DeleteAsync<T>(T entity, CancellationToken token = default) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                using var context = _factory.CreateDbContext();
                context.Set<T>().Remove(entity);
                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes multiple entities at once
        /// </summary>
        public async Task<int> DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken token = default) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(entities);

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return 0;

            try
            {
                using var context = _factory.CreateDbContext();
                context.Set<T>().RemoveRange(entityList);
                await context.SaveChangesAsync(token);
                return entityList.Count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteRangeAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes entities based on a predicate condition
        /// </summary>
        public async Task<int> DeleteWhereAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                var entities = await context.Set<T>().Where(predicate).ToListAsync(token);

                if (entities.Count == 0)
                    return 0;

                context.Set<T>().RemoveRange(entities);
                await context.SaveChangesAsync(token);
                return entities.Count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteWhereAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Static helper method for adding entities within an existing context transaction
        /// Use this when you need to add entities as part of a larger transaction
        /// </summary>
        public static async Task<T> AddIfNotExistsAsync<T>(DbContext context, T entity, CancellationToken token = default) where T : class, IEntityCode
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                var dbSet = context.Set<T>();
                var existingEntity = await dbSet.FirstOrDefaultAsync(x => x.Code == entity.Code, token);

                if (existingEntity == null)
                {
                    if (string.IsNullOrEmpty(entity.Code))
                    {
                        Debug.WriteLine($"Warning: Adding entity with null/empty code: {JsonConvert.SerializeObject(entity)}");
                    }

                    dbSet.Add(entity);
                    await context.SaveChangesAsync(token);
                    return entity;
                }

                return existingEntity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in static AddIfNotExistsAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the total count of entities of type T
        /// </summary>
        public async Task<int> CountAsync<T>(CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Set<T>().CountAsync(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CountAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the count of entities that match a predicate
        /// </summary>
        public async Task<int> CountWhereAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                return await context.Set<T>().CountAsync(predicate, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CountWhereAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}