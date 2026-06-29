using BMPC.Models;
using BMPC.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BMPC.UserControls
{
    /// <summary>
    /// Interaction logic for SongItemUserControl.xaml
    /// </summary>
    public partial class SongItemUserControl : UserControl
    {
        public ICommand EditCommand
        {
            get { return (ICommand)GetValue(EditCommandProperty); }
            set { SetValue(EditCommandProperty, value); }
        }

        public static readonly DependencyProperty EditCommandProperty =
            DependencyProperty.Register("EditCommand", typeof(ICommand), typeof(SongItemUserControl), new PropertyMetadata(null));

        public ICommand RemoveCommand
        {
            get { return (ICommand)GetValue(RemoveCommandProperty); }
            set { SetValue(RemoveCommandProperty, value); }
        }

        public static readonly DependencyProperty RemoveCommandProperty =
            DependencyProperty.Register("RemoveCommand", typeof(ICommand), typeof(SongItemUserControl), new PropertyMetadata(null));

        public SongItemUserControl()
        {
            InitializeComponent();
        }
    }
}
