using System.Net.WebSockets;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Voice;

/// <summary>
/// Text-to-speech using Microsoft Edge TTS (free, websocket-based, no key).
/// Connects to the Edge TTS service and streams audio back as MP3.
/// Failures are logged and return empty audio rather than throwing.
/// </summary>
public sealed class EdgeTextToSpeech(IOptions<EdgeTtsOptions> options, ILogger<EdgeTextToSpeech> logger, TimeProvider clock) : ITextToSpeech
{
    private const string EdgeTtsUrl = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1";
    private const string DefaultLanguage = "en-US";

    /// <summary>
    /// The long-standing client token reverse-engineered from the Edge browser.
    /// Microsoft also wires a <c>Sec-MS-GEC</c> header on top — see
    /// <see cref="EdgeTtsSecurity.ComputeSecMsGec"/>.
    /// </summary>
    internal const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

    /// <summary>
    /// User-Agent string Microsoft's gateway sniffs to confirm we're "Edge". The free
    /// TTS endpoint rejects anything that doesn't look like a current Edge build *and*
    /// rejects when the UA version disagrees with <see cref="EdgeTtsSecurity.SecMsGecVersion"/>.
    /// Tracks the upstream <c>rany2/edge-tts</c> Python package's pinned version —
    /// bump <see cref="EdgeTtsSecurity.ChromiumFullVersion"/> when they do.
    /// </summary>
    internal static readonly string EdgeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        $"(KHTML, like Gecko) Chrome/{EdgeTtsSecurity.ChromiumMajorVersion}.0.0.0 " +
        $"Safari/537.36 Edg/{EdgeTtsSecurity.ChromiumMajorVersion}.0.0.0";

