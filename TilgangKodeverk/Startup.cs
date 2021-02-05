using Fhi.Lmr.Felles.TilgangKodeverk;
using Fhi.Lmr.Felles.TilgangKodeverk.Contracts;
using Fhi.Lmr.Grunndata.Kodeverk.Apiklient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using TilgangKodeverk.DataAksess;
using TilgangKodeverk.DataAksess.Repository;


namespace TilgangKodeverk
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TilgangKodeverk", Version = "v1" });
            });


            services.AddDbContext<TilgangKodeverkContext>(options =>
                  options.UseSqlServer(Configuration.GetConnectionString("TilgangKodeverkDB")));


            services.AddHttpClient(
              "GrunndataKodeverkHttpKlient", c => {
                  c.BaseAddress = new Uri(Configuration["GrunndataKodeverk:ApiUrl"]);
              }
            );

            services.AddSingleton<GrunndataKodeverkHttpKlient>();
        

            services.AddScoped<IKodeverkRepository, KodeverkRepository>();
            services.AddTilgangKodeverk();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, TilgangKodeverkContext tilgangKodeverkContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TilgangKodeverk v1"));

                tilgangKodeverkContext.Database.EnsureCreated();



            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
