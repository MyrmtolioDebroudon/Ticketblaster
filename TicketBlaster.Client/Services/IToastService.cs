namespace TicketBlaster.Client.Services
{
    public interface IToastService
    {
        event Action<string, ToastLevel>? OnShow;
        
        void ShowSuccess(string message);
        void ShowInfo(string message);
        void ShowWarning(string message);
        void ShowError(string message);
    }

    public enum ToastLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class ToastService : IToastService
    {
        public event Action<string, ToastLevel>? OnShow;

        public void ShowSuccess(string message)
        {
            OnShow?.Invoke(message, ToastLevel.Success);
        }

        public void ShowInfo(string message)
        {
            OnShow?.Invoke(message, ToastLevel.Info);
        }

        public void ShowWarning(string message)
        {
            OnShow?.Invoke(message, ToastLevel.Warning);
        }

        public void ShowError(string message)
        {
            OnShow?.Invoke(message, ToastLevel.Error);
        }
    }
}