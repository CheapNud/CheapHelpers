using CheapHelpers.Blazor.Services;
using CheapHelpers.Extensions;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeMapping;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;

namespace CheapHelpers.Blazor.Pages.Account
{
    [Route("[controller]/[action]")]
    [Authorize]
    public class AccountController(
        SignInManager<CheapUser> signInManager,
        IEmailService mailer,
        UserManager<CheapUser> userManager,
        UserService userService,
        UrlEncoder urlEncoder
        ) : Controller
    {
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private const string AppName = "CheapHelpers.Blazor";
        private const int RecoveryCodesCount = 10;
        private const int AuthenticatorKeyGroupSize = 4;
        private const string HomeRoute = "/";
        private const string LoginRoute = "/Account/Login/";
        private const string LockoutRoute = "/Account/Lockout/";
        private const string EnableAuthenticatorRoute = "Account/EnableAuthenticator";

        // Error messages
        private const string InvalidTokenMessage = "invalid token";
        private const string AuthenticatorNotEnabledMessage = "The authenticator was not enabled.";
        private const string ExternalLoginNotRemovedMessage = "The external login was not removed.";
        private const string UnableToLoadTwoFactorUserMessage = "Unable to load two-factor authentication user.";

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SignInWithRecoveryCode(IFormCollection fc)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException(UnableToLoadTwoFactorUserMessage);
            }

            var recoveryCode = ((string)fc["RecoveryCode"]).Replace(" ", string.Empty);
            var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            LogSignInAttempt(user.UserName, result.Succeeded, GetSignInDescription(result));

            return result.Succeeded switch
            {
                true => Redirect(HomeRoute),
                false when result.IsLockedOut => Redirect(LockoutRoute),
                _ => Redirect(LoginRoute)
            };
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SignIn(IFormCollection fc)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            string username = fc["UserName"];
            var signInResult = await signInManager.PasswordSignInAsync(
                username,
                fc["Password"],
                isPersistent: true,
                lockoutOnFailure: false
            );

            LogSignInAttempt(username, signInResult.Succeeded, GetSignInDescription(signInResult));

            return signInResult.Succeeded ? Redirect(HomeRoute) : Redirect(LoginRoute);
        }

        public new async Task<IActionResult> SignOut()
        {
            if (signInManager.IsSignedIn(User))
            {
                await signInManager.SignOutAsync();
                LogSignOut();
            }

            return Redirect(HomeRoute);
        }

        public async Task<IActionResult> Refresh()
        {
            if (signInManager.IsSignedIn(User))
            {
                var user = await userService.GetUserAsync(User);
                await signInManager.RefreshSignInAsync(user);
            }

            return Redirect(HomeRoute);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest(ExternalLoginNotRemovedMessage);
            }

            var result = await userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                return BadRequest(ExternalLoginNotRemovedMessage);
            }

            await signInManager.RefreshSignInAsync(user);
            return Ok();
        }

        /// <summary>
        /// Gets the shared key and QR code URI for authenticator registration
        /// </summary>
        /// <returns>JSON containing shared key and authenticator URI</returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GetSharedKey()
        {
            var user = await userManager.GetUserAsync(User);
            var result = await LoadSharedKeyAndQrCodeUriAsync(user);

            return result.HasValue ? Json(result.Value) : BadRequest("Unable to generate shared key");
        }

        [HttpPost("EnableAuthenticator")]
        public async Task<IActionResult> EnableAuthenticator(IFormCollection fc)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest(AuthenticatorNotEnabledMessage);
            }

            var verificationCode = CleanVerificationCode((string)fc["VerificationCode"]);
            var isTokenValid = await userManager.VerifyTwoFactorTokenAsync(
                user,
                userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode
            );

            if (!isTokenValid)
            {
                return BadRequest(InvalidTokenMessage);
            }

            await userManager.SetTwoFactorEnabledAsync(user, true);

            // Generate recovery codes if none exist
            if (await userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodesCount);
                return Ok(recoveryCodes.ToArray());
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetExternalLogins()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID {userManager.GetUserId(User)}.");
            }

            var currentLogins = await userManager.GetLoginsAsync(user);
            var availableSchemes = await signInManager.GetExternalAuthenticationSchemesAsync();
            var otherLogins = availableSchemes
                .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();

            return Ok(new { CurrentLogins = currentLogins, AvailableLogins = otherLogins });
        }

        [HttpPost]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await userManager.SetTwoFactorEnabledAsync(user, false);
            await userManager.ResetAuthenticatorKeyAsync(user);
            await signInManager.RefreshSignInAsync(user);

            return Redirect(EnableAuthenticatorRoute);
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonalData()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            var personalData = await BuildPersonalDataDictionary(user);
            var json = personalData.ToJson();
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            Response.Headers.Append("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(jsonBytes, KnownMimeTypes.Json);
        }

        #region Private Helper Methods

        private static string CleanVerificationCode(string code) =>
            code.Replace(" ", string.Empty).Replace("-", string.Empty);

        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;

            while (currentPosition + AuthenticatorKeyGroupSize < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, AuthenticatorKeyGroupSize)).Append(" ");
                currentPosition += AuthenticatorKeyGroupSize;
            }

            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey) =>
            string.Format(
                AuthenticatorUriFormat,
                urlEncoder.Encode(AppName),
                urlEncoder.Encode(email),
                unformattedKey
            );

        /// <summary>
        /// Loads the shared key and QR code URI for authenticator registration
        /// </summary>
        /// <param name="user">The user to generate the key for</param>
        /// <returns>Tuple containing (shared key, authenticator URI) or null if failed</returns>
        private async Task<(string SharedKey, string AuthenticatorUri)?> LoadSharedKeyAndQrCodeUriAsync(CheapUser user)
        {
            try
            {
                var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
                if (string.IsNullOrEmpty(unformattedKey))
                {
                    await userManager.ResetAuthenticatorKeyAsync(user);
                    unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
                }

                var sharedKey = FormatKey(unformattedKey);
                var email = await userManager.GetEmailAsync(user);
                var authenticatorUri = GenerateQrCodeUri(email, unformattedKey);

                return (sharedKey, authenticatorUri);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load shared key and QR code URI: {ex.Message}");
                return null;
            }
        }

        private async Task<Dictionary<string, string>> BuildPersonalDataDictionary(CheapUser user)
        {
            var personalData = new Dictionary<string, string>();

            // Add properties marked with PersonalDataAttribute
            var personalDataProps = typeof(CheapUser)
                .GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));

            foreach (var property in personalDataProps)
            {
                personalData.Add(property.Name, property.GetValue(user)?.ToString() ?? "null");
            }

            // Add external login information
            var logins = await userManager.GetLoginsAsync(user);
            foreach (var login in logins)
            {
                personalData.Add($"{login.LoginProvider} external login provider key", login.ProviderKey);
            }

            return personalData;
        }

        private void LogSignInAttempt(string username, bool success, string description)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            Debug.WriteLine($"Sign-in attempt - User: {username}, Success: {success}, IP: {ipAddress}, Details: {description}");
        }

        private void LogSignOut()
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            Debug.WriteLine($"Sign-out - User: {User.Identity?.Name ?? "Unknown"}, IP: {ipAddress}");
        }

        private static string GetSignInDescription(Microsoft.AspNetCore.Identity.SignInResult result) =>
            result switch
            {
                { Succeeded: true } => "Signed in successfully",
                { IsLockedOut: true } => "Account locked out",
                { RequiresTwoFactor: true } => "Requires two-factor authentication",
                { IsNotAllowed: true } => "Sign-in not allowed",
                _ => "Failed sign-in"
            };

        #endregion
    }
}