// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNet.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using SomeWebLib;

namespace LocalizationWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // this custom StringLocalizer is slightly modifed from
            // https://github.com/aspnet/Localization/blob/dev/src/Microsoft.Extensions.Localization/ResourceManagerStringLocalizerFactory.cs
            // the problem seems to be at line 66 where assembly of the type is used to create the resourcemanager
            // which means it will not look for the resx file int he webapp /Resources folder
            // var assembly = typeInfo.Assembly;
            // I fixed it by using the assembly of the web app in all cases
            //  if (!(assembly.FullName.StartsWith(_applicationEnvironment.ApplicationName)))
            //  {
            //      assembly = Assembly.Load(_applicationEnvironment.ApplicationName);
            //  }

            // the problem is solved by uncommenting this, I'm leaving it commented to show the problem
            //services.TryAdd(new ServiceDescriptor(
            //    typeof(IStringLocalizerFactory),
            //    typeof(CustomResourceManagerStringLocalizerFactory),
            //    ServiceLifetime.Singleton));
            
            services.AddMvc().AddViewLocalization(options => options.ResourcesPath = "Resources");
        }

        public void Configure(
            IApplicationBuilder app,
            ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();
            // this could not be resolved in beta8
            //app.UseCultureReplacer();

            app.UseDeveloperExceptionPage();

            app.UseIISPlatformHandler();

            var options = new RequestLocalizationOptions
            {
                SupportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("fr"),
                    new CultureInfo("en-GB")
                },
                SupportedUICultures = new List<CultureInfo>
                {
                    new CultureInfo("fr"),
                    new CultureInfo("en-GB")
                }
            };
            app.UseRequestLocalization(options, new RequestCulture("en-US"));

            //app.UseMvcWithDefaultRoute();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.Run(context =>
            {
                context.Response.StatusCode = 404;
                
                return Task.FromResult(0);
            });
        }
    }
}
