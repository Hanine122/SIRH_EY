using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class PositionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PositionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var query = _context.Positions
            .Include(p => p.SubDepartment)
                .ThenInclude(s => s!.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        return View(await query.OrderBy(p => p.Name).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await LoadSubDepartmentsAsync();
        return View(new Position());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Code,SubDepartmentId,Description,IsActive")] Position position)
    {
        if (ModelState.IsValid)
        {
            _context.Positions.Add(position);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Poste « {position.Name} » créé.";
            return RedirectToAction(nameof(Index));
        }
        await LoadSubDepartmentsAsync(position.SubDepartmentId);
        return View(position);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var position = await _context.Positions.FindAsync(id);
        if (position == null) return NotFound();
        await LoadSubDepartmentsAsync(position.SubDepartmentId);
        return View(position);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,SubDepartmentId,Description,IsActive")] Position position)
    {
        if (id != position.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(position);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Poste « {position.Name} » mis à jour.";
            return RedirectToAction(nameof(Index));
        }
        await LoadSubDepartmentsAsync(position.SubDepartmentId);
        return View(position);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var position = await _context.Positions
            .Include(p => p.Collaborateurs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null) return NotFound();

        if (position.Collaborateurs.Count > 0)
        {
            TempData["Error"] = "Impossible de supprimer : ce poste est assigné à des collaborateurs.";
            return RedirectToAction(nameof(Index));
        }

        _context.Positions.Remove(position);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Poste « {position.Name} » supprimé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadSubDepartmentsAsync(int? selectedId = null)
    {
        var subs = await _context.SubDepartments
            .Include(s => s.Department)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Department.Name)
            .ThenBy(s => s.Name)
            .Select(s => new { s.Id, Label = s.Department.Name + " / " + s.Name })
            .ToListAsync();

        ViewBag.SubDepartmentId = new SelectList(subs, "Id", "Label", selectedId);
    }
}
