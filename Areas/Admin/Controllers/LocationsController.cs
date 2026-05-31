using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class LocationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public LocationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var query = _context.Locations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Name.Contains(search) || (l.City != null && l.City.Contains(search)));

        return View(await query.OrderBy(l => l.Country).ThenBy(l => l.Name).ToListAsync());
    }

    public IActionResult Create() => View(new LocationEntity());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,City,Country,IsActive")] LocationEntity location)
    {
        if (ModelState.IsValid)
        {
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Localisation « {location.Name} » créée.";
            return RedirectToAction(nameof(Index));
        }
        return View(location);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var location = await _context.Locations.FindAsync(id);
        if (location == null) return NotFound();
        return View(location);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,City,Country,IsActive")] LocationEntity location)
    {
        if (id != location.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(location);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Localisation « {location.Name} » mise à jour.";
            return RedirectToAction(nameof(Index));
        }
        return View(location);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var location = await _context.Locations
            .Include(l => l.Collaborateurs)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (location == null) return NotFound();

        if (location.Collaborateurs.Count > 0)
        {
            TempData["Error"] = "Impossible de supprimer : cette localisation est assignée à des collaborateurs.";
            return RedirectToAction(nameof(Index));
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Localisation « {location.Name} » supprimée.";
        return RedirectToAction(nameof(Index));
    }
}
