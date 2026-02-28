using ATL;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RadikoShift.EF;
using System.Diagnostics;

namespace RadikoShift.Jobs
{
    public class RecordingJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                int reservationId = context.JobDetail.JobDataMap.GetInt("ReservationId")!;
                bool isPrev = context.JobDetail.JobDataMap.GetBoolean("IsPrev")!;
                if (isPrev)
                {
                    this.JournalWriteLine($"前回分の録音を実施");
                }
                this.JournalWriteLine($"録音開始 予約ID:{reservationId}");

                ShiftContext shiftContext = new();
                var reservation = shiftContext.Reservations.Find(reservationId);
                if (reservation != null)
                {
                    reservation.Status = ReservationStatus.Running;
                    reservation.UpdatedAt = DateTime.Now;
                    await shiftContext.SaveChangesAsync();

                    string radikoMail = Environment.GetEnvironmentVariable("RADIKO_MAIL") ?? "";
                    string radikoPass = Environment.GetEnvironmentVariable("RADIKO_PASS") ?? "";
                    string station = reservation.StationId;
                    DateOnly baseDate;
                    if (reservation.TargetDate == null)
                    {
                        if(isPrev)
                        {
                            if (reservation.RepeatType == RepeatType.Daily)
                            {
                                if (TimeOnly.FromDateTime(DateTime.Now) < reservation.StartTime)
                                {
                                    //開始時間に達してないので前回は前日
                                    baseDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
                                }
                                else
                                {
                                    //開始時間に達してるので前回は当日
                                    baseDate = DateOnly.FromDateTime(DateTime.Now);
                                }
                            }
                            else if(reservation.RepeatType == RepeatType.Weekly)
                            {
                                if (DateTime.Now.DayOfWeek == reservation.RepeatDays)
                                {
                                    if (TimeOnly.FromDateTime(DateTime.Now) < reservation.StartTime)
                                    {
                                        //開始時間に達してないので前回は一週間前
                                        baseDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
                                    }
                                    else
                                    {
                                        //開始時間に達してるので前回は当日
                                        baseDate = DateOnly.FromDateTime(DateTime.Now);
                                    }
                                }
                                else
                                {
                                    baseDate = DateOnly.FromDateTime(GetPreviousWeekday(DateTime.Now, reservation.RepeatDays!.Value));
                                }
                            }
                            else
                            {
                                baseDate = DateOnly.FromDateTime(DateTime.Now);
                            }
                        }
                        else
                        {
                            baseDate = DateOnly.FromDateTime(DateTime.Now);
                        }
                    }
                    else
                    {
                        baseDate = reservation.TargetDate!.Value;
                    }
                    var startDateTime = new DateTime(baseDate, reservation.StartTime);
                    var endDateTime = new DateTime(baseDate, reservation.EndTime);
                    // 日付またぎ対応
                    if (endDateTime <= startDateTime)
                    {
                        endDateTime = endDateTime.AddDays(1);
                        this.JournalWriteLine($"日付またぎ対応 録音終了日時を翌日に変更: {endDateTime}");
                    }

                    //繰り返し録音の場合は今回のProgram情報を取得
                    string? programName = reservation.ProgramName;
                    string? castName = reservation.CastName;
                    string? imageUrl = reservation.ImageUrl;
                    string programId = reservation.ProgramId;
                    if ((reservation.RepeatType == RepeatType.Weekly || reservation.RepeatType == RepeatType.Daily) && !reservation.IsManual!.Value)
                    {
                        this.JournalWriteLine($"繰り返し録音のためProgram情報を再取得 予約ID:{reservationId}");
                        var program = await shiftContext.Programs.Where(p => p.StationId == reservation.StationId && p.StartTime == startDateTime).FirstOrDefaultAsync();
                        if (program != null)
                        {
                            this.JournalWriteLine($"繰り返し録音のためProgram情報を再取得成功 予約ID:{reservationId}");
                            programId = program.Id;
                            programName = program.Title;
                            castName = program.CastName;
                            imageUrl = program.ImageUrl;
                        }
                    }

                    string startTime = startDateTime.ToString("yyyyMMddHHmmss");
                    string endTime = endDateTime.ToString("yyyyMMddHHmmss");
                    string fileName = $@"0_{baseDate:yyyyMMdd}_{programName}.m4a";
                    string arg1 = $@"-s {station} -f {startTime} -t {endTime} -o ""{fileName}""";
                    string arg2 = $@"-m ""{radikoMail}"" -p ""{radikoPass}""";
                    ProcessStartInfo recProcessInfo = new()
                    {
                        FileName = "Tools/rec_radiko_ts.sh",
                        Arguments = $"{arg1} {arg2}"
                    };
                    this.JournalWriteLine($"録音コマンド実行: {recProcessInfo.FileName} {arg1} -m *** -p ***");
                    Process recProcess = new()
                    {
                        StartInfo = recProcessInfo
                    };
                    recProcess.Start();
                    recProcess.WaitForExitAsync().Wait();
                    if (recProcess.ExitCode != 0)
                    {
                        this.JournalWriteLine($"録音失敗 予約ID:{reservationId} 録音コマンドの終了コード:{recProcess.ExitCode}");
                        reservation.Status = ReservationStatus.Failed;
                        reservation.UpdatedAt = DateTime.Now;
                        await shiftContext.SaveChangesAsync();
                        return;
                    }
                    //タグ埋め込み
                    Track recorded = new(fileName)
                    {
                        Title = programName,
                        Artist = $"{reservation.StationName}-{castName}"
                    };
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        //画像取得してタグ埋め込み
                        using var httpClient = new HttpClient();
                        var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                        var picture = PictureInfo.fromBinaryData(imageBytes, PictureInfo.PIC_TYPE.CD);
                        recorded.EmbeddedPictures.Add(picture);
                        this.JournalWriteLine($"録音ファイルタグ埋め込み 画像あり");
                    }
                    recorded.Save();
                    this.JournalWriteLine($"録音ファイルタグ埋め込み");

