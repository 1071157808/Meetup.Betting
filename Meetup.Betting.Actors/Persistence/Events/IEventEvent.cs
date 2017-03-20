using System;

namespace Meetup.Betting.Actors.Persistence.Events
{
    public interface IEventEvent
    {
    }

    [Serializable]
    public class MarketRegistered : IEventEvent
    {
        public string MarketKey { get; set; }
    }

    [Serializable]
    public class ScoreboardChanged : IEventEvent
    {
        public TimeSpan? CurrentTime { get; set; }
        public string[] CurrentScore { get; set; }
        public string[] ScoresByPeriod { get; set; }
    }

    [Serializable]
    public class EventInformationChanged : IEventEvent
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public string TournamentId { get; set; }
        public string[] TeamsIds { get; set; }
    }
}