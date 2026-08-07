using System.ComponentModel.DataAnnotations;

namespace SpecimenCheckIn.Api.Models
{
    public class ReceiveSpecimenRequest
    {
        [Required(ErrorMessage = "ReceivedBy is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "ReceivedBy name must be between 2 and 100 characters.")]
        public string ReceivedBy { get; set; } = string.Empty;
    }
}
