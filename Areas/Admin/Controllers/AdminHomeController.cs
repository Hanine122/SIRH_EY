using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;

namespace SIRH.EY.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ITAdmin")]
public class AdminHomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminHomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.DepartmentCount     = await _context.Departments.CountAsync();
        ViewBag.SubDepartmentCount  = await _context.SubDepartments.CountAsync();
        ViewBag.PositionCount       = await _context.Positions.CountAsync();
        ViewBag.GradeCount          = await _context.Grades.CountAsync();
        ViewBag.BusinessUnitCount   = await _context.BusinessUnits.CountAsync();
        ViewBag.LocationCount       = await _context.Locations.CountAsync();
        ViewBag.ParameterCount      = await _context.SystemParameters.CountAsync();
        ViewBag.CollaborateurCount  = await _context.Collaborateurs.CountAsync(c => c.Actif);
        return View();
    }
}
