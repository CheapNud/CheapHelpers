using MoreLinq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace CheapHelpers.Extensions
{
    public static class Extensions
    {
        public static string ToJson<O>(this O obj) where O : class => JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        public static T FromJson<T>(this string json) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JSON deserialization error: {ex.Message}");
                Debug.WriteLine($"JSON content: {json}");
                throw;
            }
        }

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

        /// <summary>
        /// Be careful! this serializes/deserializes an object to make a deep clone.
        /// This is cpu+ram intensive but easy to use vs manually deep copying properties
        /// </summary>
        /// <typeparam name="O">object source</typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepClone<O, T>(this O obj) where O : class => JsonConvert.DeserializeObject<T>(obj.ToJson());

        /// <summary>
        /// Be careful! this serializes/deserializes an object to make a deep clone.
        /// This is cpu+ram intensive but easy to use vs manually deep copying properties
        /// </summary>
        /// <typeparam name="O"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static O DeepClone<O>(this O obj) where O : class => JsonConvert.DeserializeObject<O>(obj.ToJson());

        public static BindingList<T> ToBindingList<T>(this IList<T> source)
        {
            return new BindingList<T>(source);
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            return new ObservableCollection<T>(source);
        }

        public static bool HasProperty(dynamic source, string name)
        {
            if (source is ExpandoObject)
            {
                return ((IDictionary<string, object>)source).ContainsKey(name);
            }
            return source.GetType().GetProperty(name) != null;
        }

        public static string Capitalize(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                ArgumentException.ThrowIfNullOrWhiteSpace($"'{nameof(str)}' cannot be null or whitespace.", nameof(str));
            }

            str = str.ToLower();
            return str.First().ToString().ToUpper() + string.Join("", str.Skip(1));
        }

        public static bool IsDigitsOnly(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                ArgumentException.ThrowIfNullOrWhiteSpace($"'{nameof(str)}' cannot be null or whitespace.", nameof(str));
            }

            return !str.Any(x => x < '0' || x > '9');
        }

        /// <summary>
        /// checks for j/n and converts to true false
        /// </summary>
        /// <param name="str"></param>
        /// <returns>true, false</returns>
        public static bool CheckBool(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            return str.Equals("j", StringComparison.CurrentCultureIgnoreCase);
        }

        public static string ToInternationalPhoneNumber(this string phonenumber)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(phonenumber);

            //regex?
            string output = phonenumber.Replace(@" ", string.Empty).Replace(@"\", string.Empty).Replace(@"/", string.Empty).Replace(@"-", string.Empty).Replace(@".", string.Empty);

            if (output.StartsWith('+'))
            {
                return output;
            }
            else if (output.StartsWith("00"))
            {
                return $@"+{output.Remove(0, 2)}";
            }
            else if (output.StartsWith("06"))
            {
                return $@"+31{output.Remove(0, 1)}";
            }
            else if (output.StartsWith('0'))
            {
                return $@"+32{output.Remove(0, 1)}";
            }

            Debug.WriteLine($@"cannot convert -- {output} -- to international phone number");
            return null;
        }

        public static string ToAuthorizationCredentials(string usr, string pwd)
        {
            return $@"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($@"{usr}:{pwd}"))}";
        }

        public static (string username, string password) GetAuthorizationCredentials(this string encodedstring)
        {
            var token = encodedstring.Substring("Basic ".Length).Trim();
            var credentialstring = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var credentials = credentialstring.Split(':');
            return (credentials[0], credentials[1]);
        }

        public static SecureString ToSecureString(this string input)
        {
            SecureString output = new SecureString();
            int l = input.Length;
            char[] s = input.ToCharArray(0, l);
            for (int i = 0; i < s.Length; i++)
            {
                output.AppendChar(s[i]);
            }
            return output;
        }

        public static string CharArrayToString(this IEnumerable<char> input)
        {
            return string.Concat(input);
        }

        public static string StringArrayToString(this IEnumerable<string> input)
        {
            return string.Concat(input);
        }

        public static string ToShortString(this string input, int chars = 20)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            if (input.Length <= chars)
            {
                return input;
            }

            return @$"{input[..chars]}...";
        }

        public static string ToString(this SecureString ss)
        {
            nint unmanagedString = nint.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(ss);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        public static UriBuilder AddQueryParm(this UriBuilder uri, string parmName, string parmValue)
        {
            var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
            q[parmName] = parmValue;
            uri.Query = q.ToString();
            return uri;
        }

        public static DateTime GetDateTime(this TimeZoneInfo ti, DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTime(dateTime, ti);
        }

        public static DateTime GetDateTime(this DateTime dateTime, TimeZoneInfo ti)
        {
            return TimeZoneInfo.ConvertTime(dateTime, ti);
        }

        public static int GetWorkingDays(this DateTime current, DateTime finishDateExclusive, List<DateTime> excludedDates = null)
        {
            Func<int, bool> isWorkingDay = days =>
            {
                var currentDate = current.AddDays(days);
                var isNonWorkingDay =
                    currentDate.DayOfWeek == DayOfWeek.Saturday ||
                    currentDate.DayOfWeek == DayOfWeek.Sunday ||
                    excludedDates != null && excludedDates.Exists(excludedDate => excludedDate.Date.Equals(currentDate.Date));
                return !isNonWorkingDay;
            };

            return Enumerable.Range(0, (finishDateExclusive - current).Days).Count(isWorkingDay);
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) => enumerable == null || !enumerable.Any();
    }
}