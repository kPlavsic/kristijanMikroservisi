using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.LokacijaAPI.Data;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Events;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Models;
using System.Text.Json;

namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Store
{
    public class EventStore
    {
        private readonly LokacijaDbContext _context;

        public EventStore(LokacijaDbContext context)
        {
            _context = context;
        }

        public async Task SaveEventsAsync(int aggregateId, IReadOnlyList<Event> events)
        {
            foreach (var @event in events)
            {
                var storedEvent = new StoredEvent
                {
                    AggregateId = aggregateId,
                    EventType = @event.EventType,
                    EventData = JsonSerializer.Serialize(@event, @event.GetType()),
                    CreatedAt = @event.CreatedAt,
                    Version = 0
                };

                await _context.Events.AddAsync(storedEvent);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<Event>> LoadEventsAsync(int aggregateId)
        {
            var storedEvents = await _context.Events
                .Where(e => e.AggregateId == aggregateId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            return storedEvents.Select(Deserialize).ToList();
        }

        public async Task<List<Event>> LoadEventsAfterVersionAsync(int aggregateId, int version)
        {
            var storedEvents = await _context.Events
                .Where(e => e.AggregateId == aggregateId)
                .OrderBy(e => e.CreatedAt)
                .Skip(version)
                .ToListAsync();

            return storedEvents.Select(Deserialize).ToList();
        }

        public async Task SaveSnapshotAsync(int aggregateId, LokacijaSnapshot snapshot)
        {
            var existing = await _context.Snapshots
                .FirstOrDefaultAsync(s => s.AggregateId == aggregateId);

            if (existing != null)
            {
                existing.SnapshotData = JsonSerializer.Serialize(snapshot);
                existing.Version = snapshot.Version;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                await _context.Snapshots.AddAsync(new StoredSnapshot
                {
                    AggregateId = aggregateId,
                    SnapshotData = JsonSerializer.Serialize(snapshot),
                    Version = snapshot.Version,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<LokacijaSnapshot?> LoadSnapshotAsync(int aggregateId)
        {
            var stored = await _context.Snapshots
                .FirstOrDefaultAsync(s => s.AggregateId == aggregateId);

            if (stored == null)
                return null;

            return JsonSerializer.Deserialize<LokacijaSnapshot>(stored.SnapshotData);
        }

        public async Task<LokacijaAggregate?> LoadAggregateAsync(int aggregateId)
        {
            var snapshot = await LoadSnapshotAsync(aggregateId);
            var aggregate = new LokacijaAggregate();

            if (snapshot != null)
            {
                aggregate.RestoreSnapshot(snapshot);
                var eventsAfter = await LoadEventsAfterVersionAsync(aggregateId, snapshot.Version);
                aggregate.LoadFromHistory(eventsAfter);
            }
            else
            {
                var allEvents = await LoadEventsAsync(aggregateId);
                if (allEvents.Count == 0)
                    return null;
                aggregate.LoadFromHistory(allEvents);
            }

            return aggregate;
        }

        public async Task<List<StoredEvent>> LoadHistoryAsync(int aggregateId)
        {
            return await _context.Events
                .Where(e => e.AggregateId == aggregateId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();
        }

        private Event Deserialize(StoredEvent stored)
        {
            var eventType = stored.EventType switch
            {
                nameof(LokacijaKreiranaEvent) => typeof(LokacijaKreiranaEvent),
                nameof(NazivPromenjenEvent) => typeof(NazivPromenjenEvent),
                nameof(AdresaPromenjenEvent) => typeof(AdresaPromenjenEvent),
                nameof(KapacitetPromenjenEvent) => typeof(KapacitetPromenjenEvent),
                nameof(LokacijaObrisanaEvent) => typeof(LokacijaObrisanaEvent),
                _ => throw new InvalidOperationException($"Nepoznat tip dogadjaja: {stored.EventType}")
            };

            return (Event)JsonSerializer.Deserialize(stored.EventData, eventType)!;
        }
    }
}