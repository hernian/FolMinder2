using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;

namespace FolMinder2.Infrastructure
{
    public enum ToastType
    {
        Error,
        Information
    }
    public record ToastMessage(ToastType Type, string Text)
    {
        public static void SendInformation(string text) => Send(ToastType.Information, text);
        public static void SendError(string text) => Send(ToastType.Error, text);
        public static void Send(ToastType type, string text)
        {
            var message = new ToastMessage(type, text);
            WeakReferenceMessenger.Default.Send(message);
        }
    }
}
