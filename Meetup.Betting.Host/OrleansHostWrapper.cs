using System;
using System.Collections.Generic;
using System.Net;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;
using OrleansDashboard;

namespace Meetup.Betting.Host
{
    internal class OrleansHostWrapper : IDisposable
    {
        public bool Debug
        {
            get { return _siloHost != null && _siloHost.Debug; }
            set { _siloHost.Debug = value; }
        }

        private SiloHost _siloHost;

        public OrleansHostWrapper(string[] args)
        {
            ParseArguments(args);
            Init();
        }

        public bool Run()
        {
            bool ok = false;

            try
            {
                _siloHost.InitializeOrleansSilo();

                ok = _siloHost.StartOrleansSilo(false);

                if (ok)
                {
                    Console.WriteLine($"Successfully started Orleans silo '{_siloHost.Name}' as a {_siloHost.Type} node.");
                }
                else
                {
                    throw new SystemException(
                        $"Failed to start Orleans silo '{_siloHost.Name}' as a {_siloHost.Type} node.");
                }
            }
            catch (Exception exc)
            {
                _siloHost.ReportStartupError(exc);
                var msg = $"{exc.GetType().FullName}:\n{exc.Message}\n{exc.StackTrace}";
                Console.WriteLine(msg);
            }

            return ok;
        }

        public bool Stop()
        {
            bool ok = false;

            try
            {
                _siloHost.ShutdownOrleansSilo();

                Console.WriteLine($"Orleans silo '{_siloHost.Name}' shutdown.");
            }
            catch (Exception exc)
            {
                _siloHost.ReportStartupError(exc);
                var msg = $"{exc.GetType().FullName}:\n{exc.Message}\n{exc.StackTrace}";
                Console.WriteLine(msg);
            }

            return ok;
        }

        private void Init()
        {
        }

        private bool ParseArguments(string[] args)
        {
            string siloName = Dns.GetHostName(); // Default to machine name

            int siloPort = 9800;
            int proxyGatewayPort = 9880;

            int argPos = 1;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a.StartsWith("-") || a.StartsWith("/"))
                {
                    switch (a.ToLowerInvariant())
                    {
                        case "/?":
                        case "/help":
                        case "-?":
                        case "-help":
                            // Query usage help
                            return false;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + a);
                            return false;
                    }
                }
                else if (a.Contains("="))
                {
                    string[] split = a.Split('=');
                    if (String.IsNullOrEmpty(split[1]))
                    {
                        Console.WriteLine("Bad command line arguments supplied: " + a);
                        return false;
                    }

                    switch (split[0])
                    {
                        case "silo":
                            siloPort = int.Parse(split[1]);
                            break;
                        case "proxy":
                            proxyGatewayPort = int.Parse(split[1]);
                            break;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + a);
                            return false;
                    }
                }
                // unqualified arguments below
                else if (argPos == 1)
                {
                    siloName = a;
                    argPos++;
                }
                else
                {
                    // Too many command line arguments
                    Console.WriteLine("Too many command line arguments supplied: " + a);
                    return false;
                }
            }

            var config = new ClusterConfiguration();

            config.StandardLoad();

            config.Defaults.Port = siloPort;
            config.Defaults.ProxyGatewayEndpoint = new IPEndPoint(config.Defaults.ProxyGatewayEndpoint.Address,
                proxyGatewayPort);

            config.UseStartupType<Startup>();
            config.Globals.DeploymentId = "BetLab.Meetup";
            config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
            config.Globals.MembershipTableAssembly = "OrleansConsulUtils";
            config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Disabled;
            
            //config.Globals.RegisterBootstrapProvider<Dashboard>("Dashboard", new Dictionary<string, string>()
            //{
            //    ["Port"] = "8080"
            //});

            config.AddCustomStorageInterfaceBasedLogConsistencyProvider("CustomStorage");

            _siloHost = new SiloHost(Dns.GetHostName(), config);

            return true;
        }

        public void PrintUsage()
        {
            Console.WriteLine(
@"USAGE: 
    OrleansHost.exe [<siloName> [<configFile>]] [DeploymentId=<idString>] [/debug]
Where:
    <siloName>      - Name of this silo in the Config file list (optional)
    DeploymentId=<idString> 
                    - Which deployment group this host instance should run in (optional)
    /debug          - Turn on extra debug output during host startup (optional)");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool dispose)
        {
            _siloHost.Dispose();
            _siloHost = null;
        }
    }
}
