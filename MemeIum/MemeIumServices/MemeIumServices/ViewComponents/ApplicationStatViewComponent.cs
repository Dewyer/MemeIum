using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MemeIumServices.ViewComponents
{
    public class ApplicationStatViewComponent : ViewComponent
    {

        public async Task<IViewComponentResult> InvokeAsync(ApplicationStatsViewModel model)
        {
            return View(model);
        }

    }
}
