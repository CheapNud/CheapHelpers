using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace CheapHelpers
{
	public static class DynamicHelper
	{
		public static bool HasProperty(dynamic obj, string name)
		{
			Type objType = obj.GetType();
			if (objType == typeof(ExpandoObject))
			{
				return ((IDictionary<string, object>)obj).ContainsKey(name);
			}
			if (objType == typeof(JObject))
			{
				var jobj = (JObject)obj;
                return jobj.ContainsKey(name);
            }
			return objType.GetProperty(name) != null;
		}
	}
}
