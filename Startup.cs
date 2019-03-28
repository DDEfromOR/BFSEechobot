extern alias BBC;
extern alias SE;
extern alias BB;

using BB::Microsoft.Bot.Builder;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BBC::Microsoft.Bot.Connector.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SE::Microsoft.Bot.Builder.BotFramework;
using SE::Microsoft.Bot.Builder.Integration.AspNet.Core;
using SE::Microsoft.Bot.Builder.Integration.AspNet.Core.StreamingExtensions;

namespace Microsoft.EchoBot
{
    public class Startup
    {
        private const string BotOpenIdMetadataKey = "BotOpenIdMetadata";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            if (!string.IsNullOrEmpty(Configuration[BotOpenIdMetadataKey]))
                ChannelValidation.OpenIdMetadataUrl = Configuration[BotOpenIdMetadataKey];

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            services.AddSingleton<IChannelProvider, ConfigurationChannelProvider>();

            // Create the Bot Framework Adapter.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the Bot Framework Streaming Extension Adapter.
            services.AddSingleton<BotFrameworkWebSocketAdapter>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, EchoBot>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();
            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