                    //保存             
                    byte[] recordedByte = File.ReadAllBytes(fileName);
                    var recording = new Recording
                    {
                        ReservationId = reservation.Id,
                        ProgramId = programId,
                        StationId = reservation.StationId,
                        StationName = reservation.StationName,
                        ProgramName = programName,
                        CastName = castName,

                        StartTime = startDateTime,
                        EndTime = endDateTime,

                        FileName = fileName,
                        MimeType = "audio/mp4",
                        FileSize = recordedByte.Length,
                        AudioData = recordedByte,

                        CreatedAt = DateTime.UtcNow
                    };
                    shiftContext.Recordings.Add(recording);
                    await shiftContext.SaveChangesAsync();
                    this.JournalWriteLine($"録音ファイルをDBに保存 保存ID:{recording.Id}");

                    //ファイル削除                
                    File.Delete(fileName);
                    this.JournalWriteLine($"録音ファイル削除: {fileName}");
                }
                else
                {
                    this.JournalWriteLine($"録音失敗 予約ID:{reservationId} が見つかりません");
                    return;
                }

                reservation.Status = ReservationStatus.Completed;
                reservation.UpdatedAt = DateTime.Now;
                await shiftContext.SaveChangesAsync();
                this.JournalWriteLine($"ステータス更新");
                this.JournalWriteLine($"録音完了 予約ID:{reservationId}");

                if (reservation.RepeatType == RepeatType.Weekly || reservation.RepeatType == RepeatType.Daily)
                {
                    reservation.Status = ReservationStatus.Scheduled;
                    reservation.UpdatedAt = DateTime.Now;
                    this.JournalWriteLine($"繰り返し予約のためステータスを再度予約中に更新 予約ID:{reservationId}");
                    await shiftContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                this.JournalWriteLine($"録音ジョブ実行中に例外が発生: {ex.ToString}");
            }
        }
        private DateTime GetPreviousWeekday(DateTime baseDate, DayOfWeek targetDay)
        {
            int diff = (7 + (baseDate.DayOfWeek - targetDay)) % 7;

            if (diff == 0)
                diff = 7; // 同じ曜日なら1週間前

            return baseDate.Date.AddDays(-diff);
        }
    }
}
