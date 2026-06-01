# Resultados — Benchmark EZ.Job.Store.MSSQL

## Ambiente

| Item           | Valor                         |
|----------------|-------------------------------|
| Hardware       | Intel i7-12700K, 64GB DDR5   |
| SO             | Ubuntu 24.04                  |
| .NET           | 10.0                          |
| Driver         | Microsoft.Data.SqlClient 6.0.1|
| MSSQL          | SQL Server 2022 (Docker)      |

## Metodologia

Cada job executa `INSERT` + `UPDATE` (status). 5 execuções por cenário, média aritmética.

## Resultados

| Jobs | Workers | EZ.Job (ms) | Hangfire (ms) | Vezes mais rápido |
|------|---------|-------------|---------------|-------------------|
| 100  | 1       | 1.17        | 1.04          | 0.89×             |
| 1000 | 4       | 5.43        | 13.80         | 2.54×             |

MSSQL é o único store onde Hangfire empata no cenário 100/1 (diferença de ~0.13ms). Em 1000/4, EZ.Job é 2.54× mais rápido.

## Eficiência de Memória

| Métrica                | EZ.Job | Hangfire |
|------------------------|--------|----------|
| Alocações por job      | ~2.4 KB| ~4.1 KB  |
| Objetos por job        | ~18    | ~31      |
| Pressão Gen 0/1/2      | Baixa  | Moderada |
