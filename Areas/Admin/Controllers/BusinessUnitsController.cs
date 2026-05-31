using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class BusinessUnitsController : Controller
{
    private readonly ApplicationDbContext _context;

    public BusinessUnitsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var query = _context.BusinessUnits.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => b.Name.Contains(search) || (b.Code != null && b.Code.Contains(search)));

        return View(await query.OrderBy(b => b.Name).ToListAsync());
    }

    public IActionResult Create() => View(new BusinessUnitEntity());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Code,Description,IsActive")] BusinessUnitEntity bu)
    {
        if (ModelState.IsValid)
        {
            _context.BusinessUnits.Add(bu);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Business Unit « {bu.Name} » créée.";
            return RedirectToAction(nameof(Index));
        }
        return View(bu);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var bu = await _context.BusinessUnits.FindAsync(id);
        if (bu == null) return NotFound();
        return View(bu);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,IsActive")] BusinessUnitEntity bu)
    {
        if (id != bu.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(bu);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Business Unit « {bu.Name} » mise à jour.";
            return RedirectToAction(nameof(Index));
        }
        return View(bu);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var bu = await _context.BusinessUnits
            .Include(b => b.Collaborateurs)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bu == null) return NotFound();

        if (bu.Collaborateurs.Count > 0)
        {
            TempData["Error"] = "Impossible de supprimer : cette Business Unit est assignée à des collaborateurs.";
            return RedirectToAction(nameof(Index));
        }

        _context.BusinessUnits.Remove(bu);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Business Unit « {bu.Name} » supprimée.";
        return RedirectToAction(nameof(Index));
    }
}
