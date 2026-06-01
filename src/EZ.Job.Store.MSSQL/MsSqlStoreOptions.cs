namespace EZJob.Store.MSSQL;

public class MsSqlStoreOptions
{
    public string ConnectionString { get; set; } = "Server=localhost;Database=ez_jobs;User Id=sa;Password=Root@123;TrustServerCertificate=True";
}
