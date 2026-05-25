using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using FolMinder2.Infrastructure;

namespace FolMinder2.Presentation
{
    public class Toast : IDisposable
    {
        private readonly Window _owner;
        private bool disposedValue;

        public Toast(Window owner)
        {
            _owner = owner;
            WeakReferenceMessenger.Default.Register<ToastMessage>(this, OnToastMessage);
        }

        private void OnToastMessage(object recipient, ToastMessage message)
        {
            if (_owner.IsActive && _owner.IsVisible)
            {
                ShowToast(message);
            }
        }

        private void ShowToast(ToastMessage message)
        {
            var toastWindow = new ToastWindow(message)
            {
                Owner = _owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            toastWindow.Show();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    WeakReferenceMessenger.Default.Unregister<ToastMessage>(this);
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
