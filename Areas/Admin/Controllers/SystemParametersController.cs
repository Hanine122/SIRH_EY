using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class SystemParametersController : Controller
{
    private readonly ApplicationDbContext _context;

    public SystemParametersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? category)
    {
        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.Categories = await _context.SystemParameters
            .Where(p => p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var query = _context.SystemParameters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Key.Contains(search) || p.Value.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        return View(await query.OrderBy(p => p.Category).ThenBy(p => p.Key).ToListAsync());
    }

    public IActionResult Create() => View(new SystemParameter());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Key,Value,Category,Description,IsVisible,IsEditable")] SystemParameter param)
    {
        if (await _context.SystemParameters.AnyAsync(p => p.Key == param.Key))
            ModelState.AddModelError("Key", "Cette clé existe déjà.");

        if (ModelState.IsValid)
        {
            param.ModifiedBy  = User.Identity?.Name;
            param.ModifiedDate = DateTime.UtcNow;
            _context.SystemParameters.Add(param);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Paramètre « {param.Key} » créé.";
            return RedirectToAction(nameof(Index));
        }
        return View(param);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var param = await _context.SystemParameters.FindAsync(id);
        if (param == null) return NotFound();
        return View(param);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Key,Value,Category,Description,IsVisible,IsEditable")] SystemParameter param)
    {
        if (id != param.Id) return BadRequest();

        var existing = await _context.SystemParameters.FindAsync(id);
        if (existing == null) return NotFound();

        if (!existing.IsEditable)
        {
            TempData["Error"] = "Ce paramètre système est en lecture seule.";
            return RedirectToAction(nameof(Index));
        }

        if (ModelState.IsValid)
        {
            existing.Value       = param.Value;
            existing.Category    = param.Category;
            existing.Description = param.Description;
            existing.IsVisible   = param.IsVisible;
            existing.IsEditable  = param.IsEditable;
            existing.ModifiedBy  = User.Identity?.Name;
            existing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Paramètre « {existing.Key} » mis à jour.";
            return RedirectToAction(nameof(Index));
        }
        return View(param);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var param = await _context.SystemParameters.FindAsync(id);
        if (param == null) return NotFound();

        if (!param.IsEditable)
        {
            TempData["Error"] = "Ce paramètre système est en lecture seule et ne peut pas être supprimé.";
            return RedirectToAction(nameof(Index));
        }

        _context.SystemParameters.Remove(param);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Paramètre « {param.Key} » supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
