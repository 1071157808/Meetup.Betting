using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Actors
{
    public class EventFeedDataChangeTrackerActor : Grain, IEventFeedDataChangeTracker
    {
        private EventData _currentSnapshot;

        public override async Task OnActivateAsync()
        {
            var blek = this.GetPrimaryKeyString();
            var eventActor = GrainFactory.GetGrain<IEvent>(blek);
            var snapshot = await eventActor.GetSnapshot();
            _currentSnapshot = snapshot;
            await base.OnActivateAsync();
        }

        public async Task ReceiveInnerFeedEvent(EventData eventData)
        {
            try
            {
                var eventActor = GrainFactory.GetGrain<IEvent>(eventData.EventKey);
                if (_currentSnapshot == null)
                {
                    await eventActor.SetEventInformation(new EventInformation
                    {
                        Name = eventData.Name,
                        StartDate = eventData.StartDate,
                        TournamentId = eventData.TournamentKey,
                        TeamsIds = eventData.TeamsKeys,
                    });
                }

                if (eventData.Scoreboard != null)
                {
                    if (_currentSnapshot == null || ! _currentSnapshot.Scoreboard.Equals(eventData.Scoreboard))
                    {
                        await eventActor.SetScoreboard(new ScoreboardData
                        {
                            CurrentTime = eventData.Scoreboard.CurrentTime,
                            CurrentScore = eventData.Scoreboard.CurrentScore,
                            ScoresByPeriod = eventData.Scoreboard.ScoresByPeriod
                        });
                    }
                }

                var marketTasks = new List<Task>();
                if (_currentSnapshot != null)
                {
                    foreach (var market in eventData.Markets.Values)
                    {
                        MarketData currentMarket;
                        if (!_currentSnapshot.Markets.TryGetValue(market.Key, out currentMarket) || !currentMarket.Equals(market))
                        {
                            marketTasks.Add(
                                GrainFactory.GetGrain<IMarket>($"{eventData.EventKey}|{market.Key}")
                                    .ChangeOdds(market.Odds.Values.ToArray()));
                        }
                        _currentSnapshot.Markets.Remove(market.Key);
                    }
                    foreach (var currentMarket in _currentSnapshot.Markets)
                    {
                        marketTasks.Add(
                            GrainFactory.GetGrain<IMarket>($"{eventData.EventKey}|{currentMarket.Key}")
                                .Deactivate());
                    }
                }
                else
                {
                    foreach (var market in eventData.Markets.Values)
                    {
                        marketTasks.Add(
                            GrainFactory.GetGrain<IMarket>($"{eventData.EventKey}|{market.Key}")
                                .ChangeOdds(market.Odds.Values.ToArray()));
                    }
                }
                await Task.WhenAll(marketTasks);

                _currentSnapshot = eventData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}