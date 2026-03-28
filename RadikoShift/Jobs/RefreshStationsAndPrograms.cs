using Microsoft.EntityFrameworkCore;
using Npgsql.Bulk;
using Quartz;
using RadikoShift.EF;
using RadikoShift.Radio;
using SlackNet;
using SlackNet.WebApi;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace RadikoShift.Jobs
{
    public class RefreshStationsAndPrograms : IJob
    {
        private static readonly HttpClient _httpClient = new(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }
        );

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                this.JournalWriteLine("番組表更新開始");

                string radikoMail = Environment.GetEnvironmentVariable("RADIKO_MAIL") ?? "";
                string radikoPass = Environment.GetEnvironmentVariable("RADIKO_PASS") ?? "";
                Radiko.Login(radikoMail, radikoPass).Wait();

                Stopwatch sw = new();
                sw.Start();

                this.JournalWriteLine("放送局取得");
                var stations = await Radiko.GetStations(true,_httpClient);
                ShiftContext sContext = new();
                this.JournalWriteLine("stations全件削除");
                await sContext.Stations.ExecuteDeleteAsync();
                this.JournalWriteLine("programs全件削除");
                await sContext.Programs.ExecuteDeleteAsync();
                
                const int PartitionCount = 10;
                var stationPartitions = Partitioner
                    .Create(stations, EnumerablePartitionerOptions.NoBuffering)
                    .GetPartitions(PartitionCount);

                List<Task> tasks = [];
                List<EF.Program> programs = [];

                int taskId = 0;
                foreach (var partition in stationPartitions)
                {
                    taskId++;
                    int currentTaskId = taskId;
                    tasks.Add(Task.Run(async () =>
                    {
                        var localPrograms = new List<EF.Program>();

                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                var station = partition.Current;

                                this.JournalWriteLine($"T{currentTaskId}:{station.Name!}");

                                var programs = await Radiko.GetPrograms(station,_httpClient);
                                localPrograms.AddRange(programs);
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

