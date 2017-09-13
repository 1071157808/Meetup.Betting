using System;
using System.Net;
using Marten;
using Marten.Events;
using Meetup.Betting.Actors.Persistence;
using Meetup.Betting.Actors.Persistence.Events;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;
using OrleansDashboard;

namespace Meetup.Betting.Host
{
    /// <summary>
    /// Orleans test silo host
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            // The Orleans silo environment is initialized in its own app domain in order to more
            // closely emulate the distributed situation, when the client and the server cannot
            // pass data via shared memory.
            AppDomain hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
            {
                AppDomainInitializer = InitSilo,
                AppDomainInitializerArguments = args,
            });

            Console.WriteLine("Orleans Silo is running.\nPress Enter to terminate...");
            Console.ReadLine();

            hostDomain.DoCallBack(ShutdownSilo);
        }

        static void InitSilo(string[] args)
        {
            hostWrapper = new OrleansHostWrapper(args);

            if (!hostWrapper.Run())
            {
                Console.Error.WriteLine("Failed to initialize Orleans silo");
            }
        }

        static void ShutdownSilo()
        {
            if (hostWrapper != null)
            {
                hostWrapper.Dispose();
                GC.SuppressFinalize(hostWrapper);
            }
        }

        private static OrleansHostWrapper hostWrapper;
    }
    public class Startup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var documentStore = DocumentStore.For(_ =>
            {
                _.DatabaseSchemaName = StoreOptions.DefaultDatabaseSchemaName;
                _.AutoCreateSchemaObjects = AutoCreate.All;

                _.Connection("host=localhost;database=meetup;password=dev_dbo;username=dev_dbo;MaxPoolSize=500");
                
                // _.Events.InlineProjections.AggregateStreamsWith<EventState>();

                _.Events.AddEventType(typeof(MarketRegistered));
                _.Events.AddEventType(typeof(ScoreboardChanged));
                _.Events.AddEventType(typeof(EventInformationChanged));

                //_.Events.AddEventType(typeof(MonsterSlayed));
                //_.Events.AddEventType(typeof(MembersJoined));
            });

            services.AddSingleton<IDocumentStore>(documentStore);
            services.AddTransient<IEventStorage, MartenEventStorage>();

            return services.BuildServiceProvider();
        }
    }
}