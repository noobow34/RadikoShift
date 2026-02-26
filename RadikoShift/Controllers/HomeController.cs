using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadikoShift.EF;
using RadikoShift.Models;
using RadikoShift.ViewModel;
using System.Diagnostics;

namespace RadikoShift.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ShiftContext _db;
        public HomeController(ShiftContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var reagions = _db.Stations.Where(s => s.RegionId != "zenkoku")
                .GroupBy(s => new { s.RegionId, s.RegionName })
                .Select(g => new IdNamePair() { Id = g.Key.RegionId,Name = g.Key.RegionName,DisplayOrder = g.Min(x => x.DisplayOrder) ?? 0})
                .OrderBy(r => r.DisplayOrder)
                .ToList();
            string minDate = _db.Programs.Min(p => p.StartTime)!.Value.ToString("yyyy-MM-dd");
            string maxDate = _db.Programs.Max(p => p.StartTime)!.Value.ToString("yyyy-MM-dd");
            var vm = new ProgramFilterViewModel() { Reagions = reagions,MinDate = minDate,MaxDate = maxDate };

            return View(vm);
        }

        [HttpGet]
        public IActionResult GetStations(string region)
        {
            var stations = _db.Stations.Where(s => s.RegionId == region).Include(s => s.Area)
                .Select(g => new IdNamePair() { Id = g.Id, Name = $"{g.Area!.AreaName}-{g.Name}", DisplayOrder = g.DisplayOrder ?? 0})
                .OrderBy(o => o.DisplayOrder)
                .ToList();

            stations.AddRange(_db.Stations.Where(s => s.RegionId == "zenkoku").Include(s => s.Area)
                .Select(g => new IdNamePair() { Id = g.Id, Name = $"{g.Area!.AreaName}-{g.Name}", DisplayOrder = g.DisplayOrder ?? 0}).ToList());

            return Json(stations);
        }

        public IActionResult ProgramTablePartial(string stationId,DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var programs = _db.Programs
                .Where(p =>
                    p.StationId == stationId &&
                    p.StartTime >= start &&
                    p.StartTime < end)
                .OrderBy(p => p.StartTime)
                .Select(p => new ProgramItemViewModel
                {
                    ProgramId = p.PId,
                    StartTime = p.StartTime!.Value,
                    EndTime = p.EndTime!.Value,
                    Title = p.Title ?? "",
                    CastName = p.CastName,
                    Description = p.Description
                })
                .ToList();

            return PartialView("_ProgramTable", programs);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
