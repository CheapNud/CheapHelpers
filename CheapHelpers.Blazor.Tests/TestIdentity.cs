using CheapHelpers.Blazor.Services;
using CheapHelpers.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Blazor.Tests;

/// <summary>
/// Identity on an in-memory database, plus the IUserService the account endpoints and pages inject.
/// </summary>
internal static class TestIdentity
{
    public static IdentityBuilder AddTestIdentity(this IServiceCollection services)
    {
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContextFactory<CheapContext<TestUser>>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IUserService, UserService<TestUser, CheapContext<TestUser>>>();
        return services.AddIdentity<TestUser, IdentityRole>().AddEntityFrameworkStores<CheapContext<TestUser>>();
    }
}
