using ATL;
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
                string station = reservation.StationId;
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
                string arg = $@"-s {station} -f {startTime} -t {endTime} -m ""{radikoMail}"" -p ""{radikoPass}"" -o ""{fileName}""";
                ProcessStartInfo recProcessInfo = new()
                {
                    FileName = "Tools/rec_radiko_ts.sh",
                    Arguments = arg
                };
                this.JournalWriteLine($"録音コマンド実行: {recProcessInfo.FileName} {recProcessInfo.Arguments}");
                Process recProcess = new();
                recProcess.StartInfo = recProcessInfo;
                recProcess.Start();
                recProcess.WaitForExitAsync().Wait();
                if (recProcess.ExitCode != 0)
                {
                    this.JournalWriteLine($"録音失敗 予約ID:{reservationId} 録音コマンドの終了コード:{recProcess.ExitCode}");
                    reservation.Status = ReservationStatus.Failed;
                    reservation.UpdatedAt = DateTime.Now;
                    shiftContext.SaveChangesAsync();
                    return Task.CompletedTask;
                }
                //タグ埋め込み
                Track recorded = new(fileName);
                recorded.Title = reservation.ProgramName;
                recorded.Artist = $"{reservation.StationName}-{reservation.CastName}";
                recorded.Save();

                //保存

                //ファイル削除
                //File.Delete(fileName);

                this.JournalWriteLine($"録音完了 予約ID:{reservationId} ファイル名:{fileName}");
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
