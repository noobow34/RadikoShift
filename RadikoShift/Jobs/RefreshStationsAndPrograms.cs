using Quartz;
using RadikoShift.EF;
using RadikoShift.Radio;

namespace RadikoShift.Jobs
{
    public class RefreshStationsAndPrograms : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            string radikoMail = Environment.GetEnvironmentVariable("RADIKO_MAIL") ?? "";
            string radikoPass = Environment.GetEnvironmentVariable("RADIKO_PASS") ?? "";
            Radiko.Login(radikoMail, radikoPass).Wait();

            var stations = Radiko.GetStations(true).Result;
            ShiftContext sContext = new();
            var existStations =  sContext.Stations.Where(s => (stations.Select(s => s.Id).ToArray().Contains(s.Id)));
            sContext.RemoveRange(existStations);
            sContext.Stations.AddRange(stations);
            foreach (var station in stations)
            {
                try
                {
                    var programs = Radiko.GetPrograms(station).Result;
                    var existPrograms = sContext.Programs.Where(p => (programs.Select(p => p.Id).ToArray().Contains(p.Id)));
                    sContext.RemoveRange(existPrograms);
                    sContext.Programs.AddRange(programs);
                }
                catch (Exception e)
                {
                    
                }
            }
            sContext.SaveChanges();

            return Task.CompletedTask;
        }
    }
}

