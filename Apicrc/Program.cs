using System.Reflection;
using Apicrc.Extensions;
using Apicrc.Helpers.Errors;
using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var logger = new LoggerConfiguration()
					.ReadFrom.Configuration(builder.Configuration)
					.Enrich.FromLogContext()
					.CreateLogger();

//builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(Assembly.GetEntryAssembly());
builder.Services.ConfigureCors();
builder.Services.ConfigureRatelimiting();
builder.Services.ConfigureApiVersioning();
builder.Services.AddDbContext<CrcdbContext>(options =>
{
    string connectionString = builder.Configuration.GetConnectionString("ConexMysql");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseStatusCodePagesWithReExecute("/errors/{0}");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var loggerFactory = services.GetRequiredService<ILoggerFactory>();
	try
	{
		var context = services.GetRequiredService<CrcdbContext>();
		await context.Database.MigrateAsync();
	}
	catch (Exception ex)
	{
		var _logger = loggerFactory.CreateLogger<Program>();
		_logger.LogError(ex, "Ocurrio un error durante la migracion");
	}
}
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();

app.UseIpRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Run();
