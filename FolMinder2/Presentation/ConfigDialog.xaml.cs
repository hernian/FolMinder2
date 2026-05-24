using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FolMinder2.ViewModels;

namespace FolMinder2.Presentation
{
    /// <summary>
    /// ConfigDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigDialog : Window
    {
        private readonly ConfigViewModel _viewModel;

        public ConfigDialog(ConfigViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = viewModel;
            _viewModel.AcceptRequired += viewModel_AcceptRequired;
            _viewModel.CancelRequired += viewModel_CancelRequired;
            this.ContentRendered += ConfigDialog_ContentRendered;
            this.Loaded += ConfigDialog_Loaded;
        }

        private void viewModel_AcceptRequired(object? sender, EventArgs e)
        {
            this.DialogResult = true;
        }

        private void viewModel_CancelRequired(object? sender, EventArgs e)
        {
            this.DialogResult = false;
        }

        private void ConfigDialog_ContentRendered(object? sender, EventArgs e)
        {
            if (_viewModel.SelectedItem is not null)
            {
                keyListBox.ScrollIntoView(_viewModel.SelectedItem);
            }
        }
        private void ConfigDialog_Loaded(object sender, RoutedEventArgs e)
        {
            checkAlt.Focus();
            Keyboard.Focus(checkAlt);
        }
    }
}
