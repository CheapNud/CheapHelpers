using CheapHelpers.Blazor.Services;
using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CheapHelpers.Blazor.Pages.Account
{
    [Route("[controller]/[action]")]
    [Authorize]
    public class AccountController(
        SignInManager<CheapUser> signInManager,
        IDbContextFactory<CheapContext<CheapUser>> factory,
        IEmailService mailer,
        UserManager<CheapUser> userManager,
        UserService userService,
        UrlEncoder urlEncoder
        ) : Controller
    {
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return Ok();
        }

        //[AllowAnonymous]
        //public async Task ResetPassword(IFormCollection fc)
        //{
        //    var userid = fc["UserId"];
        //    var newpassword = fc["NewPassword"];
        //    var code = fc["Code"];


        //    var user = await _userService.GetUserAsync(userid);

        //    if (user == null)
        //    {
        //        return;
        //    }

        //    var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        //    var result = await userManager.ResetPasswordAsync(user, code, newpassword);
        //}

        //[AllowAnonymous]
        //public async Task ForgotPassword(IFormCollection fc)
        //{
        //    try
        //    {
        //        var email = fc["UserName"];
        //        var user = await userManager.FindByEmailAsync(email);
        //        if (user == null || !(await userManager.IsEmailConfirmedAsync(user)))
        //        {
        //            // Don't reveal that the user does not exist or is not confirmed
        //            //return RedirectToPage("./ForgotPasswordConfirmation");
        //            return;
        //        }

        //        // For more information on how to enable account confirmation and password reset please
        //        // visit https://go.microsoft.com/fwlink/?LinkID=532713
        //        //var callbackUrl = Url.Page("/Account/ResetPassword", pageHandler: null, values: new { area = "Identity", code }, protocol: Request.Scheme);
        //        //await Mailer.SendEmailAsync(Email, "Reset Password", $"Please reset your password by <a href='{HtmlEncoder.Alternative.Encode(callbackUrl)}'>clicking here</a>.");
        //        //return RedirectToPage("./ForgotPasswordConfirmation");

        //        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        //        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        //        var link = $@"{Nav.BaseUri}Account/ResetPassword?userid={user.Id}&code={code}";
        //        await _mailer.SendPasswordTokenAsync(email, link);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //    }
        //}

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SignInWithRecoveryCode(IFormCollection fc)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            using var context = factory.CreateDbContext();
            SignInLog log =
           new()
           {
               LogTime = DateTime.Now,
               IpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString()
           };

            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            string username = user.UserName;

            if (user == null)
            {
                throw new InvalidOperationException(
                    $"Unable to load two-factor authentication user."
                );
            }

            var recoveryCode = ((string)fc["RecoveryCode"]).Replace(" ", string.Empty);

            var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                //log.IdentityUser = await context.Users.FirstAsync(x => x.UserName == username);
                //log.Success = true;
                //log.LogDescription = $@"Signed in";
                //context.SignInLogs.Add(log);
                //await context.SaveChangesAsync();
                return Redirect("/");
            }
            if (result.IsLockedOut)
            {
                //log.IdentityUser = await context.Users.FirstOrDefaultAsync(x => x.UserName == username) ?? await context.Users.FirstAsync(x => x.UserName == Program.DefaultAccount);
                //log.Success = false;
                //log.LogDescription = $@"{username} tried to login but was locked out.";
                //context.SignInLogs.Add(log);
                //await context.SaveChangesAsync();
                return Redirect("/Account/Lockout/");
            }
            else
            {
                //log.IdentityUser = await context.Users.FirstOrDefaultAsync(x => x.UserName == username) ?? await context.Users.FirstAsync(x => x.UserName == Program.DefaultAccount);
                //log.Success = false;
                //log.LogDescription = $@"{username} Invalid recovery code entered";
                //context.SignInLogs.Add(log);
                //await context.SaveChangesAsync();
                return Redirect("/Account/Login/");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SignIn(IFormCollection fc)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            string username = fc["UserName"];
            var signinresult = await signInManager.PasswordSignInAsync(
                username,
                fc["Password"],
                true,
                false
            );

            using var context = factory.CreateDbContext();
            SignInLog log =
                new()
                {
                    LogTime = DateTime.Now,
                    IpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString()
                };

            if (signinresult.Succeeded)
            {
                //log.IdentityUser = await context.Users.FirstAsync(x => x.UserName == username);
                //log.Success = true;
                //log.LogDescription = $@"Signed in";
                //context.SignInLogs.Add(log);
                //await context.SaveChangesAsync();
                return Redirect("/");
            }
            else
            {
                //log.IdentityUser =
                //    await context.Users.FirstOrDefaultAsync(x => x.UserName == username)
                //    ?? await context.Users.FirstAsync(x => x.UserName == Program.DefaultAccount);
                //log.Success = false;
                //log.LogDescription = $@"{username} failed sign in";
                //context.SignInLogs.Add(log);
                //await context.SaveChangesAsync();
                return Redirect("/Account/Login/");
            }
        }

        public new async Task<IActionResult> SignOut()
        {
            if (signInManager.IsSignedIn(User))
            {
                await signInManager.SignOutAsync();
            }

            //using var context = factory.CreateDbContext();
            //SignInLog log =
            //    new()
            //    {
            //        LogTime = DateTime.Now,
            //        IpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
            //        IdentityUser = await context.Users.FirstAsync(
            //            x => x.UserName == User.Identity.Name
            //        ),
            //        Success = true,
            //        LogDescription = $@"Signed out"
            //    };

            //context.SignInLogs.Add(log);
            //await context.SaveChangesAsync();
            return Redirect("/");
        }

        public async Task<IActionResult> Refresh()
        {
            if (signInManager.IsSignedIn(User))
            {
                await signInManager.RefreshSignInAsync(await userService.GetUserAsync(User));
            }

            return Redirect("/");
        }

        public async Task<IActionResult> OnPostRemoveLoginAsync(
    string loginProvider,
    string providerKey
)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("The external login was not removed.");
            }

            var result = await userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                return BadRequest("The external login was not removed.");
            }

            await signInManager.RefreshSignInAsync(user);
            return Ok();
        }


        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;

            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
            AuthenticatorUriFormat,
                urlEncoder.Encode("CheapHelpers.Blazor"),
                urlEncoder.Encode(email),
                unformattedKey
            );
        }

        /// <summary>
        /// gets the shared key adn qr code uri for registration
        /// </summary>
        /// <param name="user"></param>
        /// <returns>(sharedkey, authenticatoruri)</returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GetSharedKey(IFormCollection? fc = null)
        {
            var user = await userManager.GetUserAsync(User);
            return Json(await LoadSharedKeyAndQrCodeUriAsync(user));
        }

        /// <summary>
        /// gets the shared key adn qr code uri for registration
        /// </summary>
        /// <param name="user"></param>
        /// <returns>(sharedkey, authenticatoruri)</returns>
        private async Task<ValueTuple<string, string>?> LoadSharedKeyAndQrCodeUriAsync(CheapUser user)
        {
            try
            {
                // Load the authenticator key & QR code URI to display on the form
                var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
                if (string.IsNullOrEmpty(unformattedKey))
                {
                    await userManager.ResetAuthenticatorKeyAsync(user);
                    unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
                }

                var sharedkey = FormatKey(unformattedKey);
                var email = await userManager.GetEmailAsync(user);
                var authenticatoruri = GenerateQrCodeUri(email, unformattedKey);

                return (sharedkey, authenticatoruri);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        [HttpPost("EnableAuthenticator")]
        public async Task<IActionResult> EnableAuthenticator(IFormCollection fc)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("The authenticator was not enabled.");
            }

            // Strip spaces and hypens
            var verificationCode = ((string)fc["VerificationCode"]).Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await userManager.VerifyTwoFactorTokenAsync(
                user,
                userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode
            );

            if (!is2faTokenValid)
            {
                return BadRequest("invalid token");
            }

            await userManager.SetTwoFactorEnabledAsync(user, true);

            if (await userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                    user,
                    10
                );
                var recoverycodes = recoveryCodes.ToArray();
                return Ok(recoverycodes);
            }
            else
            {
                return Ok();
            }
        }

        public async Task<IActionResult> GetExternalLogins()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID {userManager.GetUserId(User)}.");
            }

            var currentLogins = await userManager.GetLoginsAsync(user);
            var otherLogins = (await signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            return Ok();
        }

        public async Task<IActionResult> ResetAuthenticatorAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await userManager.SetTwoFactorEnabledAsync(user, false);
            await userManager.ResetAuthenticatorKeyAsync(user);

            await signInManager.RefreshSignInAsync(user);
            return Redirect("Account/EnableAuthenticator");
        }

        public async Task<IActionResult> GetPersonalData()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(CheapUser)
                .GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await userManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            Response.Headers.Append("Content-Disposition", "attachment; filename=PersonalData.json");

            return new FileContentResult(
                JsonSerializer.SerializeToUtf8Bytes(personalData),
                "application/json"
            );
        }
    }
}
