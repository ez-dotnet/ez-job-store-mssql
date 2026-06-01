using EZ.Job.Core;
using EZJob.Store.MSSQL;
using Xunit;

namespace EZ.Job.Store.MSSQL.Tests;

public sealed class MsSqlJobStoreTests
{
    private const string ConnectionString =
        "Server=localhost;Database=ez_jobs_test;User Id=sa;Password=Root@123;TrustServerCertificate=True";

    [Fact(Skip = "Requires MSSQL container")]
    public async Task AddAsync_should_store_job()
    {
        var store = new MsSqlJobStore(ConnectionString);
        var job = new Job("test-id", "T", "M", [], [], JobStatus.Enqueued, DateTime.UtcNow, null);

        await store.AddAsync(job);
        var result = await store.GetAsync("test-id");

        Assert.NotNull(result);
        Assert.Equal("test-id", result!.Id);
    }
}
