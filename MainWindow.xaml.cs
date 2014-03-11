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
using System.Windows.Threading;

namespace Pingo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Host> hosts;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            hosts = new List<Host>();

            txtInput.Focus();
            txtInput.SelectAll();

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0,10,0);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, object e)
        {
            Refresh();
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
                    UpdateOutput();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            txtInput.Focus();
            txtInput.SelectAll();
        }

        private void UpdateOutput()
        {
            lsBox.Items.Clear();

            foreach (Host host in hosts)
            {
                lsBox.Items.Add(host.ToString()[0] + "\t" + host.ToString()[1]);
            }
        }

        private void Refresh()
        {
            foreach (Host host in hosts)
            {
                host.Ping();
            }

            UpdateOutput();
        }

        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void btnRefreshAll_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int minutes = int.Parse(txtInterval.Text);

            dispatcherTimer.Stop();
            dispatcherTimer.Interval = new TimeSpan(0, minutes, 0);
            dispatcherTimer.Start();
        }
    }
}
