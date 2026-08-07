using System.ComponentModel.DataAnnotations;

namespace SpecimenCheckIn.Api.Models
{
    public class FlagSpecimenRequest
    {
        [Required(ErrorMessage = "ReceivedBy operator identity is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "ReceivedBy operator identity must be between 2 and 100 characters.")]
        public string ReceivedBy { get; set; } = string.Empty;

        [Required(ErrorMessage = "Notes explaining the flag discrepancy are required.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Discrepancy notes must be between 5 and 500 characters.")]
        public string Notes { get; set; } = string.Empty;
    }
}
