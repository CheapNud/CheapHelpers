using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CheapHelpers.EF.Infrastructure
{
    public class PaginatedList<T> : List<T>
    {
        public PaginatedList()
        {
            PageIndex = 0;
            ResultCount = 0;
            TotalPages = 1;
        }

        /// <summary>
        /// Creates the paginated list, Use CreateAsync to effectively create it, not the constructor (Constructors cannot be async), PaginatedList is used for heavy results, heavy results should always use a task.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="useSkip"></param>
        public PaginatedList(List<T> source, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            ResultCount = count;
            TotalPages = (int)Math.Ceiling(ResultCount / (double)pageSize);
            AddRange(source);
        }

        /// <summary>
        /// Index where the page is on
        /// </summary>
        public int PageIndex { get; private set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// Total number of items in the query
        /// </summary>
        public int ResultCount { get; private set; }

        public bool HasPreviousPage
        {
            get
            {
                return PageIndex > 1;
            }
        }

        public bool HasNextPage
        {
            get
            {
                return PageIndex < TotalPages;
            }
        }

        /// <summary>
        /// Uses Ef lazy loading to create the list.
        /// The higher the pageSize, the longer the wait
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize, bool count, CancellationToken token = default)
        {
            try
            {
                List<T> items;
                int counter = 0;

                if (count)
                {
                    items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(token);
                    counter = await source.CountAsync(token);
                }
                else
                {
                    items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(token);
                }

                return new PaginatedList<T>(items, counter, pageIndex, pageSize);
            }
            catch (TaskCanceledException tc)
            {
                return null;
            }
            catch (SqlException sex)
            {
                Debug.WriteLine(token.IsCancellationRequested);
                Debug.WriteLine(sex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(token.IsCancellationRequested);
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize, CancellationToken token = default)
        {
            return await CreateAsync(source, pageIndex, pageSize, true, token);
        }
    }
}