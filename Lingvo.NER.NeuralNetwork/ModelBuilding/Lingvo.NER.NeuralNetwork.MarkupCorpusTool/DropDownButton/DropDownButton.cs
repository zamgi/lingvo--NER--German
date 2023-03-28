using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Lingvo.NER.NeuralNetwork.MarkupCorpusTool.DropDownButton
{
    /// <summary>
    /// 
    /// </summary>
    public class DropDownButton : ToggleButton
    {
        #region [.Dependency Properties.]
        public static readonly DependencyProperty DropDownContextMenuProperty   = DependencyProperty.Register( "DropDownContextMenu", typeof(ContextMenu), typeof(DropDownButton), new UIPropertyMetadata( null ) );
        public static readonly DependencyProperty ImageProperty                 = DependencyProperty.Register( "Image", typeof(ImageSource), typeof(DropDownButton) );
        public static readonly DependencyProperty TextProperty                  = DependencyProperty.Register( "Text", typeof(string), typeof(DropDownButton) );
        public static readonly DependencyProperty TargetProperty                = DependencyProperty.Register( "Target", typeof(UIElement), typeof(DropDownButton) );
        public static readonly DependencyProperty DropDownButtonCommandProperty = DependencyProperty.Register( "DropDownButtonCommand", typeof(ICommand), typeof(DropDownButton), new FrameworkPropertyMetadata(null) );
        #endregion

        #region [.ctor().]
        public DropDownButton()
        {
            // Bind the ToogleButton.IsChecked property to the drop-down's IsOpen property 
            var binding = new Binding( "DropDownContextMenu.IsOpen" ) { Source = this };
            SetBinding( IsCheckedProperty, binding );
        }
        #endregion

        #region [.props.]
        public ContextMenu DropDownContextMenu
        {
            get => GetValue( DropDownContextMenuProperty ) as ContextMenu;
            set => SetValue( DropDownContextMenuProperty, value );
        }
        public ImageSource Image
        {
            get => GetValue( ImageProperty ) as ImageSource;
            set => SetValue( ImageProperty, value );
        }
        public string Text
        {
            get => GetValue( TextProperty ) as string;
            set => SetValue( TextProperty, value );
        }
        public UIElement Target
        {
            get => GetValue( TargetProperty ) as UIElement;
            set => SetValue( TargetProperty, value );
        }
        public ICommand DropDownButtonCommand
        {
            get => GetValue( DropDownButtonCommandProperty ) as ICommand;
            set => SetValue( DropDownButtonCommandProperty, value );
        }
        #endregion

        #region [.overrides.]
        protected override void OnPropertyChanged( DependencyPropertyChangedEventArgs e )
        {
            base.OnPropertyChanged( e );

            if ( e.Property == DropDownButtonCommandProperty )
                Command = DropDownButtonCommand;
        }

        protected override void OnClick()
        {
            if ( DropDownContextMenu == null ) return;

            if ( DropDownButtonCommand != null ) DropDownButtonCommand.Execute( null );

            // If there is a drop-down assigned to this button, then position and display it 
            DropDownContextMenu.PlacementTarget = this;
            DropDownContextMenu.Placement = PlacementMode.Bottom;
            DropDownContextMenu.IsOpen = !DropDownContextMenu.IsOpen;
        }
        #endregion
    }
}