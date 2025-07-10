namespace CheapHelpers.Services.Export.Json
{
    public interface IJsonService
    {
        /// <summary>
        /// Use this to parse large files, this uses a stream to convert instead of a string
        /// </summary>
        /// <typeparam name="C"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public C ReadJson<C>(string path) where C : class;
        public Task<C> ReadJsonAsync<C>(string path) where C : class;
    }
}
