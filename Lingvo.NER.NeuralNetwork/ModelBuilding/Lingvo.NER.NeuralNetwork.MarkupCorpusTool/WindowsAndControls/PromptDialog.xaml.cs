using System;
using System.Windows;
using System.Windows.Input;

namespace Lingvo.NER.NeuralNetwork.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PromptDialog : Window
    {
        public PromptDialog() => InitializeComponent();

        public string ReplacedText
        {
            get => textBox1.Text;
            set => textBox1.Text = value; 
        }
        public bool HasReplacedText => (ReplacedText != null); 

        private bool? _ShowDialogResult;
        public bool? ShowDialogResult
        {
            get => _ShowDialogResult; 
            private set
            {
                if ( _ShowDialogResult != value )
                {
                    _ShowDialogResult = value;

                    Visibility = Visibility.Hidden;
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if ( !ReplacedText.IsNullOrWhiteSpace() )
            {
                ShowDialogResult = true;
            }
            else
            {
                textBox1.Focus();
                textBox1.SelectAll();
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if ( e.Key == Key.Escape )
            {
                ShowDialogResult = false;
            }
        }
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            _ShowDialogResult = null;

            textBox1.Focus();
            textBox1.SelectAll();
        }
    }
}
