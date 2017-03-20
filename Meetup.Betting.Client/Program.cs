using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;
using Orleans.Runtime;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace Meetup.Betting.Client
{
    class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient("357143285:AAEIzDgUL-uabNMuBcCJjIV2LGK2mhU96Wk");
        
        private static readonly IEventsFeedStream EventsFeedStream = new EmptyEventsFeedStream();

        static void Main(string[] args)
        {
            using (new OrleansClientWrapper().Run())
            {
                using (EventsFeedStream)
                {
                    using (EventsFeedStream.Subscribe(new EventsFeedObserver()))
                    {
                        using (EventsFeedStream.Connect())
                        {
                            Bot.OnMessage += BotOnMessageReceived;

                            Bot.StartReceiving();

                            Console.WriteLine("Orleans Client is running.\nPress Enter to terminate...");
                            Console.ReadLine();

                            Bot.StopReceiving();
                        }
                    }
                }
            }
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;
                if (message == null || message.Type != MessageType.TextMessage)
                {
                    // todo: send valid message
                    return;
                }

                var playerId = message.Chat.Id;
                if (message.Text.StartsWith("/start"))
                {
                    var response =
                        "Надішліть /bet щоб зробити випадкову ставку, /history щоб переглянути історію ставок, /rating щоб переглянути рейтинг подій";
                    await Bot.SendTextMessageAsync(playerId, response);
                }
                else if (message.Text.StartsWith("/bet"))
                {
                    await PlaceBetCommand(playerId);
                }
                else if (message.Text.StartsWith("/history"))
                {
                    await BetHistoryCommand(playerId);
                }
                else if (message.Text.StartsWith("/rating"))
                {
                    await RatingCommand(playerId);
                }
                else
                {
                    await Bot.SendTextMessageAsync(playerId, "Invalid command");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async Task PlaceBetCommand(long playerId)
        {
            var playerActor = GrainClient.GrainFactory.GetGrain<IPlayer>(playerId);
            var randomBet = GetRandomBet();
            if (randomBet != null)
            {
                var betId = Guid.NewGuid();
                try
                {
                    var bet = new Bet
                    {
                        Id = betId,
                        EventKey = randomBet.Item1,
                        MarketKey = randomBet.Item2,
                        SelectionKey = randomBet.Item3,
                        Amount = randomBet.Item4,
                        EventName = randomBet.Item5
                    };
                    await playerActor.PlaceBet(bet);
                    await Bot.SendTextMessageAsync(playerId,
                        $"Bet({betId}) of amount {bet.Amount} was placed on {bet.EventName}");
                }
                catch (Exception e)
                {
                    await Bot.SendTextMessageAsync(playerId, $"Bet {betId} failed. {e.Message}");
                }
            }
        }

        private static async Task BetHistoryCommand(long playerId)
        {
            try
            {
                var playerActor = GrainClient.GrainFactory.GetGrain<IPlayer>(playerId);
                var bets = await playerActor.GetBetHistory();
                await Bot.SendTextMessageAsync(playerId,
                    bets.Select((x, i) => new {Value = x, Index = i})
                        .Aggregate(new StringBuilder(),
                            (ac, x) =>
                                ac.AppendLine(
                                    $"{x.Index + 1}. {x.Value.EventName} - {x.Value.Amount}"))
                        .ToString());
            }
            catch (Exception e)
            {
                await Bot.SendTextMessageAsync(playerId, $"Bet history failed. {e.Message}");
            }
        }

        private static async Task RatingCommand(long playerId)
        {
            try
            {
                var eventsAggregator = GrainClient.GrainFactory.GetGrain<IEventAggregator>(0);
                var events = await eventsAggregator.GetEventsStatistics();

                await Bot.SendTextMessageAsync(playerId,
                    events.Select((x, i) => new {Value = x, Index = i})
                        .Aggregate(new StringBuilder(),
                            (ac, x) =>
                                ac.AppendLine(
                                    $"{x.Index + 1}. {x.Value.EventName} - Bets: {x.Value.BetsCount}, TotalAmount: {x.Value.TotalAmount}"))
                        .ToString());
            }
            catch (Exception e)
            {
                await Bot.SendTextMessageAsync(playerId, $"Events rating failed. {e.Message}");
            }
        }

        private static Tuple<string, string, string, int, string> GetRandomBet()
        {
            var events = EventsFeedStream.GetEvents().Where(x => x.Markets.Count > 0).ToArray();
            if (events.Length == 0)
            {
                return null;
            }

            var rnd = new Random();
            var @event = events[rnd.Next(0, events.Length - 1)];
            var market = @event.Markets.Values.ToArray()[rnd.Next(0, @event.Markets.Count - 1)];
            if (market == null)
            {
                return null;
            }
            var selection = market.Odds.Values.ToArray()[rnd.Next(0, market.Odds.Count - 1)];

            return Tuple.Create(@event.EventKey, market.Key, selection.SelectionKey, rnd.Next(1, 10),
                @event.Name);
        }
    }

    /// <summary>Represents an Action-based disposable.</summary>
    internal sealed class AnonymousDisposable : IDisposable
    {
        private volatile Action _dispose;

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// </summary>
        public bool IsDisposed => _dispose == null;

        /// <summary>
        /// Constructs a new disposable with the given action used for disposal.
        /// </summary>
        /// <param name="dispose">Disposal action which will be run upon calling Dispose.</param>
        public AnonymousDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        /// <summary>
        /// Calls the disposal action if and only if the current instance hasn't been disposed yet.
        /// </summary>
        public void Dispose()
        {
            Action action = Interlocked.Exchange(ref _dispose, null);
            action?.Invoke();
        }
    }
}
