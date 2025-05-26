using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketsApp.Models;

namespace TicketsApp.Controllers
{
    public class EmpresaExternasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpresaExternasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: EmpresaExternas
        public async Task<IActionResult> Index()
        {
            return View(await _context.EmpresasExternas.ToListAsync());
        }

        // GET: EmpresaExternas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empresaExterna = await _context.EmpresasExternas
                .FirstOrDefaultAsync(m => m.EmpresaId == id);
            if (empresaExterna == null)
            {
                return NotFound();
            }

            return View(empresaExterna);
        }

        // GET: EmpresaExternas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: EmpresaExternas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([Bind("EmpresaId,NombreEmpresa,ContactoPrincipal,TelefonoEmpresa,DireccionEmpresa")] EmpresaExterna empresaExterna)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest("Error de validación del modelo: " + errores);
            }

            _context.Add(empresaExterna);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { id = empresaExterna.EmpresaId, nombre = empresaExterna.NombreEmpresa });
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: EmpresaExternas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empresaExterna = await _context.EmpresasExternas.FindAsync(id);
            if (empresaExterna == null)
            {
                return NotFound();
            }
            return View(empresaExterna);
        }

        // POST: EmpresaExternas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmpresaId,NombreEmpresa,ContactoPrincipal,TelefonoEmpresa,DireccionEmpresa")] EmpresaExterna empresaExterna)
        {
            if (id != empresaExterna.EmpresaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(empresaExterna);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmpresaExternaExists(empresaExterna.EmpresaId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(empresaExterna);
        }

        // GET: EmpresaExternas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empresaExterna = await _context.EmpresasExternas
                .FirstOrDefaultAsync(m => m.EmpresaId == id);
            if (empresaExterna == null)
            {
                return NotFound();
            }

            return View(empresaExterna);
        }

        // POST: EmpresaExternas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empresaExterna = await _context.EmpresasExternas.FindAsync(id);
            if (empresaExterna != null)
            {
                _context.EmpresasExternas.Remove(empresaExterna);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmpresaExternaExists(int id)
        {
            return _context.EmpresasExternas.Any(e => e.EmpresaId == id);
        }
    }
}
