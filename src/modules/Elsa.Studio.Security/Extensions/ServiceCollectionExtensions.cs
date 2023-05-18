using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Security.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityModule(this IServiceCollection services)
    {
        return services
            .AddSingleton<IModule, Module>()
            .AddSingleton<IMenuProvider, SecurityMenu>();
    }
}