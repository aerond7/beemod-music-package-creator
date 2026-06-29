using BMPC.Interfaces;
using BMPC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BMPC.UserControls
{
    /// <summary>
    /// Interaction logic for PackageSummaryUC.xaml
    /// </summary>
    public partial class PackageSummaryUC : UserControl, ICreatePackageSetupStage
    {
        private readonly bool _isEditMode;

        public PackageSummaryUC(bool isEditMode = false)
        {
            InitializeComponent();
            _isEditMode = isEditMode;
        }

        public string GetStageName()
        {
            return "Summary";
        }

        public string GetStageDescription()
        {
            return _isEditMode
                ? "Here's the summary about the music package you are about to update."
                : "Here's the summary about the new music package you are about to create.";
        }

        public PackageSetupStageValidationResult Validate()
        {
            return new PackageSetupStageValidationResult
            {
                IsValid = true
            };
        }
    }
}
