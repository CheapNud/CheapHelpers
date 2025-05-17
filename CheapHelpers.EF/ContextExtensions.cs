using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheapHelpers;

namespace CheapHelpers.EF
{
    public static class ContextExtensions
    {
        public static IEnumerable<T> DistinctByMyself<T>(this IQueryable<T> context, Func<T, string> a)
        {
            return context.GroupBy(a).Select(g => g.First());
        }


        /// <summary>
        /// Do not use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Task EnableIdentityInsert<T>(this DbContext context) => SetIdentityInsert<T>(context, enable: true);
        /// <summary>
        /// Do not use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Task DisableIdentityInsert<T>(this DbContext context) => SetIdentityInsert<T>(context, enable: false);

        /// <summary>
        /// Do not use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        private static Task<int> SetIdentityInsert<T>(DbContext context, bool enable)
        {
            var entityType = context.Model.FindEntityType(typeof(T));
            var value = enable ? "ON" : "OFF";
            return context.Database.ExecuteSqlAsync($"SET IDENTITY_INSERT {entityType.GetSchema()}.{entityType.GetTableName()} {value}");
        }

        /// <summary>
        /// Do not use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        public static async Task SaveChangesWithIdentityInsert<T>(this DbContext context)
        {
            using var transaction = context.Database.BeginTransaction();
            await context.EnableIdentityInsert<T>();
            await context.SaveChangesAsync();
            await context.DisableIdentityInsert<T>();
            await transaction.CommitAsync();
        }

        public async static Task<PaginatedList<T>> ToPaginatedListAsync<T>(this IQueryable<T> query, int? pageIndex = null, int pageSize = 10, CancellationToken token = default) where T : class, IEntityId
        {
            return await PaginatedList<T>.CreateAsync(query, pageIndex ?? 1, pageSize, token);
        }

        public static void Clear<T>(this DbSet<T> dbset) where T : class
        {
            dbset.RemoveRange(dbset);
        }

        /// <summary>
        /// don't use this for smaller tables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="f"></param>
        /// <param name="batchsize"></param>
        /// <param name="concurrentcontextcalls"></param>
        /// <returns></returns>
        public async static Task BatchDelete<T>(this IDbContextFactory<DbContext> f, int batchsize = 1000, int concurrentcontextcalls = 1) where T : class
        {
            try
            {
                using var c1 = f.CreateDbContext();
                var dbset = c1.Set<T>();
                Debug.WriteLine(@$"Deleting from DbSet<{typeof(T).Name}>");
                int counter = 0;
                while (await dbset.AnyAsync())
                {
                    List<Task> tasks = [];
                    for (int i = 1; i <= concurrentcontextcalls; i++)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            using var c = f.CreateDbContext();
                            var q = c.Set<T>().AsQueryable();
                            if (i != 0)
                            {
                                q = q.Skip(batchsize * i);
                            }
                            counter += await q.Take(batchsize).ExecuteDeleteAsync();
                        }));
                    }
                    await Task.WhenAll(tasks);
                    Debug.WriteLine(@$"{counter} records deleted from DbSet<{typeof(T).Name}>");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// static extension of the base repo function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="newvalue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<T> Add<T>(this DbContext context, T value) where T : class, IEntityCode
        {
            ArgumentNullException.ThrowIfNull(value);

            return await BaseRepo.Add(context, value);
        }
    }
}