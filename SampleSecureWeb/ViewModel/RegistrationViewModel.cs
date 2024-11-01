using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SampleSecureWeb.ViewModel
{
    public class RegistrationViewModel : IValidatableObject
    {
        [Required]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters long.")]
        
        public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The Password and Confirmation Password do not match.")]
        public string? ConfirmPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
                if (!string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(Username))
                {
                if (Password.Equals(Username, StringComparison.OrdinalIgnoreCase))
                {
                    
                    yield return new ValidationResult("Password tidak boleh sama dengan Username.", new[] { nameof(Password) });
                }

                var PasswordUmum = new[]
                {
                    "passwordsaya",
                    "123456789101112",
                    "abc123456789",
                    "abcdefghijkl",
                    "121110987654321"
                };

                if (PasswordUmum.Contains(Password.ToLower()))
                {
                    yield return new ValidationResult("Password terlalu umum. Silakan pilih password lain.", new[] { nameof(Password) });
                }

                if (CekKarakterBerulang(Password))
                {
                    yield return new ValidationResult("Password tidak boleh menggunakan karakter yang sama berulang.", 
                    new[] { nameof(Password) });
                }

                if (!Password.Any(char.IsUpper) || !Password.Any(char.IsLower) || !Password.Any(char.IsDigit) || !Password.Any(c => 
                !char.IsLetterOrDigit(c)))
                {
                    yield return new ValidationResult("Password harus mengandung huruf besar, huruf kecil, angka, dan simbol.", 
                    new[] { nameof(Password) });
                }

            }
        }

        private bool CekKarakterBerulang(string password)
        {
            return password.Length > 0 && password.All(c => c == password[0]);
        }
    }
}
