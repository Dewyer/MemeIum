using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.Jobs;
using MemeIumServices.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemeIumServices
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
            services.AddMvc();
            services.AddSingleton<INodeComService, NodeComService>();
            services.AddSingleton<ITransactionUtil, TransactionUtil>();
            services.AddSingleton<IWalletUtil, WalletUtil>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<IServerWalletService, ServerWalletService>();
            services.AddScoped<ICompetitionService, CompetitionService>();
            services.AddDbContext<UASContext>(options =>
                options.UseSqlServer(Configuration["Connection"]));
            services.AddDbContext<MemeOffContext>(options =>
                options.UseSqlServer(Configuration["Connection2"]));


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            JobScheduler.Start();
        }
    }
}
