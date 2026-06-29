namespace BMPC.Models
{
    public class PackageSetupStageValidationResult
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }

        public PackageSetupStageValidationResult()
        { }

        public PackageSetupStageValidationResult(bool isValid, string? message = null)
        {
            this.IsValid = isValid;
            this.Message = message;
        }
    }
}
