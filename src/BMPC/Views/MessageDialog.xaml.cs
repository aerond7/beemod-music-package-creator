using BMPC.Services;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;

namespace BMPC.Views
{
    public partial class MessageDialog : Window
    {
        public MessageDialogResult Result { get; private set; } = MessageDialogResult.Cancel;

        public MessageDialog(string message, string title, MessageDialogIcon icon, MessageDialogButtons buttons)
        {
            InitializeComponent();
            ThemeService.PrepareWindow(this);

            var dialogTitle = GetDialogTitle(title, icon);
            DataContext = new MessageDialogModel(
                dialogTitle,
                message,
                GetIconBrush(icon));

            DialogIcon.Kind = GetIconKind(icon);
            Title = dialogTitle;

            var isConfirmation = buttons == MessageDialogButtons.YesNo;
            YesButton.Visibility = isConfirmation ? Visibility.Visible : Visibility.Collapsed;
            NoButton.Visibility = isConfirmation ? Visibility.Visible : Visibility.Collapsed;
            OkButton.Visibility = isConfirmation ? Visibility.Collapsed : Visibility.Visible;
        }

        private static PackIconKind GetIconKind(MessageDialogIcon icon)
            => icon switch
            {
                MessageDialogIcon.Information => PackIconKind.InformationOutline,
                MessageDialogIcon.Error => PackIconKind.AlertCircleOutline,
                _ => PackIconKind.AlertOutline
            };

        private static Brush GetIconBrush(MessageDialogIcon icon)
            => icon switch
            {
                MessageDialogIcon.Information => new SolidColorBrush(Color.FromRgb(19, 143, 255)),
                MessageDialogIcon.Error => new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                _ => new SolidColorBrush(Color.FromRgb(217, 108, 0))
            };

        private static string GetDialogTitle(string title, MessageDialogIcon icon)
        {
            if (!string.Equals(title, "BEEmod Music Package Creator", StringComparison.Ordinal))
            {
                return title;
            }

            return icon switch
            {
                MessageDialogIcon.Information => "Information",
                MessageDialogIcon.Error => "Error",
                _ => "Warning"
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.Ok;
            DialogResult = true;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.Yes;
            DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.No;
            DialogResult = false;
        }

        private sealed record MessageDialogModel(string DialogTitle, string Message, Brush IconBackground);
    }

    public enum MessageDialogButtons
    {
        Ok,
        YesNo
    }

    public enum MessageDialogResult
    {
        Ok,
        Yes,
        No,
        Cancel
    }
}
