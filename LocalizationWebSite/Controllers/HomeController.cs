// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Localization;
//using SomeWebLib;

namespace LocalizationWebSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHtmlLocalizer _localizer;
        private readonly IHtmlLocalizer common;

        public HomeController(
            IHtmlLocalizer<HomeController> localizer,
            IHtmlLocalizer<SomeWebLib.CommonResources> localizer2)
        {
            _localizer = localizer;
            common = localizer2;
        }

        public IActionResult Index()
        {
            ViewData["Message"] = common["Learn More"];
            return View();
        }

        public IActionResult Locpage()
        {
            ViewData["Message"] = _localizer["Learn More"];
            return View();
        }
    }
}
