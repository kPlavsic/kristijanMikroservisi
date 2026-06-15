namespace mikroservisnaApp.LokacijaAPI.Data
{
    public class StoredSnapshot
    {
        public int Id { get; set; }
        public int AggregateId { get; set; }
        public string SnapshotData { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}