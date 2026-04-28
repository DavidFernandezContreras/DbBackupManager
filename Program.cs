using DbBackupManager.HostedServices;
using DbBackupManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<BackupRestoreOptions>(
    builder.Configuration.GetSection("BackupRestore"));
builder.Services.Configure<ConnectivityOptions>(
    builder.Configuration.GetSection("Connectivity"));

builder.Services.AddSingleton<EventLogService>();
builder.Services.AddSingleton<ConnectivityState>();
builder.Services.AddSingleton<BackupScheduleState>();
builder.Services.AddSingleton<BackupRestoreExecutor>();

builder.Services.AddHostedService<BackupRestoreHostedService>();
builder.Services.AddHostedService<ConnectivityMonitorHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/error");

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