    public async Task<ReadOnlyMemory<byte>> SynthesizeAsync(string text, TtsOptions? ttsOptions = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ReadOnlyMemory<byte>.Empty;

        var edgeOptions = options.Value;
        var voice = ttsOptions?.Voice ?? edgeOptions.Voice;
        var language = ttsOptions?.Language ?? DefaultLanguage;

        // URL + header set ported from rany2/edge-tts (Python upstream)
        // — see edge_tts/constants.py and edge_tts/communicate.py:
        //   wss://...edge/v1?TrustedClientToken=<t>&ConnectionId=<uuid>&Sec-MS-GEC=<hash>&Sec-MS-GEC-Version=<v>
        //
        // Parameter ORDER matters in practice: upstream sends them in that order and the
        // gateway has been observed to reject other orderings. Every value is URL-encoded
        // for safety (hex/dotted-version contain no reserved chars today, but if Microsoft
        // ever changes the token format we don't want silent URL corruption).
        var secMsGec = EdgeTtsSecurity.ComputeSecMsGec(clock.GetUtcNow(), TrustedClientToken);
        var connectionId = Guid.NewGuid().ToString("N");
        var wsUri = new Uri(
            $"{EdgeTtsUrl}" +
            $"?TrustedClientToken={Uri.EscapeDataString(TrustedClientToken)}" +
            $"&ConnectionId={connectionId}" +
            $"&Sec-MS-GEC={Uri.EscapeDataString(secMsGec)}" +
            $"&Sec-MS-GEC-Version={Uri.EscapeDataString(EdgeTtsSecurity.SecMsGecVersion)}");

        // Pulled out of the using/try so the catch can log the response status + headers
        // that ClientWebSocket captures after a failed handshake (with
        // CollectHttpResponseDetails). The finally below handles disposal in all paths.
        ClientWebSocket? webSocket = null;
        try
        {
            webSocket = new ClientWebSocket();
            // Negotiate permessage-deflate on the WebSocket handshake — upstream
            // rany2/edge-tts passes compress=15 to aiohttp which sends
            // "Sec-WebSocket-Extensions: permessage-deflate; client_max_window_bits=15".
            // Microsoft's gateway has been observed to reject the upgrade with HTTP 400
            // when the extension isn't offered (the request looks fine to a browser eye
            // but to their server it's an indicator of a non-Edge client). Defaults
            // here match aiohttp's compress=15 — 15-bit window, context-takeover enabled.
            webSocket.Options.DangerousDeflateOptions = new WebSocketDeflateOptions
            {
                ClientMaxWindowBits = 15,
                ClientContextTakeover = true,
                ServerMaxWindowBits = 15,
                ServerContextTakeover = true,
            };
            // Capture the response status + headers when the handshake fails so we can
            // log Microsoft's actual rejection reason instead of guessing. .NET wires
            // these into the ClientWebSocket properties after ConnectAsync (success or
            // failure) when this flag is on.
            webSocket.Options.CollectHttpResponseDetails = true;
            // Header set mirrors edge_tts/constants.py BASE_HEADERS + WSS_HEADERS +
            // headers_with_muid. Missing any one of these has been enough to earn a 403
            // — the gateway sniffs the full bundle, not just the User-Agent.
            // NOTE: do not SetRequestHeader("Sec-WebSocket-Version", ...) here —
            // ClientWebSocket emits that header automatically; setting it manually
            // results in a duplicate "Sec-WebSocket-Version: 13, 13" on the wire that
            // Microsoft now rejects with HTTP 400.
            webSocket.Options.SetRequestHeader("Pragma", "no-cache");
            webSocket.Options.SetRequestHeader("Cache-Control", "no-cache");
            webSocket.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
            webSocket.Options.SetRequestHeader("User-Agent", EdgeUserAgent);
            webSocket.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br, zstd");
            webSocket.Options.SetRequestHeader("Accept-Language", "en-US,en;q=0.9");
            // MUID is a random 32-hex-char cookie that real Edge browsers send on every
            // request. Upstream generates a fresh one per call via secrets.token_hex(16).upper().
            webSocket.Options.SetRequestHeader("Cookie", $"muid={GenerateMuid()};");

            // Log the prefix only — full hash is enough to cross-check against a
            // reference Python/Go run if Microsoft tightens things further, but no
            // need to dump the whole 64-char digest on every synth.
            logger.LogDebug(
                "EdgeTTS Sec-MS-GEC prefix={Prefix} version={Version}",
                secMsGec[..16], EdgeTtsSecurity.SecMsGecVersion);

            await webSocket.ConnectAsync(wsUri, cancellationToken);
            logger.LogDebug("EdgeTTS connected (voice {Voice})", voice);

            // Send configuration. Upstream rany2/edge-tts adds an X-Timestamp header to
            // both the speech.config and SSML messages; we mirror that for parity in
            // case Microsoft starts validating it. (The 2026-05-30 handshake-400 cliff
            // turned out to be a duplicate Sec-WebSocket-Version header, not anything
            // about X-Timestamp — but we keep this matched to upstream so a future
            // gateway tightening doesn't blindside us again.)
            var timestamp = BuildEdgeTimestamp(clock.GetUtcNow().UtcDateTime);
            var configJson = "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"},\"outputFormat\":\"audio-24khz-96kbitrate-mono-mp3\"}}}}";
            var configMessage = $"X-Timestamp:{timestamp}\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n{configJson}";

            await SendTextAsync(webSocket, configMessage, cancellationToken);

            // Send SSML synthesis request. Upstream order is: X-RequestId, Content-Type,
            // X-Timestamp, Path (and the timestamp gets a trailing 'Z' here — verified
            // against rany2/edge-tts communicate.py's ssml_headers_plus_data).
            var requestId = Guid.NewGuid().ToString("N");
            var ssmlMessage = $"X-RequestId:{requestId}\r\nContent-Type:application/ssml+xml\r\nX-Timestamp:{timestamp}Z\r\nPath:ssml\r\n\r\n" +
                $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{SecurityElement.Escape(language)}'>" +
                $"<voice name='{SecurityElement.Escape(voice)}'>" +
                $"<prosody pitch='{SecurityElement.Escape(edgeOptions.Pitch)}' rate='{SecurityElement.Escape(edgeOptions.Rate)}' volume='{SecurityElement.Escape(edgeOptions.Volume)}'>" +
                $"{SecurityElement.Escape(text)}</prosody></voice></speak>";

            await SendTextAsync(webSocket, ssmlMessage, cancellationToken);

            // Receive audio data. A message can span several receives, so frames are
            // reassembled until EndOfMessage before the header is parsed.
            using var audioStream = new MemoryStream();
            using var messageStream = new MemoryStream();
            var receiveBuffer = new byte[8192];

            while (true)
            {
                var wsResult = await webSocket.ReceiveAsync(receiveBuffer, cancellationToken);

                if (wsResult.MessageType == WebSocketMessageType.Close)
                    break;

                messageStream.Write(receiveBuffer, 0, wsResult.Count);
                if (!wsResult.EndOfMessage)
                    continue;

                var message = messageStream.GetBuffer().AsSpan(0, (int)messageStream.Length);

                if (wsResult.MessageType == WebSocketMessageType.Binary)
                {
                    // Binary messages contain audio data after a 2-byte header length prefix
                    if (message.Length > 2)
                    {
                        var headerLength = (message[0] << 8) | message[1];
                        if (headerLength + 2 < message.Length)
                            audioStream.Write(message[(headerLength + 2)..]);
                    }
                }
                else if (wsResult.MessageType == WebSocketMessageType.Text)
                {
                    if (Encoding.UTF8.GetString(message).Contains("Path:turn.end"))
                        break;
                }

                messageStream.SetLength(0);
            }

            // Politely close the socket so Dispose doesn't have to abort the connection —
            // aborts trigger a noisy first-chance WebSocketException cascade in the debugger
            // that drowns out useful log lines. We have all the audio bytes by this point;
            // if the close handshake fails, we don't care.
            //
            // CloseOutputAsync (send-only), NOT CloseAsync: CloseAsync also WAITS to receive
            // the server's close reply, and Edge routinely RSTs the connection instead of
            // replying — that receive is itself the cascade. Also handle CloseReceived (the
            // server closed first and the receive loop broke on its Close frame): without it
            // the socket disposes mid-handshake and Abort floods the debugger. From either
            // Open or CloseReceived, a send-only close reaches a terminal state cleanly.
            if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try
                {
                    using var closeTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    closeTimeoutCts.CancelAfter(TimeSpan.FromSeconds(1));
                    await webSocket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure,
                        string.Empty,
                        closeTimeoutCts.Token);
                }
                catch (WebSocketException) { /* expected if server is slow or already gone */ }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { /* close-handshake timeout */ }
                catch (ObjectDisposedException) { /* socket disposed while we were politely closing */ }
            }

            if (audioStream.Length == 0)
            {
                logger.LogWarning(
                    "EdgeTTS connected but returned zero audio bytes — Edge probably rejected the request. Voice='{Voice}' Rate={Rate}",
                    voice, edgeOptions.Rate);
            }
            else
            {
                logger.LogDebug("EdgeTTS synthesised {Bytes} bytes (voice {Voice})", audioStream.Length, voice);
            }
            return audioStream.ToArray();
        }
        // The caller's cancellation propagates; everything else returns empty audio.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Edge's websocket can fail for any reason (rate limited, throttled, blocked).
            // Returning empty audio keeps the pipeline alive; the consumer logs and moves on.
            // When the handshake itself failed (typical "HTTP 400/403 instead of 101"),
            // surface Microsoft's status code + headers so we can see the actual rejection
            // reason rather than guessing which header is missing.
            if (webSocket?.HttpStatusCode is { } status && status != 0)
            {
                var headers = webSocket.HttpResponseHeaders is { Count: > 0 } h
                    ? string.Join("; ", h.Select(kvp => $"{kvp.Key}={string.Join(",", kvp.Value)}"))
                    : "(none)";
                logger.LogError(
                    ex,
                    "EdgeTTS handshake failed with HTTP {Status}. Response headers: {Headers}",
                    (int)status, headers);
            }
            else
            {
                logger.LogError(ex, "EdgeTTS request failed");
            }
            return ReadOnlyMemory<byte>.Empty;
        }
        finally
        {
            webSocket?.Dispose();
        }
    }

    private static async Task SendTextAsync(ClientWebSocket webSocket, string message, CancellationToken cancellationToken)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(messageBytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    /// <summary>
    /// 32-character upper-case hex blob, matching <c>secrets.token_hex(16).upper()</c>
    /// in the upstream Python <c>DRM.generate_muid</c>. Used as the <c>muid</c> cookie
    /// the Edge browser sends on every request — Microsoft's gateway treats its absence
    /// as a fingerprint of non-Edge clients.
    /// </summary>
    private static string GenerateMuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes); // Convert.ToHexString is upper-case by default
    }

    /// <summary>
    /// JavaScript-style UTC timestamp Edge browsers send in the <c>X-Timestamp</c>
    /// header, matching <c>date_to_string()</c> in upstream <c>rany2/edge-tts</c>.
    /// Example: <c>Wed May 29 2026 22:00:00 GMT+0000 (Coordinated Universal Time)</c>.
    /// The literal "Coordinated Universal Time" suffix is what Microsoft validates
    /// against; <c>%Z</c> in strftime would produce an inconsistent name.
    /// </summary>
    public static string BuildEdgeTimestamp(DateTime utcNow) =>
        utcNow.ToString(
            "ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'",
            System.Globalization.CultureInfo.InvariantCulture);
}
