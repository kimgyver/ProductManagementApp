using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class NoSpecialCharactersAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is string str && str.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return new ValidationResult("Special characters are not allowed.");
        }
        return ValidationResult.Success;
    }
}