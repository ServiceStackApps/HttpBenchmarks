using System;
using System.Threading;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.Redis;

namespace Techempower.ServiceInterface
{
    public enum DbProvider
    {
        MySql,
        Sqlite,
        SqlServer,
        PostgreSql,
        InMemory,
    }

    public static class ConfigApp
    {
        public static void AppHost(ServiceStackHost appHost, DbProvider defaultDb = DbProvider.SqlServer)
        {
            appHost.Plugins.Add(new RazorFormat());

            appHost.Container.Register<IDbConnectionFactory>(CreateDbFactory(defaultDb));
            //appHost.Container.Register<IRedisClientsManager>(c => new PooledRedisClientManager());
        }

        public static OrmLiteConnectionFactory CreateDbFactory(DbProvider defaultDb)
        {
            var appSettings = new AppSettings();
            var dbProvider = appSettings.Get("connection", defaultValue: defaultDb);
            switch (dbProvider)
            {
                case DbProvider.InMemory:
                    return new OrmLiteConnectionFactory(":memory:",
                        SqliteDialect.Provider);

                case DbProvider.SqlServer:
                    return new OrmLiteConnectionFactory(
                        appSettings.Get("connection.sqlserver", "Server=localhost;Database=test;User Id=test;Password=test;"),
                        SqlServerDialect.Provider);

                case DbProvider.PostgreSql:
                    return new OrmLiteConnectionFactory(
                        appSettings.Get("connection.postgresql", "Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200"),
                        PostgreSqlDialect.Provider);

                case DbProvider.MySql:
                    return new OrmLiteConnectionFactory(
                        appSettings.Get("connection.mysql", "Server=localhost;Database=test;UID=root;Password=test"),
                        MySqlDialect.Provider);

                case DbProvider.Sqlite:
                    return new OrmLiteConnectionFactory(
                        appSettings.Get("connection.sqlite", "db.sqlite"),
                        SqliteDialect.Provider);

                default:
                    throw new NotImplementedException(dbProvider.ToString());
            }
        }

        public static void MinWorkerThreads(int defaultValue=8)
        {
            // To improve CPU utilization, increase the number of threads that the .NET thread pool expands by when
            // a burst of requests come in. We could do this by editing machine.config/system.web/processModel/minWorkerThreads,
            // but that seems too global a change, so we do it in code for just our AppPool. More info:
            //
            // http://support.microsoft.com/kb/821268
            // http://blogs.msdn.com/b/tmarq/archive/2007/07/21/asp-net-thread-usage-on-iis-7-0-and-6-0.aspx
            // http://blogs.msdn.com/b/perfworld/archive/2010/01/13/how-can-i-improve-the-performance-of-asp-net-by-adjusting-the-clr-thread-throttling-properties.aspx

            var appSettings = new AppSettings();
            int newMinWorkerThreads = appSettings.Get("minWorkerThreadsPerLogicalProcessor", defaultValue);
            if (newMinWorkerThreads > 0)
            {
                int minWorkerThreads, minCompletionPortThreads;
                ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
                ThreadPool.SetMinThreads(Environment.ProcessorCount * newMinWorkerThreads, minCompletionPortThreads);
            }
        }
    }
}