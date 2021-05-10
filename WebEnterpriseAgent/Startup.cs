using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hyperledger.Aries.Storage;
using FluentValidation.AspNetCore;
using System;
using System.IO;
using Jdenticon.AspNetCore;
using WebAgent.Utils;
using WebAgent.Protocols.BasicMessage;
using WebAgent;
using WebAgent.Messages;
using WebAgent.ExtendedServices;
using Hyperledger.Indy.PoolApi;

namespace WebEnterpriseAgent
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
            //services.AddControllersWithViews();

            // Aries Open Api Requires Fluent Validation
            services
                .AddMvc()
                .AddFluentValidation();
                //.AddAriesOpenApi(a => a.UseSwaggerUi = true);

            services.AddLogging();
            services.AddRazorPages();

#if DEBUG
            services.AddAriesFramework(builder =>
            {
                builder.RegisterAgent<SimpleWebAgent>(c =>
                {
                    //c.AgentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? NameGenerator.GetRandomName();                    
                    c.AgentName = "Enterprise Agent";
                    c.EndpointUri = Environment.GetEnvironmentVariable("ENDPOINT_HOST") ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");                    
                    c.WalletConfiguration = new WalletConfiguration { Id = "WebAgentWallet03" };
                    c.WalletCredentials = new WalletCredentials { Key = "MyWalletKey123" };
                    c.GenesisFilename = Path.GetFullPath("baksak_pool_transactions_genesis.txn");
                    c.PoolName = "baksak-pool";
                    c.AutoRespondCredentialRequest = true;                    
                    c.IssuerKeySeed = "111222333444555666BAKSAKSteward1";
                    c.ProtocolVersion = 2;
                    Pool.SetProtocolVersionAsync(2);
                });     
                
            });
#endif
#if RELEASE
            services.AddAriesFramework(builder =>
            {
                builder.RegisterAgent<SimpleWebAgent>(c =>
                {
                    c.AgentName = "Enterprise Web Agent";                    
                    c.EndpointUri = "https://enterprise-webagent-win.azurewebsites.net/";
                    c.WalletConfiguration = new WalletConfiguration { Id = "WebAgentWallet03" };
                    c.WalletCredentials = new WalletCredentials { Key = "MyWalletKey123" };
                    c.GenesisFilename = Path.GetFullPath("baksak_pool_transactions_genesis.txn");
                    c.PoolName = "baksak-pool";                   
                    c.AutoRespondCredentialRequest = true;
                    c.IssuerKeySeed = "111222333444555666BAKSAKSteward1";
                    c.ProtocolVersion = 2;
                    Pool.SetProtocolVersionAsync(2);
                });                
            });
#endif

            // Register custom handlers with DI pipeline
            services.AddSingleton<BasicMessageHandler>();
            services.AddSingleton<TrustPingMessageHandler>();
            //services.AddExtendedLedgerService<ExtendedLedgerService>();
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                //app.UseParcelDevMiddleware(new ParcelBundlerOptions { EntryPoint="~/js/dcurrency.js" });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Register agent middleware
            app.UseAriesFramework();

            // Configure OpenApi
            //app.UseAriesOpenApi();

            // fun identicons
            app.UseJdenticon();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
