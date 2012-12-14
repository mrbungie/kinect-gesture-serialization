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
using System.Windows.Shapes;

namespace KinectGestureLearner
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //Instancia nueva ventana tipo MainWindow (Aprender)
            var Window1 = new MainWindow();
            //La muestra
            Window1.Show();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //Instancia nueva ventana tipo SecondWindow (Leer)
            var Window2 = new SecondWindow();
            //La muestra
            Window2.Show();
        }
    }
}
