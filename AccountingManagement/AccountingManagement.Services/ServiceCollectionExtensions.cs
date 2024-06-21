using Microsoft.Extensions.DependencyInjection;
using AccountingManagement.Services.ErrorHandling;

namespace AccountingManagement.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAccountingManagementServices(this IServiceCollection serviceCollection)
        {
            var serviceProvider = serviceCollection
                .AddSingleton<IErrorReportingService, ErrorReportingService>();

            return serviceProvider;
        }
    }
}
