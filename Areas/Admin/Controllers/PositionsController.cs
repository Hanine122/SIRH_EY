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

    // ── LIST ──────────────────────────────────────────────────────────────────

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

    // ── DETAILS + JUNCTION MANAGEMENT ────────────────────────────────────────

    public async Task<IActionResult> Details(int id)
    {
        var position = await _context.Positions
            .Include(p => p.SubDepartment).ThenInclude(s => s!.Department)
            .Include(p => p.RequiredCompetences)
            .Include(p => p.MandatoryFormations).ThenInclude(mf => mf.Formation)
            .Include(p => p.GradeEligibilities).ThenInclude(ge => ge.GradeEntity)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null) return NotFound();

        ViewBag.AvailableFormations = await _context.Formations
            .OrderBy(f => f.Titre)
            .Select(f => new SelectListItem { Value = f.Id.ToString(), Text = f.Titre })
            .ToListAsync();

        ViewBag.AvailableGrades = await _context.Grades
            .Where(g => g.IsActive)
            .OrderBy(g => g.Level)
            .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = $"N{g.Level} – {g.Name}" })
            .ToListAsync();

        return View(position);
    }

    // ── REQUIRED COMPETENCES ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRequiredCompetence(
        int positionId, string competenceName, string? category, int requiredLevel, bool isMandatory)
    {
        if (!await _context.Positions.AnyAsync(p => p.Id == positionId)) return NotFound();
        if (string.IsNullOrWhiteSpace(competenceName)) return BadRequest();

        _context.PositionRequiredCompetences.Add(new PositionRequiredCompetence
        {
            PositionId     = positionId,
            CompetenceName = competenceName.Trim(),
            Category       = category?.Trim(),
            RequiredLevel  = Math.Clamp(requiredLevel, 1, 5),
            IsMandatory    = isMandatory
        });
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Compétence « {competenceName} » ajoutée.";
        return RedirectToAction(nameof(Details), new { id = positionId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRequiredCompetence(int id, int positionId)
    {
        var item = await _context.PositionRequiredCompetences.FindAsync(id);
        if (item != null) _context.PositionRequiredCompetences.Remove(item);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = positionId });
    }

    // ── MANDATORY FORMATIONS ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMandatoryFormation(int positionId, int formationId, int? deadlineMonths)
    {
        if (!await _context.Positions.AnyAsync(p => p.Id == positionId)) return NotFound();
        if (!await _context.Formations.AnyAsync(f => f.Id == formationId)) return BadRequest();

        // Prevent duplicates
        if (!await _context.PositionMandatoryFormations.AnyAsync(
                pmf => pmf.PositionId == positionId && pmf.FormationId == formationId))
        {
            _context.PositionMandatoryFormations.Add(new PositionMandatoryFormation
            {
                PositionId     = positionId,
                FormationId    = formationId,
                DeadlineMonths = deadlineMonths
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Formation obligatoire ajoutée.";
        }
        else
        {
            TempData["Error"] = "Cette formation est déjà associée à ce poste.";
        }

        return RedirectToAction(nameof(Details), new { id = positionId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMandatoryFormation(int id, int positionId)
    {
        var item = await _context.PositionMandatoryFormations.FindAsync(id);
        if (item != null) _context.PositionMandatoryFormations.Remove(item);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = positionId });
    }

    // ── GRADE ELIGIBILITIES ───────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGradeEligibility(int positionId, int gradeEntityId, bool isMinimum)
    {
        if (!await _context.Positions.AnyAsync(p => p.Id == positionId)) return NotFound();

        if (!await _context.PositionGradeEligibilities.AnyAsync(
                pge => pge.PositionId == positionId && pge.GradeEntityId == gradeEntityId))
        {
            _context.PositionGradeEligibilities.Add(new PositionGradeEligibility
            {
                PositionId    = positionId,
                GradeEntityId = gradeEntityId,
                IsMinimum     = isMinimum
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Grade ajouté.";
        }
        else
        {
            TempData["Error"] = "Ce grade est déjà associé à ce poste.";
        }

        return RedirectToAction(nameof(Details), new { id = positionId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveGradeEligibility(int id, int positionId)
    {
        var item = await _context.PositionGradeEligibilities.FindAsync(id);
        if (item != null) _context.PositionGradeEligibilities.Remove(item);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = positionId });
    }

    // ── CREATE / EDIT / DELETE ────────────────────────────────────────────────

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
            position.CreatedBy = User.Identity?.Name;
            position.CreatedAt = DateTime.UtcNow;
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
            var existing = await _context.Positions.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name            = position.Name;
            existing.Code            = position.Code;
            existing.SubDepartmentId = position.SubDepartmentId;
            existing.Description     = position.Description;
            existing.IsActive        = position.IsActive;
            existing.UpdatedBy       = User.Identity?.Name;
            existing.UpdatedAt       = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Poste « {existing.Name} » mis à jour.";
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
