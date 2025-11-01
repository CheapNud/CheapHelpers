using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace CheapHelpers.Extensions
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Seraches for the index of the old item and replaces it with the new item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="oldItem"></param>
        /// <param name="newItem"></param>
        /// <returns>the index replaced</returns>
        public static T Replace<T>(this IList<T> list, T oldItem, T newItem)
        {
            //new Thread(new ThreadStart(sss));
            var oldItemIndex = list.IndexOf(oldItem);
            return list[oldItemIndex] = newItem;
        }

        /// <summary>
        /// Replace with predicate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="oldItemSelector"></param>
        /// <param name="newItem"></param>
        public static void Replace<T>(this List<T> list, Predicate<T> oldItemSelector, T newItem)
        {
            //check for different situations here and throw exception
            //if list contains multiple items that match the predicate
            //or check for nullability of list and etc ...
            var oldItemIndex = list.FindIndex(oldItemSelector);
            list[oldItemIndex] = newItem;
        }

        public static BindingList<T> ToBindingList<T>(this IList<T> source)
        {
            return new BindingList<T>(source);
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            return new ObservableCollection<T>(source);
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) => enumerable == null || !enumerable.Any();

        /// <summary>
        /// Dynamically orders a queryable collection by a property name in ascending order using reflection.
        /// Useful for sorting based on runtime-determined property names.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query</typeparam>
        /// <param name="query">The queryable collection to order</param>
        /// <param name="orderByMember">The property name to order by</param>
        /// <returns>An ordered queryable collection in ascending order</returns>
        /// <example>
        /// var users = dbContext.Users.AsQueryable();
        /// var sortedUsers = users.OrderByDynamic("LastName");
        /// </example>
        public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> query, string orderByMember)
            => OrderByDynamicInternal(query, orderByMember, "OrderBy");

        /// <summary>
        /// Dynamically orders a queryable collection by a property name in descending order using reflection.
        /// Useful for sorting based on runtime-determined property names.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query</typeparam>
        /// <param name="query">The queryable collection to order</param>
        /// <param name="orderByMember">The property name to order by</param>
        /// <returns>An ordered queryable collection in descending order</returns>
        /// <example>
        /// var users = dbContext.Users.AsQueryable();
        /// var sortedUsers = users.OrderByDescendingDynamic("LastName");
        /// </example>
        public static IQueryable<T> OrderByDescendingDynamic<T>(this IQueryable<T> query, string orderByMember)
            => OrderByDynamicInternal(query, orderByMember, "OrderByDescending");

        private static IQueryable<T> OrderByDynamicInternal<T>(
            IQueryable<T> query,
            string orderByMember,
            string methodName)
        {
            var queryElementTypeParam = Expression.Parameter(typeof(T));
            var memberAccess = Expression.PropertyOrField(queryElementTypeParam, orderByMember);
            var keySelector = Expression.Lambda(memberAccess, queryElementTypeParam);

            var orderBy = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(T), memberAccess.Type],
                query.Expression,
                Expression.Quote(keySelector));

            return query.Provider.CreateQuery<T>(orderBy);
        }
    }
}
