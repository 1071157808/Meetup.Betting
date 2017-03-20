namespace Meetup.Betting.Contracts.Messages
{
    public class EventsFeedChangeSet
    {
        public int Revision { get; set; }
        public string[] Removed { get; set; }
        public EventData[] Updated { get; set; }
    }
}