using Bunit;
using CheapHelpers.Blazor.Configuration;
using CheapHelpers.Blazor.Pages.Account;
using CheapHelpers.Services.Email;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;

namespace CheapHelpers.Blazor.Tests;

public class RegisterPageTests : BunitContext
{
    private const string CreateLabel = "Create";

    private readonly CheapHelpersBlazorOptions _options = new() { CreateUser = () => new TestUser() };

    public RegisterPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLocalization();
        Services.AddScoped<IStringLocalizer, StringLocalizer<Register>>();
        Services.AddSingleton<IEmailService, NullEmailService>();
        Services.AddSingleton(_options);
        Services.AddTestIdentity();
    }

    [Fact]
    public void Answers_not_found_without_a_form_when_registration_is_off()
    {
        _options.RegistrationEnabled = false;
        var notFoundRaised = false;
        Services.GetRequiredService<NavigationManager>().OnNotFound += (_, _) => notFoundRaised = true;

        var cut = Render<Register>();

        Assert.True(notFoundRaised);
        Assert.Empty(cut.FindComponents<MudForm>());
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public void Shows_the_create_button_when_registration_is_on()
    {
        var cut = Render<Register>();

        Assert.Contains(cut.FindAll("button"), button => button.TextContent.Trim() == CreateLabel);
    }
}
