using BMPC.Views;
using System.Windows;

namespace BMPC.Services
{
    public sealed class MessageDialogService : IMessageDialogService
    {
        public void ShowInfo(string message, string title = "BEEmod Music Package Creator")
            => Show(message, title, MessageDialogIcon.Information, MessageDialogButtons.Ok);

        public void ShowWarning(string message, string title = "BEEmod Music Package Creator")
            => Show(message, title, MessageDialogIcon.Warning, MessageDialogButtons.Ok);

        public void ShowError(string message, string title = "BEEmod Music Package Creator")
            => Show(message, title, MessageDialogIcon.Error, MessageDialogButtons.Ok);

        public bool Confirm(string message, string title, MessageDialogIcon icon = MessageDialogIcon.Warning)
            => Show(message, title, icon, MessageDialogButtons.YesNo) == MessageDialogResult.Yes;

        private static MessageDialogResult Show(string message, string title, MessageDialogIcon icon, MessageDialogButtons buttons)
        {
            var dialog = new MessageDialog(message, title, icon, buttons);
            var owner = GetOwner();
            if (owner != null && owner != dialog)
            {
                dialog.Owner = owner;
            }

            dialog.ShowDialog();
            return dialog.Result;
        }

        private static Window? GetOwner()
            => Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                ?? Application.Current?.MainWindow;
    }
}
