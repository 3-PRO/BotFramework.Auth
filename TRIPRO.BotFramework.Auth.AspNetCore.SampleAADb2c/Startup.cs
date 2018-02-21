using Autofac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;

namespace TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c
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
            MicrosoftAppCredentials credentials = new MicrosoftAppCredentials(
                Configuration.GetSection("ConnectionStrings").GetSection("BotFramework").GetSection("MicrosoftAppId")?.Value,
                Configuration.GetSection("ConnectionStrings").GetSection("BotFramework").GetSection("MicrosoftAppPassword")?.Value
            );

            services
              .AddSingleton(typeof(MicrosoftAppCredentials), credentials);

            Conversation.UpdateContainer(builder =>
            {
                builder
                   .Register(componentContext => credentials)
                   .SingleInstance();
                builder
                    .RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));
                builder
                    .Register(componentContext => new TableBotDataStore(Configuration.GetConnectionString("AzureTableStorage")))
                    .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                    .AsSelf()
                    .SingleInstance();
            });

            services
            .AddMvc(options => options.Filters.Add(typeof(TrustServiceUrlAttribute)))
            .AddJsonOptions(o => o.SerializerSettings.ContractResolver = new DefaultContractResolver());

            services.AddAuthentication(
                   // This can be removed after https://github.com/aspnet/IISIntegration/issues/371
                   options =>
                   {
                       options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                       options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                   }
               )
             .AddBotAuthentication(credentials.MicrosoftAppId, credentials.MicrosoftAppPassword);

            services
              .AddCors(corsOptions =>
              {
                  corsOptions.AddPolicy("AllowAllOrigins",
                      builder => builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
              });

            services.AddSingleton(Configuration.GetSection("ConnectionStrings").GetSection("B2C").Get<AuthenticationOptions>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowAllOrigins");

            app.UseAuthentication();

            app.UseExceptionHandler(appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(context.Features.Get<IExceptionHandlerFeature>().Error.Message));
                });
            });

            app.UseMvc();
        }
    }
}
