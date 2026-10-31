using System.Net;

namespace CheapHelpers.Testing.Tests;

public class RecordingHttpHandlerTests
{
    private const string Url = "http://example.test/items";

    [Fact]
    public async Task Records_bodies_and_replies_in_order()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpHandler().Reply("first").Reply("second", HttpStatusCode.Accepted);
        using var client = handler.CreateClient();

        using var posted = await client.PostAsync(Url, new StringContent("hello"), cancellationToken);
        using var fetched = await client.GetAsync(Url, cancellationToken);

        Assert.Equal("first", await posted.Content.ReadAsStringAsync(cancellationToken));
        Assert.Equal(HttpStatusCode.Accepted, fetched.StatusCode);
        Assert.Equal("second", await fetched.Content.ReadAsStringAsync(cancellationToken));
        Assert.Equal(["hello", null], handler.RequestBodies);
        Assert.Equal([HttpMethod.Post, HttpMethod.Get], handler.Requests.Select(request => request.Method));
    }

    [Fact]
    public async Task Falls_back_to_the_responder_and_throws_with_method_and_url_when_nothing_is_left()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var withResponder = new RecordingHttpHandler().Respond(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        using var answered = withResponder.CreateClient();
        using var empty = new RecordingHttpHandler().CreateClient();

        using var response = await answered.DeleteAsync(Url, cancellationToken);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => empty.DeleteAsync(Url, cancellationToken));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains($"DELETE {Url}", error.Message);
    }
}
