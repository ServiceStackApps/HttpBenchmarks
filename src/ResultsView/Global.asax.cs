using System;
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

            var appSettings = new AppSettings();

            Plugins.Add(new RazorFormat());

            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory("db.sqlite", SqliteDialect.Provider));
            //container.Register<IDbConnectionFactory>(c =>
            //    new OrmLiteConnectionFactory(
            //        Environment.GetEnvironmentVariable("PGSQL_TEST"), 
            //        PostgreSqlDialect.Provider));

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
            Licensing.RegisterLicenseFromFileIfExists(@"c:\src\appsettings.license.txt");
            new AppHost().Init();
        }
    }
}