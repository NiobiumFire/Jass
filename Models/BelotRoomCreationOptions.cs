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

        [Required]
        [AllowedValues(501, 1001, 1501)]
        public int ScoreTarget { get; set; } = 1501;

        [Required]
        [Display(Name = "Allow chat")]
        public bool AllowChat { get; set; } = true;

        [Required]
        [AllowedValues(0, 10, 15, 30, 45, 60, 120)]
        public int TurnTime { get; set; } = 10;

        //public bool AllowSpectators { get; set; }
    }
}
