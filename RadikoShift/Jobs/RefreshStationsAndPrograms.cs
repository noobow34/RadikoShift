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
            var existStations =  sContext.Stations.Where(s => (stations.Select(s => s.Id).ToArray().Contains(s.Id)));
            sContext.RemoveRange(existStations);
            sContext.Stations.AddRange(stations);
            foreach (var station in stations)
            {
                this.JournalWriteLine(station.Name!);
                var programs = Radiko.GetPrograms(station).Result;
                var existPrograms = sContext.Programs.Where(p => (programs.Select(p => p.Id).ToArray().Contains(p.Id)));
                sContext.RemoveRange(existPrograms);
                sContext.Programs.AddRange(programs);
            }
            this.JournalWriteLine("保存");
            sContext.SaveChanges();
            this.JournalWriteLine("番組表更新終了");

            return Task.CompletedTask;
        }
    }
}

