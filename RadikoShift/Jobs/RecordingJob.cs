using Quartz;
using RadikoShift.EF;
using System.Diagnostics;

namespace RadikoShift.Jobs
{
    public class RecordingJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            int reservationId = context.JobDetail.JobDataMap.GetInt("ReservationId")!;
            this.JournalWriteLine($"録音開始 予約ID:{reservationId}");

            ShiftContext shiftContext = new();
            var reservation = shiftContext.Reservations.Find(reservationId);
            if (reservation != null)
            {
                reservation.Status = ReservationStatus.Running;
                reservation.UpdatedAt = DateTime.Now;
                shiftContext.SaveChangesAsync();

                string radikoMail = Environment.GetEnvironmentVariable("RADIKO_MAIL") ?? "";
                string radikoPass = Environment.GetEnvironmentVariable("RADIKO_PASS") ?? "";
                string station = reservation.StationId.Replace($"{Define.Radiko.TypeName}_","");
                DateOnly baseDate;
                if (reservation.TargetDate == null)
                {
                    baseDate = DateOnly.FromDateTime(DateTime.Now);
                }
                else
                {
                    baseDate = reservation.TargetDate!.Value;
                }
                var startDateTime = new DateTime(baseDate ,reservation.StartTime);
                var endDateTime = new DateTime(baseDate, reservation.EndTime);
                // 日付またぎ対応
                if (endDateTime <= startDateTime)
                {
                    endDateTime = endDateTime.AddDays(1);
                }

                string startTime = startDateTime.ToString("yyyyMMddHHmmss");
                string endTime = endDateTime.ToString("yyyyMMddHHmmss");
                string fileName = $@"0_{baseDate:yyyyMMdd}_{reservation.ProgramName}.m4a";
                string arg = $@"-s {station} -f {startDateTime} -t {endDateTime} -m ""{radikoMail}"" -p ""{radikoPass}"" -o ""{fileName}""";
                ProcessStartInfo recProcess = new()
                {
                    FileName = "Tools/rec_radiko_ts.sh",
                    Arguments = arg
                };
                this.JournalWriteLine($"録音コマンド実行: {recProcess.FileName} {recProcess.Arguments}");
            }
            else
            {
                this.JournalWriteLine($"録音失敗 予約ID:{reservationId} が見つかりません");
                return Task.CompletedTask;
            }

            reservation.Status = ReservationStatus.Completed;
            reservation.UpdatedAt = DateTime.Now;
            shiftContext.SaveChangesAsync();

            return Task.CompletedTask;
        }
    }
}
