using System;
using System.IO;
using Funq;
using ResultsView.ServiceInterface;
using ResultsView.ServiceModel.Types;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Authentication.OAuth2;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;

namespace ResultsView
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("HTTP Benchmarks Analyzer", typeof(WebServices).Assembly) { }

        public override void Configure(Container container)
        {
            //Load environment config from text file if exists
            var liveSettings = "~/appsettings.txt".MapHostAbsolutePath();
            var isLive = File.Exists(liveSettings);
            var appSettings = isLive
                ? (IAppSettings)new TextFileSettings(liveSettings)
                : new AppSettings();

            SetConfig(new HostConfig {
                DebugMode = !isLive,
                StripApplicationVirtualPath = isLive,
                AdminAuthSecret = appSettings.GetString("AuthSecret"),
            });

            Plugins.Add(new RazorFormat());
            Plugins.Add(new RequestLogsFeature());
            Plugins.Add(new CorsFeature());
            
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
                    IncludeRegistrationService = true
                });
            
            container.Register<IUserAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) {
                    MaxLoginAttempts = appSettings.Get("MaxLoginAttempts", 5)
                });

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