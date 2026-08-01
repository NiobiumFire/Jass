using System.ComponentModel.DataAnnotations;

namespace BelotWebApp.Notification
{
    public class ServerNotification
    {
        public bool Enabled { get; set; }
        
        public bool IsMaintenance { get; set; }

        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        [Display(Name = "Scheduled for (UTC)")]
        public DateTime ScheduledUtc { get; set; }

        public string Message { get; set; } = "Maintenance started";

        public ServerNotification Clone() => new()
        {
            Enabled = Enabled,
            IsMaintenance = IsMaintenance,
            ScheduledUtc = ScheduledUtc,
            Message = Message
        };
    }
}
