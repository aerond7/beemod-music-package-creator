using BMPC.Core;
using BMPC.Interfaces;
using BMPC.Models;
using System.IO;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace BMPC.UserControls
{
    /// <summary>
    /// Interaction logic for PackageDetailsUC.xaml
    /// </summary>
    public partial class PackageDetailsUC : UserControl, ICreatePackageSetupStage
    {
        public string PackName { get; private set; } = "";
        public string PackDesc { get; private set; } = "";
        public string? PackGroup { get; private set; }

        private readonly string? _originalPackageName;

        public PackageDetailsUC(string? initialName = null, string? initialDescription = null, string? initialGroup = null)
        {
            InitializeComponent();
            _originalPackageName = initialName;
            if (initialName != null) TxtName.Text = initialName;
            if (initialDescription != null) TxtDescription.Text = initialDescription;
            if (initialGroup != null) TxtGroup.Text = initialGroup;
        }

        public string GetStageName()
        {
            return "Package details";
        }

        public string GetStageDescription()
        {
            return "Enter basic details about your new package, these are not details about the music but the package as a whole.";
        }

        public PackageSetupStageValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text.Trim())) return new PackageSetupStageValidationResult(false, "Enter a package name");
            if (TxtName.Text.Trim().Length < 3) return new PackageSetupStageValidationResult(false, "Package name must be at least 3 characters");

            if (string.IsNullOrWhiteSpace(TxtDescription.Text.Trim())) return new PackageSetupStageValidationResult(false, "Enter a description");

            var safeName = Utils.ConvertToSafeFileName(TxtName.Text.Trim()).ToLowerInvariant();
            var originalSafeName = _originalPackageName != null
                ? Utils.ConvertToSafeFileName(_originalPackageName).ToLowerInvariant()
                : null;

            if (safeName != originalSafeName &&
                File.Exists(System.IO.Path.Combine(Constants.PackagesDirectory,
                            string.Format(Constants.PackageIdPattern, safeName) + ".bmpc")))
                return new PackageSetupStageValidationResult(false, "A package with this name already exists");

            this.PackName = TxtName.Text.Trim();
            this.PackDesc = TxtDescription.Text.Trim();
            this.PackGroup = string.IsNullOrWhiteSpace(TxtGroup.Text.Trim()) ? null : TxtGroup.Text.Trim();

            return new PackageSetupStageValidationResult
            {
                IsValid = true
            };
        }
    }
}
