using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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
    }
}
