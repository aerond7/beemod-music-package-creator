using BMPC.Models;

namespace BMPC.Interfaces
{
    public interface ICreatePackageSetupStage
    {
        string GetStageName();
        string GetStageDescription();
        PackageSetupStageValidationResult Validate();
    }
}
