using System;
using Funq;
using ServiceStack;
using Techempower.ServiceInterface;

namespace Techempower.AspNet
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("Techempower Benchmarks", typeof(TechmeServices).Assembly) { }

        public override void Configure(Container container)
        {
            ConfigApp.AppHost(this);
        }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }
    }
}