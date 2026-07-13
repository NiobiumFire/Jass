using System.ComponentModel.DataAnnotations;

namespace BelotWebApp.Models
{
    public class BelotRoomCreationOptions
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Room name")]
        [StringLength(25, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string RoomName { get; set; } = "";
        [AllowedValues(501, 1001, 1501)]
        public int ScoreTarget { get; set; } = 1501;
        [Display(Name = "Allow chat")]
        public bool AllowChat { get; set; } = true;
        //public bool AllowSpectators { get; set; }
    }
}
