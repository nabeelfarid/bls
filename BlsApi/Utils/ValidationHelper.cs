using System.ComponentModel.DataAnnotations;

namespace BlsApi.Utils
{
    public static class ValidationHelper
    {
        public static (bool IsValid, List<string> Errors) Validate<T>(T obj)
        {
            if (obj == null)
            {
                return (false, new List<string> { "Object cannot be null" });
            }

            var context = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

            var errors = results
                .Select(r => r.ErrorMessage ?? "Validation error")
                .ToList();

            return (isValid, errors);
        }
    }
}

