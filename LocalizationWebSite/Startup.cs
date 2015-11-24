// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNet.Localization;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace LocalizationWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddViewLocalization(options => options.ResourcesPath = "Resources");
        }

        public void Configure(IApplicationBuilder app)
        {
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
