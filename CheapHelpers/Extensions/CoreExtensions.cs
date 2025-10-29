using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;

namespace CheapHelpers.Extensions
{
    public static class CoreExtensions
    {
        public static string ToJson<O>(this O obj) where O : class => JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,  // Critical - prevent type confusion attacks
            MaxDepth = 32  // Prevent deeply nested object attacks
        });
        public static T FromJson<T>(this string json) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    MaxDepth = 32,  // Prevent deeply nested object attacks (DoS risk)
                    TypeNameHandling = TypeNameHandling.None,  // Critical - prevent polymorphic deserialization attacks
                    ReferenceLoopHandling = ReferenceLoopHandling.Error,  // Fail on circular references (security best practice)
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
        /// Be careful! this serializes/deserializes an object to make a deep clone.
        /// This is cpu+ram intensive but easy to use vs manually deep copying properties
        /// </summary>
        /// <typeparam name="O">object source</typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepClone<O, T>(this O obj) where O : class => JsonConvert.DeserializeObject<T>(obj.ToJson(), new JsonSerializerSettings
        {
            MaxDepth = 32,  // Prevent deeply nested object attacks
            TypeNameHandling = TypeNameHandling.None,  // Critical - prevent type confusion attacks
            ReferenceLoopHandling = ReferenceLoopHandling.Error,  // Fail on circular references
            NullValueHandling = NullValueHandling.Ignore
        });

        /// <summary>
        /// Be careful! this serializes/deserializes an object to make a deep clone.
        /// This is cpu+ram intensive but easy to use vs manually deep copying properties
        /// </summary>
        /// <typeparam name="O"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static O DeepClone<O>(this O obj) where O : class => JsonConvert.DeserializeObject<O>(obj.ToJson(), new JsonSerializerSettings
        {
            MaxDepth = 32,  // Prevent deeply nested object attacks
            TypeNameHandling = TypeNameHandling.None,  // Critical - prevent type confusion attacks
            ReferenceLoopHandling = ReferenceLoopHandling.Error,  // Fail on circular references
            NullValueHandling = NullValueHandling.Ignore
        });

        public static bool HasProperty(dynamic source, string name)
        {
            if (source is ExpandoObject)
            {
                return ((IDictionary<string, object>)source).ContainsKey(name);
            }
            return source.GetType().GetProperty(name) != null;
        }

        public static UriBuilder AddQueryParm(this UriBuilder uri, string parmName, string parmValue)
        {
            var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
            q[parmName] = parmValue;
            uri.Query = q.ToString();
            return uri;
        }
    }
}