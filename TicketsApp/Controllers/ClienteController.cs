using Microsoft.AspNetCore.Mvc;

namespace TicketsApp.Controllers
{
    public class ClienteController : Controller
    {
        public IActionResult Tickets()
        {
            return View();
        }
    }
}
