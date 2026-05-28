namespace ProGlassAutomation.Models
{
    public class ValidationError
    {
        public string Field { get; }
        public string Message { get; }

        public ValidationError(string field, string message)
        {
            Field = field;
            Message = message;
        }
    }

    public class ValidationResult
    {
        private readonly System.Collections.Generic.List<ValidationError> _errors =
            new System.Collections.Generic.List<ValidationError>();

        public bool IsValid => _errors.Count == 0;
        public System.Collections.Generic.IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

        public void AddError(string field, string message)
        {
            _errors.Add(new ValidationError(field, message));
        }

        public string GetErrorSummary()
        {
            if (IsValid) return "Validation passed.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Found {_errors.Count} validation error(s):");
            foreach (var error in _errors)
            {
                sb.AppendLine($"  • {error.Field}: {error.Message}");
            }
            return sb.ToString();
        }
    }
}