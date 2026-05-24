using System;
using System.Collections.Generic;
using System.Text;

namespace FolMinder2.Presentation
{
    public class DialogRequiredEventArgs : EventArgs
    {
        public object ViewModel { get; }
        public bool? DialogResult { get; set; }

        public DialogRequiredEventArgs(object viewModel)
        {
            ViewModel = viewModel;
        }
    }
}
