using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace SnapKeyLauncher
{
    public partial class UpdateProgressWindow : Window
    {
        private CancellationTokenSource _cts = new();

        public UpdateProgressWindow(string title)
        {
            InitializeComponent();
            TitleText.Text = title;
        }

        public void SetStep(string step) =>
            Dispatcher.BeginInvoke(() => StepText.Text = step);

        public void SetProgress(int percent) =>
            Dispatcher.BeginInvoke(() =>
            {
                ProgressBar.Value = percent;
                PercentText.Text = $"{percent}%";
            });

        public void HideCancel() =>
            Dispatcher.BeginInvoke(() => CancelBtn.Visibility = Visibility.Collapsed);

        public void SetMirrorMode() =>
            Dispatcher.BeginInvoke(() =>
                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)));

        public CancellationToken Token => _cts.Token;

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            Close();
        }
    }
}