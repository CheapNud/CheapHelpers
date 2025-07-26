using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CheapHelpers.EF.Infrastructure
{
    public class PaginatedList<T>(List<T> source, int count, int pageIndex, int pageSize) : List<T>(source)
    {
        private const int DEFAULT_PAGE_INDEX = 0;
        private const int DEFAULT_RESULT_COUNT = 0;
        private const int DEFAULT_TOTAL_PAGES = 1;
        private const int FIRST_PAGE = 1;

        /// <summary>
        /// Default constructor for empty paginated list
        /// </summary>
        public PaginatedList() : this([], DEFAULT_RESULT_COUNT, DEFAULT_PAGE_INDEX, DEFAULT_TOTAL_PAGES)
        {
        }

        /// <summary>
        /// Index where the page is on
        /// </summary>
        public int PageIndex { get; private init; } = pageIndex;

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; private init; } = (int)Math.Ceiling(count / (double)pageSize);

        /// <summary>
        /// Total number of items in the query
        /// </summary>
        public int ResultCount { get; private init; } = count;

        public bool HasPreviousPage => PageIndex > FIRST_PAGE;

        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// Uses EF lazy loading to create the list.
        /// The higher the pageSize, the longer the wait
        /// </summary>
        /// <param name="source">The queryable source</param>
        /// <param name="pageIndex">Current page index (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="count">Whether to perform count operation</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Paginated list of items</returns>
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize, bool count, CancellationToken token = default)
        {
            try
            {
                var skipCount = (pageIndex - FIRST_PAGE) * pageSize;
                var pagedQuery = source.Skip(skipCount).Take(pageSize);

                var (items, totalCount) = count
                    ? (await pagedQuery.ToListAsync(token), await source.CountAsync(token))
                    : (await pagedQuery.ToListAsync(token), DEFAULT_RESULT_COUNT);

                return new PaginatedList<T>(items, totalCount, pageIndex, pageSize);
            }
            catch (TaskCanceledException)
            {
                // Preserve cancellation semantics instead of returning null
                throw;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine($"SQL Exception - Cancellation Requested: {token.IsCancellationRequested}");
                Debug.WriteLine($"SQL Exception Message: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General Exception - Cancellation Requested: {token.IsCancellationRequested}");
                Debug.WriteLine($"Exception Message: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates paginated list with count enabled by default
        /// </summary>
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize, CancellationToken token = default)
        {
            return await CreateAsync(source, pageIndex, pageSize, true, token);
        }
    }
}