using System;

namespace Meetup.Betting.Contracts
{
    [Serializable]
    public class EventInformation
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public string TournamentId { get; set; }
        public string[] TeamsIds { get; set; }
    }
}