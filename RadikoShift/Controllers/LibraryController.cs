using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadikoShift.EF;
using RadikoShift.ViewModel;
using System;

namespace RadikoShift.Controllers
{
    public class LibraryController : Controller
    {
        private readonly ShiftContext _db;

        public LibraryController(ShiftContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var list = await _db.Recordings
                        .OrderByDescending(r => r.CreatedAt)
                        .Select(r => new LibraryListViewModel
                        {
                            Id = r.Id,
                            ProgramName = r.ProgramName,
                            CastName = r.CastName,
                            StationName = r.StationName ?? r.StationId,
                            StartTime = r.StartTime,
                            EndTime = r.EndTime,
                            FileName = r.FileName,
                            FileSize = r.FileSize
                        })
                        .ToListAsync();

            return View(list);
        }

        public async Task<IActionResult> Download(int id)
        {
            var rec = await _db.Recordings.FindAsync(id);
            if (rec == null)
                return NotFound();

            return File(
                rec.AudioData,
                rec.MimeType ?? "audio/mp4",
                rec.FileName
            );
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var rec = await _db.Recordings.FindAsync(id);
            if (rec == null)
                return NotFound();

            _db.Recordings.Remove(rec);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
