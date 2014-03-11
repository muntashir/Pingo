using System;
using System.Collections;
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

namespace Pingo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Host> hosts;

        public MainWindow()
        {
            InitializeComponent();

            hosts = new List<Host>();
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtInput.Text == "" || txtInput.Text == "Enter a hostname or IP")
                    throw new Exception("Invalid Input");
                else
                {
                    hosts.Add(new Host(txtInput.Text));

                    if (hosts[hosts.Count - 1].IsOnline() == true)
                        MessageBox.Show("y");
                    else
                        MessageBox.Show("n");
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
