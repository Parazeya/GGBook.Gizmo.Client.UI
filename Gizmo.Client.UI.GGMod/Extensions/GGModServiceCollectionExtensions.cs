using Microsoft.Extensions.DependencyInjection;

namespace Gizmo.Client.UI.GGMod.Extensions
{
    public static class GGModServiceCollectionExtensions
    {
        public static IServiceCollection AddGGModServices(this IServiceCollection services)
        {
            services.AddHttpClient("GGMod.Api");
            return services;
        }
    }
}
