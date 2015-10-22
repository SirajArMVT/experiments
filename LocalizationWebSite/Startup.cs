// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
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

            app.UseRequestLocalization();
            
            app.UseMvcWithDefaultRoute();

            app.Run(context =>
            {
                context.Response.StatusCode = 404;
                
                return Task.FromResult(0);
            });
        }
    }
}
