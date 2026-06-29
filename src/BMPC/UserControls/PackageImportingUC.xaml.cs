using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BMPC.UserControls
{
    /// <summary>
    /// Interaction logic for PackageImportingUC.xaml
    /// </summary>
    public partial class PackageImportingUC : UserControl
    {
        public PackageImportingUC()
        {
            InitializeComponent();
        }

        public void UpdateStatus(string status)
        {
            this.Dispatcher.Invoke(() =>
            {
                LblStatus5.Content = LblStatus4.Content;
                LblStatus4.Content = LblStatus3.Content;
                LblStatus3.Content = LblStatus2.Content;
                LblStatus2.Content = LblStatus1.Content;
                LblStatus1.Content = status;
            });
        }
    }
}
