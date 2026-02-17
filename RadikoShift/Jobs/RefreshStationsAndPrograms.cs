using Quartz;
using RadikoShift.EF;
using RadikoShift.Radio;

namespace RadikoShift.Jobs
{
    public class RefreshStationsAndPrograms : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            this.JournalWriteLine("番組表更新開始");

            string radikoMail = Environment.GetEnvironmentVariable("RADIKO_MAIL") ?? "";
            string radikoPass = Environment.GetEnvironmentVariable("RADIKO_PASS") ?? "";
            Radiko.Login(radikoMail, radikoPass).Wait();

            this.JournalWriteLine("放送局取得");
            var stations = Radiko.GetStations(true).Result;
            ShiftContext sContext = new();
            var areas = sContext.Areas.ToDictionary(a => a.AreaCode , a => a.AreaName);
            foreach (var station in stations)
            {
                if (areas.TryGetValue(station.Area!, out string? value))
                {
                    station.AreaName = value;
                }
            }
            var existStations =  sContext.Stations;
            sContext.RemoveRange(existStations);
            sContext.Stations.AddRange(stations);
            var existPrograms = sContext.Programs;
            sContext.RemoveRange(existPrograms);
            foreach (var station in stations)
            {
                this.JournalWriteLine(station.Name!);
                var programs = Radiko.GetPrograms(station).Result;
                sContext.Programs.AddRange(programs);
            }
            this.JournalWriteLine("保存");
            sContext.SaveChanges();
            this.JournalWriteLine("番組表更新終了");

            return Task.CompletedTask;
        }
    }
}

