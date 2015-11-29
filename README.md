# Localization Experiments

This solution is modified from the [LocalizationWebSite](https://github.com/aspnet/Mvc/tree/dev/test/WebSites/LocalizationWebSite) sample from the MVC repo

I added a class library project named SomeWebLib to demonstrate that there is [a difference or bug in how to localize a controller that lives in a class library project](https://github.com/aspnet/Localization/issues/152).

In the Startup.cs ConfigureServices method we have:

    services.AddMvc().AddViewLocalization(options => options.ResourcesPath = "Resources");
    
Which means it should look for resx files in the /Reosurces folder of the web app.

In Configure we have:

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
    
    which is configuring the app to use en-US as the default but to also add en-GB and fr support.
    
    For my testing I'm setting my web browser language preference to fr to test French localization.
    
    ## Things that Work
    
    * Views are correclty localized if you are using a view for the language. ie with fr it uses 
    
        Views/Home/Index.fr.cshtml
  
  for the home controller index view.
  
  
## Things that don't work as expected
  
  HomeController.cs is taking constructor dependencies for 3 different IHtmlLocalizers
  
      public HomeController(
        IHtmlLocalizer<HomeController> localizer,
        IHtmlLocalizer<SharedResources> s,
        IHtmlLocalizer<SomeWebLib.CommonResources> localizer2)
      {
          _localizer = localizer;
          common = localizer2;
          shared = s;
      }
      
      private readonly IHtmlLocalizer _localizer;
      private readonly IHtmlLocalizer common;
      private readonly IHtmlLocalizer shared;
      
      public IActionResult Index()
      {
          ViewData["Message"] = common["Learn More"]; // this does not localize because CommonResources is in a separate classlibrary
          ViewData["Message2"] = shared["yes yes yes"]; //this does localize because SharedResources is directly in the web app
          ViewData["Message3"] = _localizer["Hello there!!"]; //this does work because HomeController is in the web app
          return View();
      }
      
  The 
  
      IHtmlLocalizer<SomeWebLib.CommonResources> 
      
  does not work as expected because it lives in a class library whereas
  
      IHtmlLocalizer<SharedResources>
      
  does work because it lives in the Web App
  
  To be clear I'm saying it does work if it finds the corresponding resx file in the Resources folder of the web app and uses it for localizing strings.
  
      SomeWebLib.CommonResources.fr.resx
      
  exists in the Resources folder of the web app but does not get used by IHtmlLocalizer<SomeWebLib.CommonResources>
  
  Similarly, there exists a FooController.cs in SomeWebLib that runs in the context of the LocalizationWebSite at /Foo
  
  It also does not get localized when using 
  
      IHtmlLocalizer<FooController>
      
  even though there exists within the web app Resources folder:
  
      SomeWebLib.Controllers.FooController.fr.resx
   
  I determined that reason the localization does not work for class library classes is because in line 66 of [ResourceManagerStringLocalizaerFactory](https://github.com/aspnet/Localization/blob/dev/src/Microsoft.Extensions.Localization/ResourceManagerStringLocalizerFactory.cs), the assembly is being assigned as the assembly that contains the type to be localized, and this assembly gets passed into the ResourceManager, therefore it does not look for the resx file in the Resources folder of the web app if the type tobelocalized is in a different assembly than the web app.
  
  To make it look for the resx file in the web app we have to pass in the assembly of the web app.
  
  I verified this by making [CustomResourceManagerStringLocalizerFactory.cs](https://github.com/joeaudette/experiments/blob/master/SomeWebLib/CustomResourceManagerStringLocalizerFactory.cs) where I added this right after the initial assignment of assembly:
  
      if(!(assembly.FullName.StartsWith(_applicationEnvironment.ApplicationName)))
    {
	    if(!(string.IsNullOrEmpty(options.ResourcesPath)))
	    {
		    assembly = Assembly.Load(_applicationEnvironment.ApplicationName);
	    }
    }
    
I plugged in the custom one in Startup.cs like this:

    services.TryAdd(new ServiceDescriptor(
        typeof(IStringLocalizerFactory),
        typeof(CustomResourceManagerStringLocalizerFactory),
        ServiceLifetime.Singleton));
        
and now both of my previous not working examples work, that is both IHtmlLocalizer<FooController> and IHtmlLocalizer<SomeWebLib.CommonResources> now localize strings using the corresponding resx files from the web app Resources folder.

I have commented out the line in Startup.cs that adds my custom StringLocalizerFactory so that the problem can be seen even though I have found a solution or workaround.

I'm waiting to find out if the asp.net team views this as a bug or not. I don't know if they intend us to use the same technique for localizing a controller that lives in a classlibrary or not.

