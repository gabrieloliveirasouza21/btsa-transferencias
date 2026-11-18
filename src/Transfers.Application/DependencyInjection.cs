using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Application.Transfers;

namespace Transfers.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TransferRulesOptions>(configuration.GetSection(TransferRulesOptions.SectionName));
            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<TransferService>();
            services.AddScoped<AccountService>();

            return services;
        }
    }
}
