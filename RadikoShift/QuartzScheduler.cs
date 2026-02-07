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
            var startAt = new DateTime(r.TargetDate!.Value, r.StartTime);

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
                            r.StartTime.Hour,
                            r.StartTime.Minute
                        )
                )
                .Build();
        }

        private ITrigger BuildWeeklyTrigger(Reservation r)
        {
            var days = new DayOfWeek[] { r.RepeatDays!.Value };

            if (!days.Any())
                throw new InvalidOperationException("Weekly reservation has no days");

            return TriggerBuilder.Create()
                .WithIdentity($"trigger-{r.Id}")
                .WithSchedule(
                    CronScheduleBuilder
                        .AtHourAndMinuteOnGivenDaysOfWeek(
                            r.StartTime.Hour,
                            r.StartTime.Minute,
                            days
                        )
                )
                .Build();
        }
    }
}
