using BlazorBootstrap;

namespace GrapheneTrace.Client.Services
{
    public class AppToastService
    {
        private Action<ToastMessage>? _onNotify;

        public void Subscribe(Action<ToastMessage> callback)
        {
            _onNotify = callback;
        }

        public void Notify(ToastMessage toast)
        {
            _onNotify?.Invoke(toast);
        }

        public void ShowSuccess(string message) => Notify(new ToastMessage(ToastType.Success, message));
        public void ShowWarning(string message) => Notify(new ToastMessage(ToastType.Warning, message));
        public void ShowError(string message) => Notify(new ToastMessage(ToastType.Danger, message));
        public void ShowInfo(string message) => Notify(new ToastMessage(ToastType.Info, message));
    }
}
