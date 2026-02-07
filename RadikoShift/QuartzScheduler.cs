using Quartz;
using RadikoShift.EF;
using RadikoShift.Jobs;

namespace RadikoShift
{
    public class QuartzScheduler
    {
        private readonly IScheduler _scheduler;

        public QuartzScheduler(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async Task RegisterAsync(Reservation reservation)
        {
            var jobKey = new JobKey($"reservation-{reservation.Id}");

            var job = JobBuilder.Create<RecordingJob>()
                .WithIdentity(jobKey)
                .UsingJobData("ReservationId", reservation.Id.ToString())
                .Build();

            var trigger = BuildTrigger(reservation);

            await _scheduler.ScheduleJob(job, trigger);
        }

        private ITrigger BuildTrigger(Reservation r)
        {
            return r.RepeatType switch
            {
                RepeatType.Once => BuildOnceTrigger(r),
                RepeatType.Daily => BuildDailyTrigger(r),
                RepeatType.Weekly => BuildWeeklyTrigger(r),
                _ => throw new NotSupportedException()
            };
        }

        private ITrigger BuildOnceTrigger(Reservation r)
        {
            var startAt = new DateTime(r.TargetDate!.Value, r.EndTime);
            if (r.EndTime <= r.StartTime)
            {
                //日またぎ
                startAt = startAt.AddDays(1);
            }
            //放送終了後少ししてからスタート
            startAt = startAt.AddMinutes(5);

            return TriggerBuilder.Create()
                .WithIdentity($"trigger-{r.Id}")
                .StartAt(startAt)
                .WithSimpleSchedule(x => x.WithRepeatCount(0))
                .Build();
        }

        private ITrigger BuildDailyTrigger(Reservation r)
        {
            return TriggerBuilder.Create()
                .WithIdentity($"trigger-{r.Id}")
                .WithSchedule(
                    CronScheduleBuilder
                        .DailyAtHourAndMinute(
                            r.EndTime.Hour,
                            r.EndTime.Minute
                        )
                )
                .Build();
        }

        private ITrigger BuildWeeklyTrigger(Reservation r)
        {

            var days = new DayOfWeek[] { r.RepeatDays!.Value };
            if (r.EndTime <= r.StartTime)
            {
                //日またぎ
                days[0] = days[0] + 1;
            }
            var jobStart = r.EndTime.AddMinutes(5);

            return TriggerBuilder.Create()
                .WithIdentity($"trigger-{r.Id}")
                .WithSchedule(
                    CronScheduleBuilder
                        .AtHourAndMinuteOnGivenDaysOfWeek(
                            jobStart.Hour,
                            jobStart.Minute,
                            days
                        )
                )
                .Build();
        }
    }
}
