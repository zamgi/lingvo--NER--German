using System;
using System.Windows;

namespace Lingvo.NER.NeuralNetwork.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    public partial class App : Application
    {
        public const string TITLE = "NER markup-corpus-tool";

        static App() => AppDomain.CurrentDomain.UnhandledException += (s, e) => MessageBox.Show( e.ExceptionObject.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error );
    }
}
