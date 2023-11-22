using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Lingvo.NNER.MarkupCorpusTool
{
    /// <summary>
    /// Provides a popup window to display a splash logo for a specific duration.
    /// </summary>
    internal class Splash : Popup
    {
        private DispatcherTimer _CloseTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Splash"/> class.
        /// </summary>
        /// <remarks>
        /// Displays the animated splash screen and starts a timer to close the screen.
        /// </remarks>
        public Splash()
        {
            this.AllowsTransparency = true;
            this.Placement          = PlacementMode.Center;
            this.PlacementRectangle = new Rect( 0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight );

            var splashAnimateImage = new SplashAnimateImage();
            splashAnimateImage.InitializeComponent();
            this.Child = splashAnimateImage;

            _CloseTimer          = new DispatcherTimer();
            _CloseTimer.Interval = TimeSpan.FromSeconds( 3 );
            _CloseTimer.Tick    += (sender, eventArgs) =>
            {
                _CloseTimer.Stop();
                this.IsOpen = false;
            };
            _CloseTimer.Start();
        }
    }
}
