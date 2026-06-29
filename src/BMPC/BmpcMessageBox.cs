using BMPC.Services;
using BMPC.Views;
using System.Windows;

namespace BMPC
{
    public static class BmpcMessageBox
    {
        public static MessageBoxResult ShowInfo(string message, string title = "BEEmod Music Package Creator")
        {
            Show(message, title, MessageDialogIcon.Information);
            return MessageBoxResult.OK;
        }

        public static MessageBoxResult ShowWarning(string message, string title = "BEEmod Music Package Creator")
        {
            Show(message, title, MessageDialogIcon.Warning);
            return MessageBoxResult.OK;
        }

        public static MessageBoxResult ShowError(string message, string title = "BEEmod Music Package Creator")
        {
            Show(message, title, MessageDialogIcon.Error);
            return MessageBoxResult.OK;
        }

        private static void Show(string message, string title, MessageDialogIcon icon)
        {
            var dialog = new MessageDialog(message, title, icon, MessageDialogButtons.Ok);
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                ?? Application.Current?.MainWindow;
            if (owner != null && owner != dialog)
            {
                dialog.Owner = owner;
            }

            dialog.ShowDialog();
        }
    }
}
