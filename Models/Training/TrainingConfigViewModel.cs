using System.ComponentModel.DataAnnotations;

namespace BelotWebApp.Models.Training
{
    public class TrainingConfigViewModel
    {
        [Required]
        [Range(1, 10000)]
        public int PopulationSize { get; set; } = 100;

        [Required]
        [Range(1, 100)]
        public int NumGenerations { get; set; } = 2;
    }

}
