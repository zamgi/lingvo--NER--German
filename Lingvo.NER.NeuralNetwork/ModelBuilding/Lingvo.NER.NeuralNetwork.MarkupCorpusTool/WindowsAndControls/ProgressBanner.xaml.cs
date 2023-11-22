using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Lingvo.NNER.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ProgressBanner : UserControl
	{
        private const string PROGRESS_TITLE = "...processing...";   

        public ProgressBanner( string fileName ) 
        {
            InitializeComponent();

            _FileName         = fileName;
            txt_Filename.Text = fileName;
        }
		/*public ProgressBanner( bool showCancelButton )
		{
			InitializeComponent();

            btn_Cancel.Visibility = showCancelButton ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
		}*/

		public string Text
		{
			get => txt_Title.Text;
			set => txt_Title.Text = value; 
		}

        private string _FileName;
        private double _FilenameActualWidth;
        private void ViewFileName()
        {
            const double CHAR_WIDTH = 6.5;
            
            if ( Math.Abs( _FilenameActualWidth - txt_Filename.ActualWidth ) > double.Epsilon ) //---if ( _FilenameActualWidth != txt_Filename.ActualWidth )
            {
                _FilenameActualWidth = txt_Filename.ActualWidth;
                var charsCountWithoutEllipsis = Convert.ToInt32( _FilenameActualWidth / CHAR_WIDTH );
                txt_Filename.Text = _FileName.MinimizePath( charsCountWithoutEllipsis );
            }
        }

        public Action Cancelcallback { get; set; }

        public void SetProgressText( double percentValue )
        {
            Text = PROGRESS_TITLE + " (" + percentValue.ToString( "N2" ) + "%)";

            ViewFileName();
        }
        public void SetProgressTextOverDispatcher( double percentValue )
        {
            var action = new Action< double >( SetProgressText );

            Dispatcher.BeginInvoke( action, DispatcherPriority.DataBind, percentValue ).Wait();
        }

        private void btn_Cancel_Click(object sender, MouseButtonEventArgs e) => Cancelcallback?.Invoke();
    }
}
