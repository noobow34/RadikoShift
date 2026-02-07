using Microsoft.AspNetCore.Mvc;
using Quartz.Core;
using RadikoShift.EF;
using System;

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

        [HttpPost]
        public async Task<IActionResult> Create(CreateReservationRequest req)
        {
            var prg = await _db.Programs.FindAsync(req.ProgramId);

            var reservation = new Reservation
            {
                ProgramId = req.ProgramId,
                StationId = prg!.StationId!,
                ProgramName = prg!.Title,

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
    }
}
