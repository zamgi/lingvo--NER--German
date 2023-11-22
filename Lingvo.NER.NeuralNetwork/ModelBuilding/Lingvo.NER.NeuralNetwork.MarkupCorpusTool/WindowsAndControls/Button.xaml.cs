using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Lingvo.NNER.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Button : UserControl
    {
        public Button()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += new MouseButtonEventHandler( Button_MouseLeftButtonDown );
            this.MouseLeftButtonUp   += new MouseButtonEventHandler( Button_MouseLeftButtonUp );
            this.MouseLeave          += new MouseEventHandler( Button_MouseLeave );
        }

        private void Button_MouseLeave( object sender, EventArgs e )
        {
            var release = (Storyboard) this.canvas.FindResource( "release" );
            release.Begin( this );
        }

        private void Button_MouseLeftButtonUp( object sender, MouseEventArgs e )
        {
            var release = (Storyboard) this.canvas.FindResource( "release" );
            release.Begin( this );
        }

        private void Button_MouseLeftButtonDown( object sender, MouseEventArgs e )
        {
            var press = (Storyboard) this.canvas.FindResource( "press" );
            press.Begin( this );
        }

        public string Text
        {
            get => text.Text;
            set
            {
                text.Text = value;
                //text.SetValue(Canvas.LeftProperty, (double)((outline.Width - text.ActualWidth) / 2));
            }
        }
    }
}
