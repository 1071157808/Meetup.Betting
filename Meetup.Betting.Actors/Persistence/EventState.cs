using System;
using System.Collections.Generic;
using Meetup.Betting.Contracts;
using EventInformationChanged = Meetup.Betting.Actors.Persistence.Events.EventInformationChanged;
using MarketRegistered = Meetup.Betting.Actors.Persistence.Events.MarketRegistered;
using ScoreboardChanged = Meetup.Betting.Actors.Persistence.Events.ScoreboardChanged;

namespace Meetup.Betting.Actors.Persistence
{
    [Serializable]
    public class EventState
    {
        public EventState()
        {
            Scoreboard = new ScoreboardState();
            Markets = new HashSet<string>();
        }

        public string Name { get; set; }    
        public DateTime StartDate { get; set; }
        public string TournamentId { get; set; }
        public string[] TeamsIds { get; set; }
        public ScoreboardState Scoreboard { get; set; }
        public HashSet<string> Markets { get; set; }

        public void Apply(MarketRegistered @event)
        {
            Markets.Add(@event.MarketKey);
        }

        public void Apply(ScoreboardChanged @event)
        {
            Scoreboard.CurrentTime = @event.CurrentTime;
            Scoreboard.CurrentScore = @event.CurrentScore;
            Scoreboard.ScoresByPeriod = @event.ScoresByPeriod;
        }

        public void Apply(EventInformationChanged @event)
        {
            Name = @event.Name;
            StartDate = @event.StartDate;
            TournamentId = @event.TournamentId;
            TeamsIds = @event.TeamsIds;
        }
    }
}