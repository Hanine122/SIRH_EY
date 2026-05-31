using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class GradesController : Controller
{
    private readonly ApplicationDbContext _context;

    public GradesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
        => View(await _context.Grades.OrderBy(g => g.Level).ToListAsync());

    public IActionResult Create() => View(new GradeEntity());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Level,Description,IsActive")] GradeEntity grade)
    {
        if (ModelState.IsValid)
        {
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Grade « {grade.Name} » créé.";
            return RedirectToAction(nameof(Index));
        }
        return View(grade);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var grade = await _context.Grades.FindAsync(id);
        if (grade == null) return NotFound();
        return View(grade);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Level,Description,IsActive")] GradeEntity grade)
    {
        if (id != grade.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(grade);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Grade « {grade.Name} » mis à jour.";
            return RedirectToAction(nameof(Index));
        }
        return View(grade);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var grade = await _context.Grades
            .Include(g => g.Collaborateurs)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade == null) return NotFound();

        if (grade.Collaborateurs.Count > 0)
        {
            TempData["Error"] = "Impossible de supprimer : ce grade est assigné à des collaborateurs.";
            return RedirectToAction(nameof(Index));
        }

        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Grade « {grade.Name} » supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
