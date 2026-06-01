using EZ.Job.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddEZJob()
    .AddMsSqlStore("Server=localhost;Database=ez_jobs;User Id=sa;Password=Root@123;TrustServerCertificate=True");

builder.Services.AddHostedService<SampleRunner>();

using var host = builder.Build();
await host.RunAsync();

public class SampleRunner : BackgroundService
{
    private readonly IJobDispatcher _dispatcher;

    public SampleRunner(IJobDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Enfileirando 10 jobs no MSSQL...");

        for (var i = 0; i < 10; i++)
        {
            var email = $"user{i}@teste.com";
            await _dispatcher.EnqueueAsync<EmailService>(s => s.EnviarAsync(email));
        }

        Console.WriteLine("Jobs enfileirados. Aperte Ctrl+C para sair.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

public class EmailService
{
    public async Task EnviarAsync(string email)
    {
        Console.WriteLine($"Enviando e-mail para {email}...");
        await Task.Delay(200);
        Console.WriteLine($"E-mail enviado para {email}.");
    }
}
