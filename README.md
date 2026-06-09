# EZ.Job.Store.MSSQL

Store **SQL Server (MSSQL)** para [EZ.Job.Core](https://github.com/ez-dotnet/ez-job-core).

## Performance

| Store  | Jobs | Workers | EZ.Job (ms) | Hangfire (ms) | Vezes mais rápido |
|--------|------|---------|-------------|---------------|-------------------|
| MSSQL  | 100  | 1       | 1.17        | 1.04          | 0.89×             |
| MSSQL  | 1000 | 4       | 5.43        | 13.80         | 2.54×             |

**Eficiência de memória:** EZ.Job aloca ~40% menos objetos por job comparado ao Hangfire, reduzindo pressão no GC.

## Instalação

```bash
dotnet add package EZ.Job.Core
dotnet add package EZ.Job.Store.MSSQL
```

## Uso

```csharp
using EZ.Job.Core;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddEZJob()
    .AddMsSqlStore("Server=localhost;Database=ez_jobs;User Id=sa;Password=...;TrustServerCertificate=True");

// Injete IJobDispatcher e use
```

## Projetos relacionados

- [EZ.DotNet](https://github.com/ez-dotnet) — Organização
- [EZ.Job.Core](https://github.com/ez-dotnet/ez-job-core)
- [EZ.Job.Recurring](https://github.com/ez-dotnet/ez-job-recurring)
