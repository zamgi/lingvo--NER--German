using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Microsoft.Win32;

using Lingvo.NNER.MarkupCorpusTool.Properties;

namespace Lingvo.NNER.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string SAVE_QUESTION_MESSAGEBOX_TEXT = "\t Save your changes? \t\t";
        private readonly FileModel _FileModel;

        public MainWindow()
        {
            this.DataContext = _FileModel = new FileModel();
            #region comm.
            /*_FileModel.PropertyChanged += (s, e) => 
            {  
                switch ( e.PropertyName )
                {
                    case "HasLoadedFile":
                        {
                        var action = new Action( () => { 
                            pageNumberGrid.IsEnabled = _FileModel.HasLoadedFile; 
                            refreshButton .IsEnabled = _FileModel.HasLoadedFile; 
                            findButton    .IsEnabled = _FileModel.HasLoadedFile; 
                        } );
                        Dispatcher.BeginInvoke( action, DispatcherPriority.ApplicationIdle ).Wait();
                        }
                    break;

                    case "HasChanges":
                        {
                        var action = new Action( () => { 
                            saveButton.IsEnabled = _FileModel.HasChanges; 
                        } );
                        Dispatcher.BeginInvoke( action, DispatcherPriority.ApplicationIdle ).Wait();
                        }
                    break;
                }
            };*/
            #endregion

            InitializeComponent();

            #region [.splash.]
            if ( Config.ShowSplashOnStartup )
            {
                this.Visibility = Visibility.Hidden;

                var splash = new Splash();
                splash.Closed += delegate { this.Visibility = Visibility.Visible; };
                splash.IsOpen = true;
            }            
            #endregion

            #region [.window location & size.]
            if ( Settings.Default.WindowState != WindowState.Normal )
            {
                this.WindowState = WindowState.Maximized;
            }
            if ( Settings.Default.WindowRect.Size != new Size(0, 0) )
            {
                this.Top    = Settings.Default.WindowRect.Top;
                this.Left   = Settings.Default.WindowRect.Left;
                this.Height = Settings.Default.WindowRect.Height;
                this.Width  = Settings.Default.WindowRect.Width;
                
                this.WindowStartupLocation = WindowStartupLocation.Manual;
            }
            #endregion

            #region [.page-size.]
            pageSizeComboBox.TrySetSelectedValue( Settings.Default.PageSize.ToString() );

            flowDocumentScrollViewer.Zoom = Settings.Default.DocumentZoom;
            flowDocumentPageViewer  .Zoom = Settings.Default.DocumentZoom;
            #endregion

            this.Title = App.TITLE;

            FillContextMenu();

            #region [.comm. winproc.]
            //DataContext = this;

            /*
            var source = System.Windows.Interop.HwndSource.FromHwnd( new System.Windows.Interop.WindowInteropHelper( this ).Handle );
            source.AddHook( HwndSourceHook );
            */
            #endregion
        }
        //private IntPtr HwndSourceHook( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled ) => IntPtr.Zero;

        protected override void OnClosing(CancelEventArgs e)
        {
            if ( _FileModel.HasChanges )
            {
                var res = MessageBox.Show( SAVE_QUESTION_MESSAGEBOX_TEXT, App.TITLE, MessageBoxButton.YesNoCancel, MessageBoxImage.Question );
                if ( res == MessageBoxResult.Cancel )
                {
                    e.Cancel = true;
                }
                else if ( res == MessageBoxResult.Yes )
                {
                    _FileModel.SaveDocumentOnDisk();
                }
            }

            base.OnClosing(e);

            #region [.window location & size.]
            if ( !e.Cancel )
            {
                Settings.Default.WindowState = this.WindowState;
                Settings.Default.WindowRect  = new Rect( this.Left, this.Top, this.ActualWidth, this.ActualHeight );

                Settings.Default.DocumentZoom = (flowDocumentPageViewer.Visibility == Visibility.Visible) ? flowDocumentPageViewer.Zoom : flowDocumentScrollViewer.Zoom;
                Settings.Default.Save();
            }
            #endregion
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if ( _PromptDialog.IsValueCreated )
            {
                _PromptDialog.Value.Close();
            }
        }
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if ( (e.Key == Key.F5) && _FileModel.HasLoadedFile )
            {
                RefreshPage();
            }
            else if ( e.Key == Key.F1 )
            {
                var text = $"\"{AssemblyInfoHelper.AssemblyTitle}\"" + Environment.NewLine +
                           //AssemblyInfoHelper.AssemblyProduct + Environment.NewLine +
                           AssemblyInfoHelper.AssemblyCopyright + Environment.NewLine +
                           //AssemblyInfoHelper.AssemblyCompany + Environment.NewLine +
                           //AssemblyInfoHelper.AssemblyDescription + Environment.NewLine +
                           Environment.NewLine +
                           $"Version {AssemblyInfoHelper.AssemblyVersion}, ({AssemblyInfoHelper.AssemblyLastWriteTime})";
                MessageBox.Show( text, "about", MessageBoxButton.OK, MessageBoxImage.Information );
            }
            else if ( (e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control )
            {
                #region [.switch-by-key.]
                switch ( e.Key )
                {
                    case Key.F:
                        if ( flowDocumentPageViewer.Visibility == Visibility.Visible )
                        {
                            if ( !flowDocumentPageViewer.IsKeyboardFocusWithin )
                            flowDocumentPageViewer.Find();
                        }
                        else
                        {
                            if ( !flowDocumentScrollViewer.IsKeyboardFocusWithin )
                            flowDocumentScrollViewer.Find();
                        }                        
                    break;
                    case Key.O:
                        LoadFile_MenuItemClick( this, e );
                    break;
                    case Key.S:
		                if ( _FileModel.HasChanges )
                        {
                            SaveFile_MenuItemClick( this, e );
                        } 
                    break;
                    case Key.PageUp:                    
                        if ( _FileModel.HasLoadedFile && (0 < pageNumberComboBox.SelectedIndex) )
                        {
                            pageNumberComboBox.SelectedIndex--;
                        }      
                    break;
                    case Key.PageDown:
                        if ( _FileModel.HasLoadedFile && 
                             (pageNumberComboBox.SelectedIndex < pageNumberComboBox.Items.Count - 1) )
                        {
                            pageNumberComboBox.SelectedIndex++;
                        } 
                    break;
                }
                #endregion
            }
            else if ( e.Key == Key.Escape )
            {
                if ( _CancellationTokenSource != null )
                {
                    _CancellationTokenSource.Cancel();

                    _CancellationTokenSource.Dispose();
                    _CancellationTokenSource = null;
                }
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            flowDocument.AllowDrop = true;
            flowDocument.DragEnter += flowDocument_DragEnter;
            flowDocument.DragOver  += flowDocument_DragOver;
            flowDocument.Drop      += flowDocument_Drop;

            if ( Config.AutoOpenLastOpenedFile && Settings.Default.LastOpenedFileName.FileExists() )
            {
                StartLoadFileContentTask( Settings.Default.LastOpenedFileName );
            }
            else if ( Config.ShowOpenFileDialogOnStarted )
            {
                LoadFile_MenuItemClick( sender, e );
            }
        }

        private void flowDocument_DragEnter( object sender, DragEventArgs e )
        {
            var files = e.Data.GetDataPresent( DataFormats.FileDrop ) ? (string[]) e.Data.GetData( DataFormats.FileDrop ) : null;
            var allow = files.AnyEx() && (files.Length == 1);
            e.Effects = allow ? e.AllowedEffects : DragDropEffects.None;
            e.Handled = allow;
        }
        private void flowDocument_DragOver( object sender, DragEventArgs e ) => flowDocument_DragEnter( sender, e );
        private void flowDocument_Drop( object sender, DragEventArgs e )
        {
            var files = e.Data.GetDataPresent( DataFormats.FileDrop ) ? (string[]) e.Data.GetData( DataFormats.FileDrop ) : null;
            if ( files.AnyEx() && (files.Length == 1) )
            {
                var first_file = files[ 0 ];
                if ( !File.Exists( first_file ) ) return;

                if ( _FileModel.HasChanges )
                {
                    var res = MessageBox.Show( SAVE_QUESTION_MESSAGEBOX_TEXT, App.TITLE, MessageBoxButton.YesNoCancel, MessageBoxImage.Question );
                    switch ( res )
                    {
                        case MessageBoxResult.Yes: _FileModel.SaveDocumentOnDisk(); break;
                        case MessageBoxResult.No: break;
                        case MessageBoxResult.Cancel: return;
                    }
                }

                Settings.Default.LastOpenedFileName = first_file;
                Settings.Default.Save();

                StartLoadFileContentTask( first_file );
            }
        }

        private void LoadFile_MenuItemClick(object sender, RoutedEventArgs e)
        {
            if ( _FileModel.HasChanges )
            {
                var res = MessageBox.Show( SAVE_QUESTION_MESSAGEBOX_TEXT, App.TITLE, MessageBoxButton.YesNoCancel, MessageBoxImage.Question );
                switch ( res )
                {
                    case MessageBoxResult.Yes: _FileModel.SaveDocumentOnDisk(); break;
                    case MessageBoxResult.No: break;
                    case MessageBoxResult.Cancel: return;
                }
            }

            var ofd = new OpenFileDialog() 
            {
                CheckFileExists  = true,
                RestoreDirectory = true,
                Multiselect      = false,
                FileName         = Settings.Default.LastOpenedFileName,
            };
            if ( ofd.ShowDialog().GetValueOrDefault() )
            {
                Settings.Default.LastOpenedFileName = ofd.FileName;
                Settings.Default.Save();

                StartLoadFileContentTask( ofd.FileName );
            }
        }
        private void Refresh_MenuItemClick(object sender, RoutedEventArgs e)
        {
            if ( _FileModel.HasLoadedFile )
            {
                RefreshPage();
            }
        }
        private void SaveFile_MenuItemClick(object sender, RoutedEventArgs e) => _FileModel.SaveDocumentOnDiskAsync();
        private void Find_MenuItemClick(object sender, RoutedEventArgs e)
        {
            if ( flowDocumentPageViewer.Visibility == Visibility.Visible )
            {
                flowDocumentPageViewer.Find();
            }
            else
            {
                flowDocumentScrollViewer.Find();
            }
        }

        private void pageNumberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (ComboBoxItem) pageNumberComboBox.SelectedItem;
            if ( (item != null) && (item.Content != null) && _FileModel.HasLoadedFile )
            {
                PageNumberChange( item.Content.ToString().ToInt32() - 1 );
            }            
        }
        private void pageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (ComboBoxItem) pageSizeComboBox.SelectedItem;
            if ( (item != null) && (item.Content != null) && _FileModel.HasLoadedFile )
            {
                Settings.Default.PageSize = item.Content.ToString().ToInt32();
                PageSizeChange( item.Content.ToString().ToInt32() );
            }
        }

        private void ViewAsPageViewer_MenuItemClick(object sender, RoutedEventArgs e)
        {
            var fd = flowDocumentScrollViewer.Document;
            flowDocumentScrollViewer.Document = null;
            flowDocumentPageViewer  .Document = fd;
            flowDocumentPageViewer.Zoom = flowDocumentScrollViewer.Zoom;

            ViewAsScrollViewerMenuItem.IsChecked = false;
            flowDocumentScrollViewer.Visibility = Visibility.Hidden;
            flowDocumentPageViewer  .Visibility = Visibility.Visible;
        }
        private void ViewAsScrollViewer_MenuItemClick(object sender, RoutedEventArgs e)
        {
            var fd = (FlowDocument) flowDocumentPageViewer.Document;
            flowDocumentPageViewer  .Document = null;
            flowDocumentScrollViewer.Document = fd;
            flowDocumentScrollViewer.Zoom = flowDocumentPageViewer.Zoom;

            ViewAsPageViewerMenuItem.IsChecked = false;
            flowDocumentScrollViewer.Visibility = Visibility.Visible;
            flowDocumentPageViewer  .Visibility = Visibility.Hidden;
        }

        #region [.NER-contextmenu-command.]
        private Lazy< PromptDialog > _PromptDialog = new Lazy< PromptDialog >();

        private SelectedInline[] _SelectedInlines;

        private void FillContextMenu()
        {
            filterDropDownButton.DropDownContextMenu = new ContextMenu();

            var contextMenu = (ContextMenu) FindResource( "contextMenu" );
            foreach ( var mark in Config.Marks.Reverse() )
            {
                var drawing = new GeometryDrawing() 
                { 
                    Brush    = mark.BackgroundBrush,
                    Pen      = mark.Pen,
                    Geometry = new RectangleGeometry { Rect = new Rect(0, 0, 14, 14) }
                };

                //1.
                var menuItem = new MenuItem() 
                { 
                    Header     = mark.Title, 
                    Icon       = new Image() { Source = new DrawingImage( drawing ) },
                    FontWeight = FontWeights.Bold,
                    Tag        = mark,
                };
                menuItem.Click += new RoutedEventHandler(markupMenuItem_Click);

                contextMenu.Items.Insert( 0, menuItem );

                //2.
                var menuItem2 = new MenuItem() 
                { 
                    Header           = mark.Title, 
                    Icon             = new Image() { Source = new DrawingImage( drawing ) },
                    FontWeight       = FontWeights.Bold,
                    Tag              = mark,
                    StaysOpenOnClick = true,
                };
                menuItem2.Click += new RoutedEventHandler(filterMenuItem_Click);
                filterDropDownButton.DropDownContextMenu.Items.Insert( 0, menuItem2 );
            }
        }

        private void flowDocument_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu_Closed(sender, e);

            var selection = (flowDocumentPageViewer.Visibility == Visibility.Visible) 
                            ? flowDocumentPageViewer.Selection
                            : flowDocumentScrollViewer.Selection;
            
            _SelectedInlines = null;

            if ( selection.IsEmpty )
            {
                var selectedInline = TryGetSelectedInline( e.OriginalSource );
                if ( selectedInline.HasValue && !selectedInline.Value.Inline.IsRunSpace() )
                {
                    _SelectedInlines =  new[] { selectedInline.Value };
                    _SelectedInlines.SetFontStyleAndForeground();
                }
            }
            else
            {
                TextPointer start;
                TextPointer end;
                if ( selection.Text.TrimEnd() != selection.Text )
                {
                    start = GetBackwardBySpace( selection.Start );
                    end = GetBackwardBySpace( selection.End );
                }
                else
                {
                    start = GetBackwardBySpace( selection.Start );
                    end = GetForwardBySpace( selection.End );
                }
                if ( start != null && end != null ) 
                {
                    _SelectedInlines = TryGetSelectedInlines( start, end );
                    if ( _SelectedInlines != null )
                    {
                        _SelectedInlines.SetFontStyleAndForeground();                        
                    }

                    selection.Select( start, start );
                }
            }

            e.Handled = (_SelectedInlines == null);
        }
        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            if ( _SelectedInlines != null )
            {
                _SelectedInlines.ResetFontStyleAndForeground();

                _SelectedInlines = null;
            }
        }        

        private void markupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ( _SelectedInlines != null )
            {
                var menuItem = (MenuItem) e.OriginalSource;
                var mark     = (Mark)     menuItem.Tag;

                var selectedInlines = _SelectedInlines;
                var numbers = GetNumbers( selectedInlines );
                if ( numbers.AnyEx() )
                {                    
                    var replacedInlines = _FileModel.MarkupCommand( numbers, mark.TagName );

                    selectedInlines.ReplaceWith( replacedInlines );
                }
            }
        }
        private void unmarkMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ( _SelectedInlines != null )
            {
                var selectedInlines = _SelectedInlines;
                var numbers = GetNumbers( selectedInlines );
                if ( numbers.AnyEx() )
                {                    
                    var replacedInlines = _FileModel.UnmarkCommand( numbers );

                    selectedInlines.ReplaceWith( replacedInlines );
                }
            }
        }
        private void deleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ( _SelectedInlines != null )
            {
                var selectedInlines = _SelectedInlines;
                var numbers = GetNumbers( selectedInlines );
                if ( numbers.AnyEx() )
                {                    
                    _FileModel.DeleteCommand( numbers );

                    selectedInlines.Remove();
                }
            }
        }
        private void replaceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ( _SelectedInlines != null )
            {
                var selectedInlines = _SelectedInlines;
                   _SelectedInlines = null;

                var replacedText = selectedInlines.InnerText();
                _PromptDialog.Value.ReplacedText = replacedText;
                try
                {
                    _PromptDialog.Value.ShowDialog();
                }
                catch ( InvalidOperationException )
                {
                    _PromptDialog = new Lazy< PromptDialog >();
                    _PromptDialog.Value.ReplacedText = replacedText;
                    _PromptDialog.Value.ShowDialog();
                }                
                if ( _PromptDialog.Value.ShowDialogResult.GetValueOrDefault( false ) && _PromptDialog.Value.HasReplacedText )
                {
                    var numbers = GetNumbers( selectedInlines );
                    if ( numbers.AnyEx() )
                    {
                        var replacedInlines = _FileModel.ReplaceCommand( numbers, _PromptDialog.Value.ReplacedText );

                        selectedInlines.ReplaceWith( replacedInlines );
                    }
                }

                selectedInlines.ResetFontStyleAndForeground();
            }
        }
        private void filterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem) e.OriginalSource;
            var mark     = (Mark)     menuItem.Tag;
            var filter   = false;

            #region [.flip-flop filter-menu-item.]
            var img = (Image) menuItem.Icon;
            if ( img.Visibility == Visibility.Visible )
            {                
                img.Visibility = Visibility.Hidden;
                var textBlock = new TextBlock() { Text = mark.Title, /*Foreground = Brushes.Silver*/ };
                textBlock.TextDecorations.Add( new TextDecoration() { Location = TextDecorationLocation.Strikethrough } );
                menuItem.Header     = textBlock;
                menuItem.FontWeight = FontWeights.Normal;
                filter = true;
            }
            else
            {
                img.Visibility = Visibility.Visible;
                menuItem.Header     = mark.Title;
                menuItem.FontWeight = FontWeights.Bold;
            }
            #endregion

            #region [.flip-flop context-menu-item.]
            var contextMenu = (ContextMenu) FindResource( "contextMenu" );
            foreach ( FrameworkElement item in contextMenu.Items )
            {
                if ( mark.Equals( item.Tag ) )
                {
                    item.Visibility = filter ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
            }
            #endregion

            MessageBox.Show( "NotImplemented" );
        }

        private static SelectedInline? TryGetSelectedInline( object obj )
        {
            var inline = obj as Inline;
            if ( (inline != null) && !(inline is LineBreak) )
            {
                if ( inline.Parent is Span )
                {
                    inline = (Inline) inline.Parent;
                }

                return (new SelectedInline( inline ));
            }
            return (null);
        }
        private static SelectedInline[] TryGetSelectedInlines( TextPointer start, TextPointer end )
        {
            var selectedInlines = new List< SelectedInline >( Config.MaxChosenWordsInlinesCount );
            for ( var inline = (Inline) start.Parent; inline != null; )
            {
                var selectedInline = TryGetSelectedInline( inline );
                if ( selectedInline.HasValue )
                {
                    selectedInlines.Add( selectedInline.Value );
                }

                if ( inline.EqualOrContainsAsChild( end.Parent ) )
                    break;

                if ( Config.MaxChosenWordsInlinesCount < selectedInlines.Count )
                    break;

                inline = inline.NextInline;
            }

            selectedInlines.RemoveFirstAndLastRunSpace();
            
            return ((0 < selectedInlines.Count) ? selectedInlines.Distinct().ToArray() : null);
        }

        private static int[] GetNumbers( Inline inline )
        {
            var run  = default(Run);
            var span = default(Span);
            
            if ( (run = inline as Run) != null )
            {
                if ( !run.IsRunSpace() )
                {
                    var result = new[] { (int) inline.Tag };
                    return (result);
                }
            }            
            else if ( (span = inline as Span) != null )
            {
                var result = from child in span.Inlines
                             let numbers = GetNumbers( child )
                             from n in numbers
                             select n;
                                 
                return (result.ToArray());
            }
            else
            {
                throw (new InvalidOperationException("Inline-element is not Run-element and not Span-element"));
            }

            return (new int[ 0 ]);
        }
        private static int[] GetNumbers( SelectedInline[] selectedInlines )
        {
            var numbers = new List< int >();
            foreach ( var selectedInline in selectedInlines )
            {
                numbers.AddRange( GetNumbers( selectedInline.Inline ) );
            }
            return (numbers.ToArray());
        }        

        private static TextPointer GetForwardBySpace( TextPointer pointer )
        {
            while ( true )
            {
                var tempPointer = pointer.GetPositionAtOffset( 1, LogicalDirection.Forward );
                if ( tempPointer == null )
                    return (null);

                //catch-z-content-end
                if ( tempPointer.Parent is Paragraph )
                    return (pointer);

                var tr = new TextRange( pointer, tempPointer );
                if ( !tr.Text.IsEmptyOrNull() && tr.Text.IsNullOrWhiteSpace() )
                {
                    var t = tempPointer.GetPositionAtOffset( -1, LogicalDirection.Backward );
                    return (t ?? tempPointer);
                }
                pointer = tempPointer;
            }
        }
        private static TextPointer GetBackwardBySpace( TextPointer pointer )
        {
            while ( true )
            {
                var tempPointer = pointer.GetPositionAtOffset( -1, LogicalDirection.Backward );
                if ( tempPointer == null )
                    return (null);

                //catch-z-content-start
                if ( tempPointer.Parent is Paragraph )
                    return (pointer);

                var tr = new TextRange( tempPointer, pointer );
                if ( !tr.Text.IsEmptyOrNull() && tr.Text.IsNullOrWhiteSpace() )
                {
                    var t = tempPointer.GetPositionAtOffset( 1, LogicalDirection.Forward );
                    return (t ?? tempPointer);
                }
                pointer = tempPointer;
            }
        }

        private void LoadFileContentModelIntoFlowDocumentView() => LoadFileContentModelIntoFlowDocumentView( _FileModel.CurrentPageSize, _FileModel.CurrentPageNumber );
        private void LoadFileContentModelIntoFlowDocumentView( int pageSize, int pageNumber )
        {
            var pageCount = default(int);
            var inlines = _FileModel.GetInlinesByPages( pageSize, pageNumber, out pageCount );

            #region [.pageNumberComboBox.]
            pageCountTextBlock.Text = pageCount.ToString();

            if ( pageCount != pageNumberComboBox.Items.Count )
            {
                pageNumberComboBox.SelectionChanged -= pageNumberComboBox_SelectionChanged;
                pageNumberComboBox.Items.Clear();
                for ( int i = 0; i < pageCount; i++ )
                {
                    pageNumberComboBox.Items.Add( new ComboBoxItem() { Content = i + 1, IsSelected = (i == pageNumber) } );
                }
                pageNumberComboBox.SelectionChanged += pageNumberComboBox_SelectionChanged;
            }
            #endregion

            #region [.flowDocument.]
            ClearFlowDocumentView();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add( paragraph );
            var paragraph_inlines = paragraph.Inlines;
            foreach ( var inline in inlines )
            {
                paragraph_inlines.Add( inline );
            }

            flowDocumentPageViewer.FirstPage();
            #endregion

            inlines = null;
            GC.Collect();
        }
        private void ClearFlowDocumentView() => flowDocument.Blocks.Clear();
        #endregion

        #region [.Load-File-Content tasks.]
        private CancellationTokenSource _CancellationTokenSource;
        private ProgressBanner          _ProgressBanner;

        private void StartLoadFileContentTask( string fileName )
        {
            #region [.create _CancellationTokenSource.]
            if ( _CancellationTokenSource != null )
            {
                _CancellationTokenSource.Dispose();
                _CancellationTokenSource = null;
            }
            _CancellationTokenSource = new CancellationTokenSource();
            #endregion

            ClearFlowDocumentView();

            ShowLoadFileProgressBanner( fileName );

            Task.Factory.StartNew( () => LoadFileContentTaskBody( fileName ), _CancellationTokenSource.Token );

            /*.ContinueWith( task => EndLoadFileContentTask()     , _CancellationTokenSource.Token, TaskContinuationOptions.NotOnCanceled , TaskScheduler.Default )
              .ContinueWith( task => CanceledLoadFileContentTask(), _CancellationTokenSource.Token, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default )*/;
        }
        private void LoadFileContentTaskBody( string fileName )
        {
            var result = _FileModel.LoadFile( fileName, _ProgressBanner.SetProgressTextOverDispatcher, _CancellationTokenSource );
            if ( result )
            {
                EndLoadFileContentTask();
            }
            else
            {
                CanceledLoadFileContentTask();
            }
        }

        private void EndLoadFileContentTask() => Dispatcher.BeginInvoke( DispatcherPriority.DataBind, new Action( EndLoadFileContentTaskBody ) ).Wait();
        private void EndLoadFileContentTaskBody()
        {
            try
            {
                LoadFileContentModelIntoFlowDocumentView();

                this.Title = $"{App.TITLE}: '{_FileModel.FileName}'"; //selectedFilenameTextBlock.Text = _FileModel.FileName;
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error );
            }
            finally
            {
                RemoveProgressBanner();
            }
        }

        private void CanceledLoadFileContentTask() => Dispatcher.BeginInvoke( DispatcherPriority.DataBind, new Action( CanceledLoadFileContentTaskBody ) ).Wait();
        private void CanceledLoadFileContentTaskBody()
        {
            RemoveProgressBanner();

            this.Title = App.TITLE; //selectedFilenameTextBlock.Text = "NONE";
        }

        private void ShowLoadFileProgressBanner( string fileName )
        {
            _ProgressBanner = new ProgressBanner( fileName );
            _ProgressBanner.Cancelcallback = () => _CancellationTokenSource.Cancel();
            _ProgressBanner.SetValue( Grid.RowProperty, 1 );
            this.rootGrid.Children.Insert( 0, _ProgressBanner );

            this.Title = App.TITLE + ": loading...." + fileName.InSingleQuotes();
            toolBarPanel            .Visibility = Visibility.Hidden; //.IsHitTestVisible = false; //
            flowDocumentPageViewer  .IsHitTestVisible = false;
            flowDocumentScrollViewer.IsHitTestVisible = false;
        }
        private void RemoveProgressBanner()
        {
            this.rootGrid.Children.Remove( _ProgressBanner );
            _ProgressBanner = null;

            toolBarPanel            .Visibility = Visibility.Visible; //.IsHitTestVisible = true; //
            flowDocumentPageViewer  .IsHitTestVisible = true;
            flowDocumentScrollViewer.IsHitTestVisible = true;
        }
        #endregion

        #region [.turn-the-pages.]
        private WaitBanner _WaitBanner;

        private void ShowWaitBanner( string message = null )
        {
            if ( _WaitBanner == null )
            {
                _WaitBanner = new WaitBanner();
            }
            _WaitBanner.Text = message;
            _WaitBanner.SetValue( Grid.RowProperty, 1 );
            this.rootGrid.Children.Insert( 0, _WaitBanner );

            toolBarPanel.Visibility = Visibility.Hidden; //.IsHitTestVisible = false; //
            flowDocumentPageViewer  .IsHitTestVisible = false;
            flowDocumentScrollViewer.IsHitTestVisible = false;
        }
        private void RemoveWaitBanner()
        {
            this.rootGrid.Children.Remove( _WaitBanner );
            //_WaitBanner = null;

            toolBarPanel            .Visibility = Visibility.Visible; //.IsHitTestVisible = true; //
            flowDocumentPageViewer  .IsHitTestVisible = true;
            flowDocumentScrollViewer.IsHitTestVisible = true;
        }

        private void PageNumberChange( int pageNumber )
        {
            ClearFlowDocumentView();
            ShowWaitBanner( $"...page#: {pageNumber + 1}..." );
            Dispatcher.BeginInvoke( new Action< int, int >( PageFlipFlopBody ), DispatcherPriority.ApplicationIdle, _FileModel.CurrentPageSize, pageNumber);
        }
        private void PageSizeChange( int pageSize )
        {
            ClearFlowDocumentView();
            ShowWaitBanner( $"...sentences-on-page: {pageSize}..." );
            Dispatcher.BeginInvoke( new Action< int, int >( PageFlipFlopBody ), DispatcherPriority.ApplicationIdle, pageSize, _FileModel.CurrentPageNumber );
        }
        private void RefreshPage()
        {
            ClearFlowDocumentView();
            ShowWaitBanner( "...updating..." );
            Dispatcher.BeginInvoke( new Action< int, int >( PageFlipFlopBody ), DispatcherPriority.ApplicationIdle, _FileModel.CurrentPageSize, _FileModel.CurrentPageNumber );
        }

        private void PageFlipFlopBody( int pageSize, int pageNumber )
        {
            try
            {
                LoadFileContentModelIntoFlowDocumentView( pageSize, pageNumber );
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error );
            }
            finally
            {
                RemoveWaitBanner();
            }
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal struct SelectedInline
    {
        private static readonly TextDecoration _TextDecoration1 = new TextDecoration() { Location = TextDecorationLocation.OverLine , PenOffset = -0.1, Pen = new Pen() { Brush = Brushes.Black } };
        private static readonly TextDecoration _TextDecoration2 = new TextDecoration() { Location = TextDecorationLocation.Underline, PenOffset = 1   , Pen = new Pen() { Brush = Brushes.Black } };

        public SelectedInline( Inline inline )
        {
            Inline      = inline;
            _Foreground = inline.Foreground;
        }

        public Inline Inline { get; }
        private Brush _Foreground;

        public void SetFontStyleAndForeground()
        {
            if ( (Config.ChosenWordsSelectionMethod & Config.ChosenWordsSelectionMethodEnum.ItalicFontStyle) == Config.ChosenWordsSelectionMethodEnum.ItalicFontStyle )
            {
                Inline.FontStyle = FontStyles.Italic;
            }
            if ( (Config.ChosenWordsSelectionMethod & Config.ChosenWordsSelectionMethodEnum.OverAndUnderlineLine) == Config.ChosenWordsSelectionMethodEnum.OverAndUnderlineLine )
            {
                Inline.TextDecorations.Add( _TextDecoration1 );
                Inline.TextDecorations.Add( _TextDecoration2 );                
            }
            Inline.Foreground = Brushes.Gray;
        }
        public void ResetFontStyleAndForeground()
        {
            if ( (Config.ChosenWordsSelectionMethod & Config.ChosenWordsSelectionMethodEnum.ItalicFontStyle) == Config.ChosenWordsSelectionMethodEnum.ItalicFontStyle )
            {
                Inline.FontStyle = FontStyles.Normal;
            }
            if ( (Config.ChosenWordsSelectionMethod & Config.ChosenWordsSelectionMethodEnum.OverAndUnderlineLine) == Config.ChosenWordsSelectionMethodEnum.OverAndUnderlineLine )
            {
                Inline.TextDecorations.Remove( _TextDecoration1 );
                Inline.TextDecorations.Remove( _TextDecoration2 );                
            }

            Inline.Foreground = _Foreground;
        }

        public override string ToString()
        {            
            if ( Inline is Run run )
            {
                return (Inline.GetType().Name + " - " + run.Text.InSingleQuotes() );
            }            
            else if ( Inline is Span span )
            {
                var sb = new StringBuilder();
                foreach ( var child in span.Inlines )
                {
                    sb.Append( ((Run) child).Text );
                }
                return (Inline.GetType().Name + " - " + sb.ToString().InSingleQuotes() + " (" + span.Inlines.Count + ")");
            }
            return (Inline.GetType().Name + " - !!!");
        }
    }
}

