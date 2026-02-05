using Auth0.AspNetCore.Authentication;
using Quartz;
using Quartz.Impl;
using RadikoShift.Jobs;

string auth0Domain = Environment.GetEnvironmentVariable("AUTH0_DOMAIN") ?? "";
string auth0ClientId = Environment.GetEnvironmentVariable("AUTH0_CLIENT_ID") ?? "";
Console.WriteLine($"AUTH0_ISSUER:{auth0Domain.Length}");
Console.WriteLine($"AUTH0_CLIENT_ID:{auth0ClientId.Length}");

string rsCs = Environment.GetEnvironmentVariable("RADIKOSHIFT_CONNECTION_STRING") ?? "";
Console.WriteLine($"RADIKOSHIFT_CONNECTION_STRING:{auth0ClientId.Length}");

var schedulerFactory = new StdSchedulerFactory();
var sch = await schedulerFactory.GetScheduler();
await sch.Start();
var jobDetailRP = JobBuilder.Create<RefreshStationsAndPrograms>()
                .WithIdentity("RefreshPrograms")
                .Build();
var triggerRP = TriggerBuilder.Create()
    .WithIdentity("RefreshPrograms")
    .StartNow()
    .WithCronSchedule("0 0 6 ? * * *")
    .Build();
await sch.ScheduleJob(jobDetailRP, triggerRP);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = auth0Domain;
    options.ClientId = auth0ClientId;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
