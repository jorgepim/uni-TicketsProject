using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketsApp.Controllers
{

    [Authorize(Roles = "Tecnico")]
    public class TecnicoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
