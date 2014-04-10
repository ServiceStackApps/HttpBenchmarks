using System;
using System.Configuration;
using System.IO;
using Funq;
using ResultsView.ServiceInterface;
using ResultsView.ServiceModel.Types;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Authentication.OAuth2;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;

namespace ResultsView
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("HTTP Benchmarks Viewer", typeof(WebServices).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig {
                DebugMode = true
            });

            Plugins.Add(new RazorFormat());

            //Load environment config file if exists
            var hostconfigPath = "~/hostconfig.txt".MapHostAbsolutePath();
            if (new FileInfo(hostconfigPath).Exists)
            {
                var config = File.ReadAllText(hostconfigPath).ParseKeyValueText(delimiter:" ");
                if (config["DbProvider"] == "PostgreSql")
                {
                    container.Register<IDbConnectionFactory>(c =>
                        new OrmLiteConnectionFactory(config["ConnectionString"], PostgreSqlDialect.Provider));
                }
            }
            else
            {
                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory("~/db.sqlite".MapHostAbsolutePath(), SqliteDialect.Provider));
            }

            var appSettings = new AppSettings();
            Plugins.Add(new AuthFeature(() => new UserSession(),
                new IAuthProvider[] {
                    new CredentialsAuthProvider(),
                    new TwitterAuthProvider(appSettings),
                    new FacebookAuthProvider(appSettings),
                    new GoogleOAuth2Provider(appSettings), 
                    new LinkedInOAuth2Provider(appSettings), 
                }) {
                    HtmlRedirect = "/",
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
            Licensing.RegisterLicenseFromFileIfExists(@"~\appsettings.license.txt".MapHostAbsolutePath());
            new AppHost().Init();
        }
    }
}