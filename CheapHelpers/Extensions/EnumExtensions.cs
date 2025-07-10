using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CheapHelpers
{
    public static class EnumExtensions
    {
        public static string StringValue<TEnum>(this TEnum value) where TEnum : Enum
        {
            string output = null;
            Type type = value.GetType();
            FieldInfo fi = type.GetField(value.ToString());
            StringValue[] attrs = fi.GetCustomAttributes(typeof(StringValue), false) as StringValue[];
            if (attrs.Length > 0)
            {
                output = attrs[0].Value;
            }
            return output;
        }

        public static Dictionary<TEnum, string> ToDictionary<TEnum>(this Type t) where TEnum : Enum
        {
            if (t == null) throw new NullReferenceException();
            if (!t.IsEnum) throw new InvalidCastException("object is not an Enumeration");

            var dic = new Dictionary<TEnum, string>();
            foreach (TEnum item in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
            {
                dic.Add(item, item.StringValue());
            }
            return dic;
        }

        public static List<(TEnum, string)> ToListValues<TEnum>(this Type t) where TEnum : Enum
        {
            if (t == null) throw new NullReferenceException();
            if (!t.IsEnum) throw new InvalidCastException("object is not an Enumeration");

            return Enum.GetValues(t).Cast<TEnum>().Select(item => (item, item.StringValue())).ToList();
        }

        public static List<TEnum> ToList<TEnum>(this Type t) where TEnum : Enum
        {
            if (t == null) throw new NullReferenceException();
            if (!t.IsEnum) throw new InvalidCastException("object is not an Enumeration");

            return Enum.GetValues(t).Cast<TEnum>().ToList();
        }

        public static TEnum ToEnum<TEnum>(this string value, TEnum defaultValue)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return (TEnum)Enum.Parse(typeof(TEnum), value, true);
        }

        ///// <summary>
        ///// EXAMPLE DO NOT USE
        ///// </summary>
        //private static readonly Dictionary<BlobContainers, string> _configLookup = new Dictionary<BlobContainers, string>{
        //	{ BlobContainers.AnomalyContainer, "anomaly" },
        //};

        ///// <summary>
        ///// EXAMPLE DO NOT USE
        ///// </summary>
        ///// <param name="configOption"></param>
        ///// <returns></returns>
        //private static string GetContainer(BlobContainers configOption)
        //{
        //	if (!_configLookup.TryGetValue(configOption, out string value))
        //	{
        //		Debug.WriteLine("value not present config dictionary");
        //		return null;
        //		// Handle error
        //	}

        //	return value;
        //	// Use value retrieved from dictionary.
        //}
    }
}
