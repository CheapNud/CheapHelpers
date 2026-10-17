namespace CheapHelpers.Services.Voice;

/// <summary>
/// A single voice entry from Microsoft's Edge TTS voices/list endpoint. Subset of the
/// upstream response — we keep just the fields a voice picker needs.
/// </summary>
/// <param name="ShortName">
/// The identifier passed as <see cref="EdgeTtsOptions.Voice"/> or <see cref="TtsOptions.Voice"/>. Example:
/// <c>en-US-JennyNeural</c>. This is the SSML <c>&lt;voice name="…"&gt;</c> argument.
/// </param>
/// <param name="Locale">BCP-47 locale tag (e.g. <c>en-US</c>) for region filtering.</param>
/// <param name="Gender">"Male" / "Female" as Microsoft reports it.</param>
/// <param name="FriendlyName">
/// Microsoft's display name (e.g. "Microsoft Jenny Online (Natural) - English (United States)").
/// Useful as a secondary substring-match target in an autocomplete so users can find
/// voices by display name, not just the short ID.
/// </param>
public sealed record EdgeTtsVoice(
    string ShortName,
    string Locale,
    string Gender,
    string FriendlyName);
