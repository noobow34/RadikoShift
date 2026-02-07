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
                string arg1 = $@"-s {station} -f {startTime} -t {endTime} -o ""{fileName}""";
                string arg2 = $@"-m ""{radikoMail}"" -p ""{radikoPass}""";
                ProcessStartInfo recProcessInfo = new()
                {
                    FileName = "Tools/rec_radiko_ts.sh",
                    Arguments = $"{arg1} {arg2}"
                };
                this.JournalWriteLine($"録音コマンド実行: {recProcessInfo.FileName} {arg1} -m *** -p ***");
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
                this.JournalWriteLine($"録音ファイルタグ埋め込み");

                //保存             
                byte[] recordedByte = File.ReadAllBytes(fileName);
                var recording = new Recording
                {
                    ReservationId = reservation.Id,
                    ProgramId = reservation.ProgramId,
                    StationId = reservation.StationId,
                    StationName = reservation.StationName,
                    ProgramName = reservation.ProgramName,
                    CastName = reservation.CastName,

                    StartTime = startDateTime,
                    EndTime = endDateTime,

                    FileName = fileName,
                    MimeType = "audio/mp4",
                    FileSize = recordedByte.Length,
                    AudioData = recordedByte,

                    CreatedAt = DateTime.UtcNow
                };
                shiftContext.Recordings.Add(recording);
                shiftContext.SaveChangesAsync();
                this.JournalWriteLine($"録音ファイルをDBに保存 保存ID:{recording.Id}");

                //ファイル削除                
                File.Delete(fileName);
                this.JournalWriteLine($"録音ファイル削除: {fileName}");
            }
            else
            {
                this.JournalWriteLine($"録音失敗 予約ID:{reservationId} が見つかりません");
                return Task.CompletedTask;
            }

            reservation.Status = ReservationStatus.Completed;
            reservation.UpdatedAt = DateTime.Now;
            shiftContext.SaveChangesAsync();
            this.JournalWriteLine($"ステータス更新");
            this.JournalWriteLine($"録音完了 予約ID:{reservationId}");

            return Task.CompletedTask;
        }
    }
}
