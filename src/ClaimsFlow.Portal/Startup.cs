using ClaimsFlow.Portal.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClaimsFlow.Portal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews()
                .AddNewtonsoftJson()
                .AddRazorRuntimeCompilation();

            services.AddApplicationInsightsTelemetry();

            // Deprecated Key Vault SDK plus AppAuthentication. The modern pairing
            // is Azure.Security.KeyVault.Secrets with Azure.Identity.
            services.AddSingleton<IKeyVaultClient>(_ =>
            {
                var provider = new AzureServiceTokenProvider();
                return new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(provider.KeyVaultTokenCallback));
            });

            services.AddSingleton<ISecretsProvider, SecretsProvider>();
            services.AddSingleton<IClaimsTableGateway, ClaimsTableGateway>();
            services.AddSingleton<IDocumentStore, DocumentStore>();
            services.AddSingleton<IFraudDecisionQueue, FraudDecisionQueue>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Claims/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Claims}/{action=Index}/{id?}");
            });
        }
    }
}
