using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var query = _context.Departments
            .Include(d => d.SubDepartments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Name.Contains(search) || (d.Code != null && d.Code.Contains(search)));

        return View(await query.OrderBy(d => d.Name).ToListAsync());
    }

    public IActionResult Create() => View(new Department());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Code,Description,IsActive")] Department department)
    {
        if (ModelState.IsValid)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Département « {department.Name} » créé.";
            return RedirectToAction(nameof(Index));
        }
        return View(department);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        return View(dept);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,IsActive")] Department department)
    {
        if (id != department.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(department);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Département « {department.Name} » mis à jour.";
            return RedirectToAction(nameof(Index));
        }
        return View(department);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        dept.IsActive = !dept.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Département « {dept.Name} » {(dept.IsActive ? "activé" : "désactivé")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var dept = await _context.Departments
            .Include(d => d.SubDepartments)
            .Include(d => d.Collaborateurs)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dept == null) return NotFound();

        if (dept.SubDepartments.Count > 0 || dept.Collaborateurs.Count > 0)
        {
            TempData["Error"] = "Impossible de supprimer : ce département contient des sous-départements ou des collaborateurs.";
            return RedirectToAction(nameof(Index));
        }

        _context.Departments.Remove(dept);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Département « {dept.Name} » supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
