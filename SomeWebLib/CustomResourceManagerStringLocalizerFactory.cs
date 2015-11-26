
//https://github.com/aspnet/Localization/blob/dev/src/Microsoft.Extensions.Localization/ResourceManagerStringLocalizerFactory.cs

//https://github.com/aspnet/Localization/blob/dev/src/Microsoft.Extensions.Localization/LocalizationServiceCollectionExtensions.cs

//https://github.com/dotnet/corefx/blob/master/src/System.Resources.ResourceManager/ref/System.Resources.ResourceManager.cs

using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.Logging;


namespace SomeWebLib
{
    public class CustomResourceManagerStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IResourceNamesCache _resourceNamesCache = new ResourceNamesCache();

        private readonly IApplicationEnvironment _applicationEnvironment;

        private readonly string _resourcesRelativePath;

        private ILoggerFactory loggerFactory;
        private ILogger log;

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="applicationEnvironment">The <see cref="IApplicationEnvironment"/>.</param>
        /// <param name="localizationOptions">The <see cref="IOptions{LocalizationOptions}"/>.</param>
        public CustomResourceManagerStringLocalizerFactory(
            IApplicationEnvironment applicationEnvironment,
            IOptions<LocalizationOptions> localizationOptions,
            ILoggerFactory loggerFactory
            )
        {
            if (applicationEnvironment == null)
            {
                throw new ArgumentNullException(nameof(applicationEnvironment));
            }

            if (localizationOptions == null)
            {
                throw new ArgumentNullException(nameof(localizationOptions));
            }

            _applicationEnvironment = applicationEnvironment;
            _resourcesRelativePath = localizationOptions.Value.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(_resourcesRelativePath))
            {
                _resourcesRelativePath = _resourcesRelativePath.Replace(Path.AltDirectorySeparatorChar, '.')
                    .Replace(Path.DirectorySeparatorChar, '.') + ".";
            }

            this.loggerFactory = loggerFactory;
            log = loggerFactory.CreateLogger<CustomResourceManagerStringLocalizerFactory>();

            log.LogInformation("cunstructor completed");
            log.LogInformation("_resourcesRelativePath was " + _resourcesRelativePath);

            log.LogInformation("_applicationEnvironment.ApplicationName was " + _applicationEnvironment.ApplicationName);

            
           
        }

        /// <summary>
        /// Creates a <see cref="CustomResourceManagerStringLocalizer"/> using the <see cref="Assembly"/> and
        /// <see cref="Type.FullName"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="resourceSource">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer Create(Type resourceSource)
        {
            log.LogInformation("Create(Type resourceSource)");

            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            var typeInfo = resourceSource.GetTypeInfo();
            var assembly = typeInfo.Assembly;

            if(!(assembly.FullName.StartsWith(_applicationEnvironment.ApplicationName)))
            {
                // resource for a classlibrary class
                // lets try switchback to web app assembly since that is where we want to look for resx files
                // this does work!
                assembly = Assembly.Load(_applicationEnvironment.ApplicationName);
            }
            

            log.LogInformation("assembly was " + assembly.FullName);

            log.LogInformation("_resourcesRelativePath was " + _resourcesRelativePath);

            var baseName = _applicationEnvironment.ApplicationName + "." + _resourcesRelativePath + typeInfo.FullName;

            log.LogInformation("baseName was " + baseName);

            ResourceManager manager = new ResourceManager(baseName, assembly);
            
            return new CustomResourceManagerStringLocalizer(
                manager,
                assembly,
                baseName,
                _resourceNamesCache,
                loggerFactory);
        }

        /// <summary>
        /// Creates a <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="baseName">The base name of the resource to load strings from.</param>
        /// <param name="location">The location to load resources from.</param>
        /// <returns>The <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer Create(string baseName, string location)
        {
            log.LogInformation("Create(string baseName, string location)");

            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            var rootPath = location ?? _applicationEnvironment.ApplicationName;

            log.LogInformation("location was " + location);
            log.LogInformation("baseName was " + baseName);
            log.LogInformation("rootPath was " + rootPath);

            var assembly = Assembly.Load(new AssemblyName(rootPath));

            if(assembly == null)
            {
                log.LogInformation("assembly was null");
            }
            else
            {
                log.LogInformation("assembly was " + assembly.FullName);
            }

            baseName = rootPath + "." + _resourcesRelativePath + baseName;

            log.LogInformation("baseName was updated " + baseName);

            ResourceManager manager = new ResourceManager(baseName, assembly);

            return new CustomResourceManagerStringLocalizer(
                manager,
                assembly,
                baseName,
                _resourceNamesCache,
                loggerFactory);
        }



    }
}
