using System.ComponentModel.DataAnnotations;
using InternFreelance.Models;

namespace InternFreelance.ViewModels
{
    public class RegisterVm
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public RoleType Role { get; set; } = RoleType.Student; // default student
    }
}
