using CheapHelpers.EF.Extensions;
using CheapHelpers.EF.Infrastructure;
using CheapHelpers.Models.Contracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;

namespace CheapHelpers.EF.Repositories
{
    public class BaseRepo(IDbContextFactory<CheapContext> factory) : IDisposable
    {
        protected readonly IDbContextFactory<CheapContext> _factory = factory;

        public void Dispose()
        {
            //dispose of services here
            //_factory = null;
            //_mapper = null;
            GC.SuppressFinalize(this);
        }

        public async Task<List<T>> GetAll<T>() where T : class, IEntityId
        {
            using var context = _factory.CreateDbContext();
            return await context.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<PaginatedList<T>> GetAllPaginated<T>(int? pageIndex = null, int pageSize = 10, CancellationToken token = default) where T : class, IEntityId
        {
            try
            {
                using var context = _factory.CreateDbContext();
                var query = context.Set<T>().AsNoTracking();
                return await query.ToPaginatedListAsync(pageIndex, pageSize, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

        }

        /// <summary>
        ///Checks the first value, if null, adds the second and returns this, otherwise return first value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
#nullable enable
        public async Task<T> GenericAdd<T>(T? entity, T value) where T : class, IEntityId
        {
            ArgumentNullException.ThrowIfNull(value);

            if (entity == null)
            {
                using var context = _factory.CreateDbContext();
                context.Set<T>().Add(value);
                await context.SaveChangesAsync();
                return value;
            }

            return entity;
        }
#nullable disable

        /// <summary>
        /// if nothing with this codes exists in database, add it and return it, otherwise returns entity with id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="newvalue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<T> Add<T>(T value) where T : class, IEntityCode
        {
            ArgumentNullException.ThrowIfNull(value);

            using var context = _factory.CreateDbContext();
            var dbset = context.Set<T>();

            var result = await dbset.FirstOrDefaultAsync(x => x.Code == value.Code);

            if (result == null)
            {
                dbset.Add(value);
                await context.SaveChangesAsync();
                return value;
            }

            return result;
        }

        /// <summary>
        /// if nothing with this codes exists in database, add it and return it, otherwise returns entity with id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="newvalue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<T> Add<T>(DbContext context, T value) where T : class, IEntityCode
        {
            ArgumentNullException.ThrowIfNull(value);

            var dbset = context.Set<T>();

            var result = await dbset.FirstOrDefaultAsync(x => x.Code == value.Code);
            if (result == null)
            {
                if (value.Code == null)
                {
                    Debug.WriteLine(JsonConvert.SerializeObject(value));
                }

                dbset.Add(value);
                await context.SaveChangesAsync();
                return value;
            }

            return result;
        }
    }
}
