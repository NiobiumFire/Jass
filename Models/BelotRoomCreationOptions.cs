using System.ComponentModel.DataAnnotations;

namespace BelotWebApp.Models
{
    public class BelotRoomCreationOptions
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Room name")]
        [StringLength(22, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string RoomName { get; set; } = "";

        [Required]
        [AllowedValues(501, 1001, 1501)]
        public int ScoreTarget { get; set; } = 1501;

        [Required]
        [Display(Name = "Allow chat")]
        public bool AllowChat { get; set; } = true;

        [Required]
        [AllowedValues(0, 10, 30, 60, 120)]
        public int TurnTime { get; set; } = 30;

        [Required]
        public Data.MatchType MatchType { get; set; }

        //public bool AllowSpectators { get; set; }
    }
}
