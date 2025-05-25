using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketsApp.Controllers
{
    //[Authorize(Roles = "Cliente")]
    public class ClienteController : Controller
    {
        public IActionResult Tickets()
        {
            return View();
        }
    }
}
