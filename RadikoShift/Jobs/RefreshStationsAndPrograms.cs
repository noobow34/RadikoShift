using Microsoft.EntityFrameworkCore;
using Npgsql.Bulk;
using Quartz;
using RadikoShift.EF;
using RadikoShift.Radio;
using System.Collections.Concurrent;

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
                Radiko.Login(radikoMail, radikoPass).Wait();

                this.JournalWriteLine("放送局取得");
                var stations = await Radiko.GetStations(true);
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

                foreach (var partition in stationPartitions)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var localPrograms = new List<EF.Program>();

                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                var station = partition.Current;

                                this.JournalWriteLine(station.Name!);

                                var programs = await Radiko.GetPrograms(station);
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
                this.JournalWriteLine("番組表更新終了");
            }
            catch (Exception ex)
            {
                this.JournalWriteLine($"放送局・番組表更新ジョブ実行中に例外が発生:{ex.ToString}");
            }
        }
    }
}

