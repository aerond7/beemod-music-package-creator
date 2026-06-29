using BMPC.Models;
using BMPC.Services;
using BMPC.ViewModels;
using System.Windows;

namespace BMPC.Views
{
    public partial class AddSongDialog : Window
    {
        public AddSongDialogViewModel ViewModel { get; private set; }

        public AddSongDialog(SongItemModel? existingModel = null)
            : this(new FileDialogService(), new MessageDialogService(), new AppPaths(), existingModel)
        {
        }

        public AddSongDialog(
            IFileDialogService fileDialogService,
            IMessageDialogService messageDialogService,
            IAppPaths appPaths,
            SongItemModel? existingModel = null)
        {
            ThemeService.PrepareWindow(this);
            InitializeComponent();
            this.ViewModel = new AddSongDialogViewModel(fileDialogService, messageDialogService, appPaths, existingModel);
            this.DataContext = ViewModel;

            this.ViewModel.RequestClose += Close;
            this.ViewModel.RequestUpdateDialogResult += (result) => this.DialogResult = result;
        }

        private void TxtSelection_GotFocus(object sender, RoutedEventArgs e)
        {
            //UCGrid.Focus();
        }
    }
}
