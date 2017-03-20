using System;
using System.Threading;
using Orleans;
using Orleans.Runtime.Configuration;

namespace Meetup.Betting.Client
{
    public class OrleansClientWrapper
    {
        public IDisposable Run()
        {
            var config = ClientConfiguration.StandardLoad();
            config.DeploymentId = "BetLab.Meetup";
            InitializeWithRetries(config, 10);

            return new AnonymousDisposable(GrainClient.Uninitialize);
        }

        private static void InitializeWithRetries(ClientConfiguration config, int initializeAttemptsBeforeFailing)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    GrainClient.Initialize(config);
                    Console.WriteLine("Client successfully connect to silo host");
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine(
                        $"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}