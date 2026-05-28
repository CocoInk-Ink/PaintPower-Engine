using Avalonia;
using Avalonia.Controls;

namespace PaintPower.VMPanel
{
    public partial class LoadingBar : UserControl
    {
        private int _pendingPercent = -1;

        public LoadingBar()
        {
            InitializeComponent();

            // Listen for size changes — this fires when layout is ready
            Root.SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (_pendingPercent >= 0)
            {
                ApplyPercent(_pendingPercent);
                _pendingPercent = -1;
            }
        }

        public void SetPercent(int percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            // If width is still 0, wait until SizeChanged fires
            if (Root.Bounds.Width <= 0)
            {
                _pendingPercent = percent;
                return;
            }

            ApplyPercent(percent);
        }

        private void ApplyPercent(int percent)
        {
            double totalWidth = Root.Bounds.Width;
            double newWidth = (percent / 100.0) * totalWidth;

            FillBar.Width = newWidth;
        }
    }
}
