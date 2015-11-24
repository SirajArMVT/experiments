
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SomeWebLib.Controllers
{
    public class FooController : Controller
    {
        public FooController(IHtmlLocalizer<FooController> localizer)
        {
            _localizer = localizer;
        }

        private readonly IHtmlLocalizer _localizer;

        public IActionResult Index()
        {
            // this does not get localised whenbrowser language is fr
            // even though the web app has Resources/SomeWebLib.Controllers.FooController.fr.resx
            ViewData["Message"] = _localizer["Learn More"];
            return View();
        }

    }
}
