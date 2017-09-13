using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Actors
{
    public class EventsFeedProcessorActor : Grain, IEventsFeedProcessor
    {
        public async Task Process(EventsFeedChangeSet changeSet)
        {
            foreach (var buffer in changeSet.Updated.Buffer(30))
            {
                await Task.WhenAll(
                    buffer.Select(
                        x => GrainFactory.GetGrain<IEventFeedDataChangeTracker>(x.EventKey).ReceiveInnerFeedEvent(x)));
            }
        }
    }

    public static class Utils
    {
        public static IEnumerable<T[]> Buffer<T>(this IEnumerable<T> source, int bufferSize)
        {
            if (bufferSize < 1)
            {
                throw new ArgumentException("Buffer size should be greater than 0", nameof(bufferSize));
            }

            return Iterator();

            IEnumerable<T[]> Iterator()
            {
                T[] buffer = new T[bufferSize];
                int bufferPos = 0;
                foreach (var element in source)
                {
                    buffer[bufferPos] = element;
                    bufferPos++;
                    if (bufferPos == bufferSize)
                    {
                        yield return buffer;
                        bufferPos = 0;
                        buffer = new T[bufferSize];
                    }
                }
                if (bufferPos == 0) yield break;
                if (bufferPos != bufferSize)
                {
                    var lastBuffer = new T[bufferPos];
                    Array.Copy(buffer, lastBuffer, bufferPos);
                    yield return lastBuffer;
                }
                else
                {
                    yield return buffer;
                }
            }
        }
    }
}