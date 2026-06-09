using EZ.Job.Core;
using EZJob.Store.MSSQL;

namespace Microsoft.Extensions.DependencyInjection;

public static class EZJobMsSqlExtensions
{
    public static IEZJobBuilder AddMsSqlStore(this IEZJobBuilder builder, string connectionString)
    {
        return AddMsSqlStore(builder, o => o.ConnectionString = connectionString);
    }

    public static IEZJobBuilder AddMsSqlStore(this IEZJobBuilder builder, Action<MsSqlStoreOptions> configure)
    {
        var options = new MsSqlStoreOptions();
        configure(options);

        builder.Services.AddSingleton<IJobStore>(_ => new MsSqlJobStore(options.ConnectionString));
        builder.Services.AddSingleton<IRecurringStore>(_ => new MsSqlRecurringStore(options.ConnectionString));

        return builder;
    }
}
