using Dagable.Consumer.Runner;
using Dagable.Consumer.DataAccess;
using Dagable.Consumer.DataAccess.Repositories;
using Dagable.Consumer.Domain.Entities;
using Dagable.Core;
using Dagable.Core.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Reflection;

using var host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

services.GetRequiredService<DagableDbContext>().Database.Migrate();

try
{
    await host.RunAsync();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

IHostBuilder CreateHostBuilder(string[] strings)
{
    return Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(app =>
        {
            app.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureServices((context, services) =>
        {
            var migrationsAssembly = typeof(DagableDbContext).GetTypeInfo().Assembly.GetName().Name;
            var mySqlConnectionStr = context.Configuration.GetConnectionString("DefaultConnection");

            services
                .AddDbContext<DagableDbContext>(opt =>
                {
                    opt.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr),
                        sql => sql.MigrationsAssembly(migrationsAssembly));
                    opt.UseMySql(ServerVersion.AutoDetect(mySqlConnectionStr),
                        b => b.SchemaBehavior(MySqlSchemaBehavior.Translate,
                            (schema, entity) => $"{schema ?? "dbo"}_{entity}"));
                })
                .AddScoped<IProcessor, Processor>()
                .AddScoped<IRepository<Job>, JobRepository>()
                .AddScoped<IRepository<Batch>, BatchRepository>()
                .AddDagableCoreServices()
                .AddDagableSchedulingServices()
                .Configure<AppOptions>(context.Configuration)
                .AddHostedService<Runner>();
        });
}