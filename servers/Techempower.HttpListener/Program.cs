using System;
using Funq;
using ServiceStack;
using ServiceStack.Text;
using Techempower.ServiceInterface;

namespace Techempower.HttpListener
{
    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("Techempower Benchmarks", typeof(TechmeServices).Assembly) { }

        public override void Configure(Container container)
        {
            ConfigApp.AppHost(this);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            new AppHost()
                .Init()
                .Start("http://*:55001/");

            "Press Enter to Quit".Print();
            Console.ReadLine();
        }
    }
}
