using System;

namespace Meetup.Betting.Actors.Persistence
{
    public class ScoreboardState
    {
        public TimeSpan? CurrentTime { get; set; }
        public string[] CurrentScore { get; set; }
        public string[] ScoresByPeriod { get; set; }    
    }
}