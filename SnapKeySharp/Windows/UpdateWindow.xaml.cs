using SnapKeySharp.Services;
using System.Windows;

namespace SnapKeySharp.Windows
{
    public partial class UpdateWindow : Window
    {
        private readonly UpdateInfo _info;

        public UpdateWindow(UpdateInfo info)
        {
            InitializeComponent();
            _info = info;
            TitleText.Text = $"SnapKey {info.Version}";
            ChangelogText.Text = info.Changelog;
        }

        private void LaterClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UpdateClick(object sender, RoutedEventArgs e)
        {
            UpdateService.ApplyUpdate(_info.DownloadUrl);
        }
    }
}