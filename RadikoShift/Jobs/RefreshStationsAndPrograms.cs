using Microsoft.EntityFrameworkCore;
using Npgsql.Bulk;
using Quartz;
using RadikoShift.Data;
using RadikoShift.Infrastructure;
using RadikoShift.Radiko;
using SlackNet;
using SlackNet.WebApi;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RadikoShift.Jobs
{
    public class RefreshStationsAndPrograms : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                this.JournalWriteLine("番組表更新開始");

                string radikoMail = Environment.GetEnvironmentVariable("RADIKO_MAIL") ?? "";
                string radikoPass = Environment.GetEnvironmentVariable("RADIKO_PASS") ?? "";

                // ログイン済み HttpClient を生成（Cookieが正しく引き継がれる）
                using var httpClient = await RadikoClient.CreateHttpClient(radikoMail, radikoPass);
                this.JournalWriteLine("ログイン完了");

                Stopwatch sw = new();
                sw.Start();

                int partitionCount = AppSettingsService.DefaultParallelCount;
                if (context.JobDetail.JobDataMap.ContainsKey("ParallelCount"))
                    partitionCount = context.JobDetail.JobDataMap.GetInt("ParallelCount");
                this.JournalWriteLine($"並列数:{partitionCount}");

                this.JournalWriteLine("放送局取得");
                var stations = await RadikoClient.GetStations(true, httpClient);

                ShiftContext sContext = new();
                this.JournalWriteLine("stations全件削除");
                await sContext.Stations.ExecuteDeleteAsync();
                this.JournalWriteLine("programs全件削除");
                await sContext.Programs.ExecuteDeleteAsync();

                var stationPartitions = Partitioner
                    .Create(stations, EnumerablePartitionerOptions.NoBuffering)
                    .GetPartitions(partitionCount);

                List<Task> tasks = [];
                List<Data.Program> programs = [];

                int taskId = 0;
                foreach (var partition in stationPartitions)
                {
                    taskId++;
                    int currentTaskId = taskId;
                    tasks.Add(Task.Run(async () =>
                    {
                        var localPrograms = new List<Data.Program>();
                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                var station = partition.Current;
                                this.JournalWriteLine($"T{currentTaskId}:{station.Name!}");
                                var progs = await RadikoClient.GetPrograms(station, httpClient);
                                localPrograms.AddRange(progs);
                            }
                        }
                        lock (programs)
                        {
                            programs.AddRange(localPrograms);
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                this.JournalWriteLine("保存");
                var uploader = new NpgsqlBulkUploader(sContext);
                uploader.Insert(stations);
                uploader.Insert(programs);
                sw.Stop();
                this.JournalWriteLine($"番組表更新終了:{sw}");
            }
            catch (Exception ex)
            {
                var api = new SlackServiceBuilder()
                    .UseApiToken(Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN"))
                    .GetApiClient();
                string errorMessage = $"放送局・番組表更新ジョブ実行中に例外が発生:{ex.StackTrace}";
                await api.Chat.PostMessage(new Message { Text = errorMessage, Channel = Environment.GetEnvironmentVariable("SLACK_NOTIFY_CHANNEL") });
                this.JournalWriteLine(errorMessage);
            }
        }
    }
}
