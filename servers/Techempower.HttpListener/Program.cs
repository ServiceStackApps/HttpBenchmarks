﻿using System;
using Funq;
using ServiceStack;
using ServiceStack.Text;
using Techempower.ServiceInterface;

namespace Techempower.HttpListener
{
    public class AppHost : AppHostHttpListenerBase
    {
        private readonly DbProvider db;

        public AppHost(DbProvider db)
            : base("HttpListener Techempower Benchmarks", typeof(TechmeServices).Assembly)
        {
            this.db = db;
        }

        public override void Configure(Container container)
        {
            ConfigApp.AppHost(this, db);
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
                .Start("http://*:55001/");

            "\nHttpListener started with ThreadPool size of {0} using {1} listening on tcp port 55001"
                .Print(1, db);
            "Press Enter to Quit".Print();
            Console.ReadLine();
        }
    }
}
