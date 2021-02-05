using Fhi.Lmr.Felles.TilgangKodeverk.Cache;
using Fhi.Lmr.Felles.TilgangKodeverk.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Lmr.Felles.TilgangKodeverk
{

    public static class ServiceCollectionAdd { 
        public static IServiceCollection AddTilgangKodeverk(this IServiceCollection services)
        {
            services.AddSingleton<KodeverkKodeMemoryCache>();
            services.AddScoped<IKlassifikasjonService, KlassifikasjonService>();
            services.AddScoped<IValidKodeverkKodeCheckService, ValidKodeverkKodeCheckService>();

            return services;
        }

    }
}
