using Newtonsoft.Json;

namespace CheapHelpers.Services.DataExchange.Json;

internal class JsonService : IJsonService
{
    public C ReadJson<C>(string path) where C : class
    {
        var serializer = new JsonSerializer();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(reader);

        return serializer.Deserialize<C>(jsonReader);
    }

    public Task<C> ReadJsonAsync<C>(string path) where C : class
    {
        return Task.Run(() => ReadJson<C>(path));
    }
}
