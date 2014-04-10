using System;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Redis;

namespace Techempower.ServiceInterface
{
    [Route("/json")]
    public class Hello
    {
        public string name { get; set; }
    }

    public class HelloResponse
    {
        public string message { get; set; }
    }

    [Route("/db")]
    public class Db { }

    [Route("/redis")]
    public class Redis { }

    [Route("/queries")]
    public class Queries
    {
        public int queries { get; set; }
    }

    [Route("/fortunes")]
    public class Fortunes { }

    [Route("/updates")]
    public class Updates
    {
        public int queries { get; set; }
    }

    [Route("/plaintext")]
    public class PlainText { }

    public class World
    {
        public int id { get; set; }
        public int randomNumber { get; set; }
    }

    public class Fortune : IComparable<Fortune>
    {
        [AutoIncrement]
        public int id { get; set; }
        public string message { get; set; }

        public int CompareTo(Fortune other)
        {
            return message.CompareTo(other.message);
        }
    }

    public class TechmeServices : Service
    {
        readonly Random rand = new Random();

        public object Any(Hello request)
        {
            return new HelloResponse { message = "Hello, " + (request.name ?? "World") + "!" };
        }

        public object Any(Db request)
        {
            return Db.SingleById<World>(rand.Next(0, 10000) + 1);
        }

        public object Any(Redis request)
        {
            return Redis.GetById<World>(rand.Next(0, 10000) + 1);
        }

        public object Any(Queries request)
        {
            var queries = Math.Max(1, Math.Min(500, request.queries));
            var worlds = new World[queries];

            for (int i = 0; i < queries; i++)
            {
                worlds[i] = Db.SingleById<World>(rand.Next(0, 10000) + 1);
            }
            return worlds;
        }

        public List<Fortune> Any(Fortunes request)
        {
            var fortunes = new List<Fortune>();

            fortunes.AddRange(Db.Select<Fortune>());
            fortunes.Add(new Fortune { id = 0, message = "Additional fortune added at request time." });
            fortunes.Sort();

            return fortunes;
        }

        public void Any(Updates request)
        {
            var queries = Math.Max(1, Math.Min(500, request.queries));
            var worlds = new World[queries];

            for (int i = 0; i < queries; i++)
            {
                worlds[i] = Db.SingleById<World>(rand.Next(0, 10000) + 1);
                worlds[i].randomNumber = rand.Next(0, 10000) + 1;
            }

            Db.UpdateAll(worlds);
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public string Any(PlainText request)
        {
            return "Hello, World";
        }

        public void Any(Reset request)
        {
            Db.DropAndCreateTable<World>();
            Redis.FlushAll();

            var worlds = 10000.Times(i => new World { id = i, randomNumber = rand.Next(0, 10000) + 1 });
            Db.InsertAll(worlds);

            if (TryResolve<IRedisClientsManager>() != null)
            {
                Redis.StoreAll(worlds);
            }

            Db.DropAndCreateTable<Fortune>();
            new[] {
                "A budget is just a method of worrying before you spend money, as well as afterward.",
                "A classic is something that everybody wants to have read and nobody wants to read.",
                "A conclusion is simply the place where someone got tired of thinking.",
                "A diplomat is someone who can tell you to go to hell in such a way that you will look forward to the trip.",
                "A psychiatrist is a person who will give you expensive answers that your wife will give you for free.",
            }.Each(x => Db.Insert(new Fortune { message = x }));
        }
    }

    [Route("/reset")]
    public class Reset { }
}