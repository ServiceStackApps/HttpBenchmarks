using System;
using System.IO;
using BenchmarksAnalyzer.ServiceInterface;
using BenchmarksAnalyzer.ServiceModel.Types;
using Funq;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Auth;
using ServiceStack.Authentication.OAuth2;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;

namespace BenchmarksAnalyzer
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("HTTP Benchmarks Analyzer", typeof(WebServices).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());
            Plugins.Add(new RequestLogsFeature());
            Plugins.Add(new PostmanFeature());
            Plugins.Add(new OpenApiFeature());

            Plugins.Add(new CorsFeature(
                allowOriginWhitelist: new[] { "http://localhost", "http://localhost:8080", "http://test.servicestack.net", "http://null.jsbin.com" },
                allowCredentials: true,
                allowedHeaders: "Content-Type, Allow, Authorization"));

            //Load environment config from text file if exists
            var liveSettings = "~/appsettings.txt".MapHostAbsolutePath();
            var appSettings = File.Exists(liveSettings)
                ? (IAppSettings)new TextFileSettings(liveSettings)
                : new AppSettings();

            SetConfig(new HostConfig {
                DebugMode = appSettings.Get("DebugMode", false),
                StripApplicationVirtualPath = appSettings.Get("StripApplicationVirtualPath", false),
                AdminAuthSecret = appSettings.GetString("AuthSecret"),
            });
            
            if (appSettings.GetString("DbProvider") == "PostgreSql")
            {
                container.Register<IDbConnectionFactory>(c => new OrmLiteConnectionFactory(
                    appSettings.GetString("ConnectionString"), PostgreSqlDialect.Provider));
            }
            else
            {
                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory("~/db.sqlite".MapHostAbsolutePath(), SqliteDialect.Provider));
            }

            container.RegisterAs<OrmLiteCacheClient, ICacheClient>();
            container.Resolve<ICacheClient>().InitSchema();

            Plugins.Add(new AuthFeature(() => new UserSession(),
                new IAuthProvider[] {
                    new CredentialsAuthProvider(),
                    new TwitterAuthProvider(appSettings),
                    new FacebookAuthProvider(appSettings),
                    new GoogleOAuth2Provider(appSettings), 
                    new LinkedInOAuth2Provider(appSettings), 
                }) {
                HtmlRedirect = "~/",
                IncludeRegistrationService = true,
                MaxLoginAttempts = appSettings.Get("MaxLoginAttempts", 5),
            });
            
            container.Register<IUserAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

            container.Resolve<IUserAuthRepository>().InitSchema();

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<TestPlan>();
                db.CreateTableIfNotExists<TestRun>();
                db.CreateTableIfNotExists<TestResult>();                
            }
        }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();

            Licensing.RegisterLicenseFromFileIfExists(@"~/appsettings.license.txt".MapHostAbsolutePath());
            new AppHost().Init();
        }
    }
}