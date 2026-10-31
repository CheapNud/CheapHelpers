using System.Net;
using System.Text;

namespace CheapHelpers.Testing;

/// <summary>
/// An HTTP handler that records every request and answers with the queued replies in order, then with the responder.
/// Never touches the network.
/// </summary>
public sealed class RecordingHttpHandler : HttpMessageHandler
{
    private const string JsonContentType = "application/json";

    private readonly Lock _lock = new();
    private readonly Queue<(HttpStatusCode Status, string Body, string ContentType)> _replies = new();
    private Func<HttpRequestMessage, HttpResponseMessage>? _responder;

    /// <summary>Every request sent, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>The body of every request, in order; null when the request had no content.</summary>
    public List<string?> RequestBodies { get; } = [];

    /// <summary>Queues a reply. Replies go out in the order they were queued.</summary>
    public RecordingHttpHandler Reply(string body, HttpStatusCode status = HttpStatusCode.OK, string contentType = JsonContentType)
    {
        lock (_lock)
        {
            _replies.Enqueue((status, body, contentType));
        }

        return this;
    }

    /// <summary>Sets the responder that answers every request once the reply queue is empty.</summary>
    public RecordingHttpHandler Respond(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
        return this;
    }

    /// <summary>Creates a client that sends through this handler.</summary>
    public HttpClient CreateClient() => new(this);

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        (HttpStatusCode Status, string Body, string ContentType) reply;
        lock (_lock)
        {
            Requests.Add(request);
            RequestBodies.Add(body);
            if (!_replies.TryDequeue(out reply))
            {
                return _responder?.Invoke(request)
                    ?? throw new InvalidOperationException($"No reply queued for {request.Method} {request.RequestUri}.");
            }
        }

        return new HttpResponseMessage(reply.Status)
        {
            Content = new StringContent(reply.Body, Encoding.UTF8, reply.ContentType),
        };
    }
}
