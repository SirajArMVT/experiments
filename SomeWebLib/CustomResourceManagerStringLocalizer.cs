﻿

//https://github.com/aspnet/Localization/blob/dev/src/Microsoft.Extensions.Localization/ResourceManagerStringLocalizer.cs

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Localization.Internal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace SomeWebLib
{
    public class CustomResourceManagerStringLocalizer : IStringLocalizer
    {
        private readonly ConcurrentDictionary<string, object> _missingManifestCache =
            new ConcurrentDictionary<string, object>();

        private readonly IResourceNamesCache _resourceNamesCache;
        private readonly ResourceManager _resourceManager;
        private readonly AssemblyWrapper _resourceAssemblyWrapper;
        private readonly string _resourceBaseName;

        private ILoggerFactory loggerFactory;
        private ILogger log;

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="resourceManager">The <see cref="System.Resources.ResourceManager"/> to read strings from.</param>
        /// <param name="resourceAssembly">The <see cref="Assembly"/> that contains the strings as embedded resources.</param>
        /// <param name="baseName">The base name of the embedded resource in the <see cref="Assembly"/> that contains the strings.</param>
        /// <param name="resourceNamesCache">Cache of the list of strings for a given resource assembly name.</param>
        public CustomResourceManagerStringLocalizer(
            ResourceManager resourceManager,
            Assembly resourceAssembly,
            string baseName,
            IResourceNamesCache resourceNamesCache,
            ILoggerFactory loggerFactory
            )
            : this(resourceManager, new AssemblyWrapper(resourceAssembly), baseName, resourceNamesCache,loggerFactory)
        {
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException(nameof(resourceAssembly));
            }

            this.loggerFactory = loggerFactory;
            log = loggerFactory.CreateLogger<CustomResourceManagerStringLocalizer>();

        }

        /// <summary>
        /// Intended for testing purposes only.
        /// </summary>
        public CustomResourceManagerStringLocalizer(
            ResourceManager resourceManager,
            AssemblyWrapper resourceAssemblyWrapper,
            string baseName,
            IResourceNamesCache resourceNamesCache,
            ILoggerFactory loggerFactory
            )
        {
            if (resourceManager == null)
            {
                throw new ArgumentNullException(nameof(resourceManager));
            }

            if (resourceAssemblyWrapper == null)
            {
                throw new ArgumentNullException(nameof(resourceAssemblyWrapper));
            }

            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            if (resourceNamesCache == null)
            {
                throw new ArgumentNullException(nameof(resourceNamesCache));
            }

            _resourceAssemblyWrapper = resourceAssemblyWrapper;
            _resourceManager = resourceManager;
            _resourceBaseName = baseName;
            _resourceNamesCache = resourceNamesCache;

            this.loggerFactory = loggerFactory;
            log = loggerFactory.CreateLogger<CustomResourceManagerStringLocalizer>();
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var value = GetStringSafely(name, null);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var format = GetStringSafely(name, null);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return culture == null
                ? new CustomResourceManagerStringLocalizer(
                    _resourceManager,
                    _resourceAssemblyWrapper.Assembly,
                    _resourceBaseName,
                    _resourceNamesCache,
                    loggerFactory)
                : new CustomResourceManagerWithCultureStringLocalizer(
                    _resourceManager,
                    _resourceAssemblyWrapper.Assembly,
                    _resourceBaseName,
                    _resourceNamesCache,
                    loggerFactory,
                    culture);
        }

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures) =>
            GetAllStrings(includeAncestorCultures, CultureInfo.CurrentUICulture);

        /// <summary>
        /// Returns all strings in the specified culture.
        /// </summary>
        /// <param name="includeAncestorCultures"></param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get strings for.</param>
        /// <returns>The strings.</returns>
        protected IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            var resourceNames = includeAncestorCultures
                ? GetResourceNamesFromCultureHierarchy(culture)
                : GetResourceNamesForCulture(culture);

            foreach (var name in resourceNames)
            {
                var value = GetStringSafely(name, culture);
                yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        /// <summary>
        /// Gets a resource string from the <see cref="_resourceManager"/> and returns <c>null</c> instead of
        /// throwing exceptions if a match isn't found.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get the string for.</param>
        /// <returns>The resource string, or <c>null</c> if none was found.</returns>
        protected string GetStringSafely(string name, CultureInfo culture)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var cacheKey = $"name={name}&culture={(culture ?? CultureInfo.CurrentUICulture).Name}";

            if (_missingManifestCache.ContainsKey(cacheKey))
            {
                return null;
            }

            try
            {
                string result = culture == null ? _resourceManager.GetString(name) : _resourceManager.GetString(name, culture);
                return result;
            }
            catch (MissingManifestResourceException)
            {
                _missingManifestCache.TryAdd(cacheKey, null);

                log.LogInformation("MissingManifestResourceException for cacheKey " 
                    + cacheKey 
                    + " using _resourceBaseName " + this._resourceBaseName
                    + " and _resourceAssemblyWrapper" + this._resourceAssemblyWrapper.FullName
                    + " and _resourceManager " + this._resourceManager.ToString()
                    );

                return null;
            }
        }

        private IEnumerable<string> GetResourceNamesFromCultureHierarchy(CultureInfo startingCulture)
        {
            var currentCulture = startingCulture;
            var resourceNames = new HashSet<string>();

            while (true)
            {
                try
                {
                    var cultureResourceNames = GetResourceNamesForCulture(currentCulture);
                    foreach (var resourceName in cultureResourceNames)
                    {
                        resourceNames.Add(resourceName);
                    }
                }
                catch (MissingManifestResourceException) { }

                if (currentCulture == currentCulture.Parent)
                {
                    // currentCulture begat currentCulture, probably time to leave
                    break;
                }

                currentCulture = currentCulture.Parent;
            }

            return resourceNames;
        }

        private IList<string> GetResourceNamesForCulture(CultureInfo culture)
        {
            var resourceStreamName = _resourceBaseName;
            if (!string.IsNullOrEmpty(culture.Name))
            {
                resourceStreamName += "." + culture.Name;
            }
            resourceStreamName += ".resources";

            var cacheKey = $"assembly={_resourceAssemblyWrapper.FullName};resourceStreamName={resourceStreamName}";

            var cultureResourceNames = _resourceNamesCache.GetOrAdd(cacheKey, _ =>
            {
                var names = new List<string>();
                using (var cultureResourceStream = _resourceAssemblyWrapper.GetManifestResourceStream(resourceStreamName))
                using (var resources = new ResourceReader(cultureResourceStream))
                {
                    foreach (DictionaryEntry entry in resources)
                    {
                        var resourceName = (string)entry.Key;
                        names.Add(resourceName);
                    }
                }

                return names;
            });

            return cultureResourceNames;
        }



    }
}
