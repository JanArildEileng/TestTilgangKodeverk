using Fhi.Lmr.Felles.TilgangKodeverk.Cache;
using Fhi.Lmr.Felles.TilgangKodeverk.Service;
using Fhi.Lmr.Grunndata.Kodeverk.Apiklient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Fhi.Lmr.Felles.TilgangKodeverk
{

    public static class ServiceCollectionAdd { 
        public static IServiceCollection AddTilgangKodeverk(this IServiceCollection services, IConfiguration Configuration)
        {
            GrunndataKodeverkOption GrunndataKodeverkOption = Configuration.GetSection(GrunndataKodeverkOption.GrunndataKodeverk).Get<GrunndataKodeverkOption>();
            services.Configure<GrunndataKodeverkOption>(Configuration.GetSection(GrunndataKodeverkOption.GrunndataKodeverk));

            services.AddHttpClient(
            "GrunndataKodeverkHttpKlient", c => {
               // c.BaseAddress = new Uri(Configuration["GrunndataKodeverk:ApiUrl"]);
                c.BaseAddress = new Uri(GrunndataKodeverkOption.ApiUrl);
            }
          );


            services.AddSingleton<GrunndataKodeverkHttpKlient>();
            services.AddSingleton<KodeverkKodeMemoryCache>();
            services.AddScoped<IKlassifikasjonService, KlassifikasjonService>();
            services.AddScoped<IValidKodeverkKodeCheckService, ValidKodeverkKodeCheckService>();

            return services;
        }

    }
}
