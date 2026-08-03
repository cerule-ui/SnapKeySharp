using System;
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

namespace SnapKeySharp.Windows
{
    /// <summary>
    /// Interaction logic for AddExclusionWindow.xaml
    /// </summary>
    public partial class AddExclusionWindow : Window
    {
        public string ProcessName { get; private set; } = "";

        public AddExclusionWindow()
        {
            InitializeComponent();
        }

        private void SearchFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Executable|*.exe";
            if (dialog.ShowDialog() == true)
            {
                TextBoxProcessName.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxProcessName.Text)) return;
            ProcessName = System.IO.Path.GetFileNameWithoutExtension(TextBoxProcessName.Text.Trim());
            DialogResult = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
