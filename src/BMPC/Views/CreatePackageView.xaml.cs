using BMPC.Core.Models;
using BMPC.Services;
using BMPC.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace BMPC.Views
{
    public partial class CreatePackageView : Window
    {
        private CreatePackageViewModel ViewModel { get; set; }

        public CreatePackageView(
            IMessageDialogService messageDialogService,
            IFileDialogService fileDialogService,
            IAppPaths appPaths)
            : this(messageDialogService, fileDialogService, appPaths, null)
        {
        }

        public CreatePackageView(
            IMessageDialogService messageDialogService,
            IFileDialogService fileDialogService,
            IAppPaths appPaths,
            BmpcPackage? existingPackage)
        {
            ThemeService.PrepareWindow(this);
            InitializeComponent();
            this.ViewModel = new CreatePackageViewModel(messageDialogService, fileDialogService, appPaths, existingPackage);
            this.ViewModel.RequestClose += Close;
            this.ViewModel.RequestUpdateDialogResult += (result) => this.DialogResult = result;
            this.DataContext = ViewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (ViewModel.IsImporting)
            {
                e.Cancel = true;
                base.OnClosing(e);
                return;
            }

            // X button must show the same confirm as the Cancel button.
            if (!ViewModel.AllowClose && !ViewModel.ConfirmCancel())
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }
    }
}
