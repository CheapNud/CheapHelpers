namespace CheapHelpers.Blazor.Helpers;

/// <summary>
/// What a <c>PasskeySubmit</c> button asks the browser for: a new passkey or a sign-in with an existing one.
/// </summary>
public enum PasskeyOperation
{
    Create = 0,
    Request = 1,
}
