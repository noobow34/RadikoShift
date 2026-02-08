using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadikoShift.EF;

namespace RadikoShift.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ShiftContext _db;
        private readonly QuartzScheduler _scheduler;

        public ReservationController(
            ShiftContext db,
            QuartzScheduler scheduler)
        {
            _db = db;
            _scheduler = scheduler;
        }

        public async Task<IActionResult> Index()
        {
            return View(await GetReservationsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateReservationRequest req)
        {
            var prg = await _db.Programs.FindAsync(req.ProgramId);
            var sta = await _db.Stations.FindAsync(prg!.StationId);

            var reservation = new Reservation
            {
                ProgramId = req.ProgramId,
                StationId = prg!.StationId!,
                StationName = sta!.Name,
                ProgramName = prg!.Title,
                CastName = prg!.CastName,

                StartTime = TimeOnly.FromDateTime(prg.StartTime!.Value),
                EndTime = TimeOnly.FromDateTime(prg.EndTime!.Value),

                TargetDate = req.RepeatType == RepeatType.Once
                            ? DateOnly.FromDateTime(prg.StartTime!.Value)
                            : null,

                RepeatType = req.RepeatType,
                RepeatDays = req.RepeatType == RepeatType.Weekly ? prg.StartTime!.Value.DayOfWeek : null,

                Status = ReservationStatus.Scheduled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync();

            await _scheduler.RegisterAsync(reservation);

            return Ok();
        }

        public async Task<IActionResult> ListPartial()
        {
            return PartialView("_ReservationList", await GetReservationsAsync());
        }

        private async Task<List<Reservation>> GetReservationsAsync()
        {
            return await _db.Reservations.Where(r => r.Status == ReservationStatus.Scheduled || r.Status == ReservationStatus.Running)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _db.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            await _scheduler.UnregisterAsync(reservation);
            reservation.Status = ReservationStatus.Canceled;
            reservation.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
