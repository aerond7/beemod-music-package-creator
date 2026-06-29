namespace BMPC.Services
{
    public enum MessageDialogIcon
    {
        Information,
        Warning,
        Error
    }

    public interface IMessageDialogService
    {
        void ShowInfo(string message, string title = "BEEmod Music Package Creator");

        void ShowWarning(string message, string title = "BEEmod Music Package Creator");

        void ShowError(string message, string title = "BEEmod Music Package Creator");

        bool Confirm(string message, string title, MessageDialogIcon icon = MessageDialogIcon.Warning);
    }
}
