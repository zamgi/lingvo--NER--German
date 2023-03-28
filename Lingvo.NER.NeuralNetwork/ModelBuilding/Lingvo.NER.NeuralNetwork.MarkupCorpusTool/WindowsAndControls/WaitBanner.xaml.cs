using System.Windows.Controls;

namespace Lingvo.NER.NeuralNetwork.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    public partial class WaitBanner : UserControl
	{
		public WaitBanner() => InitializeComponent();
        public string Text
        {
            get => txt_Title.Text;
            set => txt_Title.Text = value.IsNullOrWhiteSpace() ? "...processing..." : value; 
        }
        public void ResetText() => Text = "...processing...";
	}
}
