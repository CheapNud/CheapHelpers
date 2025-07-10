using Newtonsoft.Json;

//TODO: fix and cleanup
namespace CheapHelpers.Services.Export.Json
{
    internal class JsonService : IJsonService
    {
        //private dynamic ReadJson(string path)
        //{
        //    var serializer = new JsonSerializer();
        //    using (Stream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        //    {
        //        using (var sr = new StreamReader(s))
        //        using (var jsonTextReader = new JsonTextReader(sr))
        //        {
        //            dynamic? jsObj = serializer.Deserialize<ExpandoObject>(jsonTextReader);
        //            if (jsObj == null)
        //            {
        //                throw new Exception("jsobj was null");
        //            }
        //            return jsObj;
        //        }
        //    }
        //}

        public C ReadJson<C>(string path) where C : class
        {
            var serializer = new JsonSerializer();
            using (Stream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(s))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    return serializer.Deserialize<C>(jsonTextReader);
                }
            }
        }

        public Task<C> ReadJsonAsync<C>(string path) where C : class
        {
            return Task.Run(() =>
            {
                return ReadJson<C>(path);
            });
        }
    }
}
