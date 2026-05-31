using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class SubDepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SubDepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int? departmentId)
    {
        ViewBag.Search = search;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Departments = new SelectList(
            await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(),
            "Id", "Name", departmentId);

        var query = _context.SubDepartments
            .Include(s => s.Department)
            .Include(s => s.Positions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search));

        if (departmentId.HasValue)
            query = query.Where(s => s.DepartmentId == departmentId.Value);

        return View(await query.OrderBy(s => s.Department.Name).ThenBy(s => s.Name).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await LoadDepartmentsAsync();
        return View(new SubDepartment());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Code,DepartmentId,Description,IsActive")] SubDepartment sub)
    {
        if (ModelState.IsValid)
        {
            _context.SubDepartments.Add(sub);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sous-département « {sub.Name} » créé.";
            return RedirectToAction(nameof(Index));
        }
        await LoadDepartmentsAsync(sub.DepartmentId);
        return View(sub);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var sub = await _context.SubDepartments.FindAsync(id);
        if (sub == null) return NotFound();
        await LoadDepartmentsAsync(sub.DepartmentId);
        return View(sub);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,DepartmentId,Description,IsActive")] SubDepartment sub)
    {
        if (id != sub.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            _context.Update(sub);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sous-département « {sub.Name} » mis à jour.";
            return RedirectToAction(nameof(Index));
        }
        await LoadDepartmentsAsync(sub.DepartmentId);
        return View(sub);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sub = await _context.SubDepartments
            .Include(s => s.Positions)
            .Include(s => s.Collaborateurs)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sub == null) return NotFound();

        if (sub.Positions.Count > 0 || sub.Collaborateurs.Count > 0)
        {
            TempData["Error"] = "Impossible de supprimer : ce sous-département contient des postes ou des collaborateurs.";
            return RedirectToAction(nameof(Index));
        }

        _context.SubDepartments.Remove(sub);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Sous-département « {sub.Name} » supprimé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDepartmentsAsync(int? selectedId = null)
    {
        ViewBag.DepartmentId = new SelectList(
            await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(),
            "Id", "Name", selectedId);
    }
}
