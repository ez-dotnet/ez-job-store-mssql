using EZ.Job.Core;
using EZJob.Store.MSSQL;

namespace Microsoft.Extensions.DependencyInjection;

public static class EZJobMsSqlExtensions
{
    public static EZJobBuilder AddMsSqlStore(this EZJobBuilder builder, string connectionString)
    {
        return AddMsSqlStore(builder, o => o.ConnectionString = connectionString);
    }

    public static EZJobBuilder AddMsSqlStore(this EZJobBuilder builder, Action<MsSqlStoreOptions> configure)
    {
        var options = new MsSqlStoreOptions();
        configure(options);

        builder.Services.AddSingleton<IJobStore>(_ => new MsSqlJobStore(options.ConnectionString));

        return builder;
    }
}
