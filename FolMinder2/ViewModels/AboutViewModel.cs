using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FolMinder2.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = string.Empty;

        [ObservableProperty]
        private string _company = string.Empty;

        [ObservableProperty]
        private string _product = string.Empty;

        [ObservableProperty]
        private string _copyright = string.Empty;

        public AboutViewModel()
        {
            var asm = Assembly.GetExecutingAssembly();
            this.Version = asm.GetName().Version?.ToString()
                ?? throw new InvalidOperationException("Missing version info");
            this.Company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
                ?? throw new InvalidOperationException("Missing company info");
            this.Product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                ?? throw new InvalidOperationException("Missing product info");
            this.Copyright = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
                ?? throw new InvalidOperationException("Missing copyright info");
        }
    }
}
