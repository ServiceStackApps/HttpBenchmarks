using System;
using Funq;
using ServiceStack;
using ServiceStack.Text;
using Techempower.ServiceInterface;

namespace Techempower.SelfHost
{
    public class AppHost : AppSelfHostBase
    {
        private readonly DbProvider db;

        public AppHost(DbProvider db)
            : base("SelfHost Techempower Benchmarks", typeof(TechmeServices).Assembly)
        {
            this.db = db;
        }

        public override void Configure(Container container)
        {
            ConfigApp.AppHost(this, db);

            this.PreRequestFilters.Add((req, res) =>
            {
                res.UseBufferedStream = true;
            });
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            DbProvider db;
            if (!(args.Length > 0 && Enum.TryParse(args[0], true, out db)))
                db = DbProvider.InMemory;

            new AppHost(db)
                .Init()
                .Start("http://*:55004/");

            "\nSelfAppHost started using {0} listening on tcp port 55004"
                .Print(db);
            "Press Enter to Quit".Print();
            Console.ReadLine();
        }
    }
}
