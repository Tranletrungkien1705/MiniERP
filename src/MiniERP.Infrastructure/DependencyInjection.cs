using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniERP.Application.Abstractions;
using MiniERP.Infrastructure.Identity;
using MiniERP.Infrastructure.Jobs;
using MiniERP.Infrastructure.Persistence;

namespace MiniERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=minierp.db";
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<IdentityProviderOptions>(configuration.GetSection("IdentityProvider"));
        services.AddHttpClient<IIdentityProviderClient, InosIdentityProviderClient>((sp, http) =>
        {
            var opts = configuration.GetSection("IdentityProvider").Get<IdentityProviderOptions>();
            http.BaseAddress = new Uri(opts?.AuthorityUrl ?? "https://localhost:44389");
            http.Timeout = TimeSpan.FromSeconds(15);
        }).AddStandardResilienceHandler();

        services.AddHostedService<InventorySyncEtlJob>();

        return services;
    }
}
