using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using Funq;
using ResultsView.ServiceInterface;
using ResultsView.ServiceModel.Types;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Authentication.OAuth2;
using ServiceStack.Authentication.OpenId;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.Text;

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
                DebugMode = true, //!isLive,
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

    public class GoogleOAuth2Provider : OAuth2Provider
    {
        public const string Name = "GoogleOAuth";

        public const string Realm = "https://accounts.google.com/o/oauth2/auth";

        public GoogleOAuth2Provider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = this.AuthorizeUrl ?? Realm;
            this.AccessTokenUrl = this.AccessTokenUrl ?? "https://accounts.google.com/o/oauth2/token";
            this.UserProfileUrl = this.UserProfileUrl ?? "https://www.googleapis.com/oauth2/v1/userinfo";

            if (this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "https://www.googleapis.com/auth/userinfo.profile",
                    "https://www.googleapis.com/auth/userinfo.email"
                };
            }
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            string json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);
            var authInfo = new Dictionary<string, string>
            {
                { "user_id", obj["id"] }, 
                { "username", obj["email"] }, 
                { "email", obj["email"] }, 
                { "name", obj["name"] }, 
                { "first_name", obj["given_name"] }, 
                { "last_name", obj["family_name"] },
                { "gender", obj["gender"] },
                { "birthday", obj["birthday"] },
                { "link", obj["link"] },
                { "picture", obj["picture"] },
                { "locale", obj["locale"] },
            };
            return authInfo;
        }

        public override DotNetOpenAuth.OAuth2.IAuthorizationState ProcessUserAuthorization(DotNetOpenAuth.OAuth2.WebServerClient authClient, DotNetOpenAuth.OAuth2.AuthorizationServerDescription authServer, IServiceBase authService)
        {
            if (!HostContext.Config.StripApplicationVirtualPath)
                return base.ProcessUserAuthorization(authClient, authServer, authService);

            var req = authService.Request.ToHttpRequestBase();
            var authState = authClient.ProcessUserAuthorization(req);
            return authState;
        }

    }
}