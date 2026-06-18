namespace mikroservisnaApp.Shared.Events
{
    public class SendEmailEvent
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}