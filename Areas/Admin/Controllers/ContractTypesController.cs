using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class ContractTypesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ContractTypesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var query = _context.ContractTypes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(ct => ct.Name.Contains(search) || (ct.Code != null && ct.Code.Contains(search)));

        return View(await query.OrderBy(ct => ct.Name).ToListAsync());
    }

    public IActionResult Create() => View(new ContractType());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Code,Description,MaxDurationMonths,IsActive")] ContractType ct)
    {
        if (ModelState.IsValid)
        {
            ct.CreatedBy  = User.Identity?.Name;
            ct.CreatedAt  = DateTime.UtcNow;
            _context.ContractTypes.Add(ct);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Type de contrat « {ct.Name} » créé.";
            return RedirectToAction(nameof(Index));
        }
        return View(ct);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var ct = await _context.ContractTypes.FindAsync(id);
        if (ct == null) return NotFound();
        return View(ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,MaxDurationMonths,IsActive")] ContractType ct)
    {
        if (id != ct.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            var existing = await _context.ContractTypes.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name               = ct.Name;
            existing.Code               = ct.Code;
            existing.Description        = ct.Description;
            existing.MaxDurationMonths  = ct.MaxDurationMonths;
            existing.IsActive           = ct.IsActive;
            existing.UpdatedBy          = User.Identity?.Name;
            existing.UpdatedAt          = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Type de contrat « {existing.Name} » mis à jour.";
            return RedirectToAction(nameof(Index));
        }
        return View(ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var ct = await _context.ContractTypes.FindAsync(id);
        if (ct == null) return NotFound();
        ct.IsActive   = !ct.IsActive;
        ct.UpdatedBy  = User.Identity?.Name;
        ct.UpdatedAt  = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"« {ct.Name} » {(ct.IsActive ? "activé" : "désactivé")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ct = await _context.ContractTypes
            .Include(c => c.Collaborateurs)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (ct == null) return NotFound();

        if (ct.Collaborateurs.Count > 0)
        {
            TempData["Error"] = "Impossible de supprimer : ce type de contrat est assigné à des collaborateurs.";
            return RedirectToAction(nameof(Index));
        }

        _context.ContractTypes.Remove(ct);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Type de contrat « {ct.Name} » supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
