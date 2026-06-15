using mikroservisnaApp.LokacijaAPI.EventSourcing.Events;

namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Models
{
    public abstract class AggregateRoot
    {
        public int ID { get; set; }
        public int Version { get; set; }

        private readonly List<Event> _unsavedEvents = new();

        public IReadOnlyList<Event> DequeueUnsavedEvents()
        {
            var events = _unsavedEvents.ToList();
            _unsavedEvents.Clear();
            return events;
        }

        protected void RaiseEvent(Event @event)
        {
            Apply(@event);
            Version++;
            _unsavedEvents.Add(@event);
        }

        public void LoadFromHistory(IEnumerable<Event> history)
        {
            foreach (var @event in history)
            {
                Apply(@event);
                Version++;
            }
        }

        protected abstract void Apply(Event @event);
        public abstract AggregateSnapshot CreateSnapshot();
        public abstract void RestoreSnapshot(AggregateSnapshot snapshot);
    }
}