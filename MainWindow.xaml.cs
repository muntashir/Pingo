using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
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
        DataTable data = new DataTable();
        bool isProcessRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            hosts = new List<Host>();

            txtInput.Focus();
            txtInput.SelectAll();

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 10, 0);
            dispatcherTimer.Start();

            data.Columns.Add("Hostname", typeof(string));
            data.Columns.Add("Status", typeof(string));
            data.Columns.Add("Timestamp", typeof(string));

            lsvOutput.ItemsSource = data.DefaultView;
        }

        private void dispatcherTimer_Tick(object sender, object e)
        {
            Refresh();
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            bool multiline = false;

            try
            {
                foreach (char c in txtInput.Text)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        multiline = true;
                        break;
                    }
                }

                if (txtInput.Text == "" || txtInput.Text == "Enter a hostname or IP")
                    throw new Exception("Invalid Input");
                else if (multiline == false)
                {
                    hosts.Add(new Host(txtInput.Text));
                    UpdateOutput();
                }
                else
                {
                    String[] delim = { "\r\n", " ", "'" };
                    String[] temp = txtInput.Text.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                    if (isProcessRunning)
                    {
                        MessageBox.Show("A process is already running");
                        return;
                    }

                    Thread backgroundThread = new Thread(
                        new ThreadStart(() =>
                        {
                            double i = 0.0;

                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.Title = "Pingo - Working";
                            }));

                            isProcessRunning = true;

                            foreach (String s in temp)
                            {
                                ProgressBar1.Dispatcher.BeginInvoke(
                                    new Action(() =>
                            {
                                ProgressBar1.Value = (i / double.Parse(temp.Count().ToString())) * 100.0;
                            }));

                                i++;

                                hosts.Add(new Host(s));
                            }

                            ProgressBar1.Dispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                ProgressBar1.Value = 0;
                                UpdateOutput();
                            }));

                            isProcessRunning = false;
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.Title = "Pingo - Idle";
                            }));
                        }));

                    backgroundThread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            txtInput.Focus();
            txtInput.SelectAll();
        }

        private void UpdateOutput()
        {
            data.Rows.Clear();

            foreach (Host host in hosts)
            {
                data.Rows.Add(host.ToString()[0], host.ToString()[1], host.ToString()[2]);
            }
        }

        private void Refresh()
        {
            try
            {
                if (isProcessRunning)
                {
                    MessageBox.Show("A process is already running");
                    return;
                }

                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        isProcessRunning = true;
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Title = "Pingo - Working";
                        }));

                        double i = 1.0;

                        foreach (Host host in hosts)
                        {
                            ProgressBar1.Dispatcher.BeginInvoke(
                                new Action(() =>
                                {
                                    ProgressBar1.Value = (i / double.Parse(hosts.Count().ToString())) * 100.0;
                                }));

                            host.Ping();
                            i++;
                        }

                        ProgressBar1.Dispatcher.BeginInvoke(
                                                   new Action(() =>
                                                   {
                                                       ProgressBar1.Value = 0;
                                                       UpdateOutput();
                                                   }));

                        isProcessRunning = false;
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Title = "Pingo - Idle";
                        }));
                    }));

                backgroundThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnRefreshAll_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int minutes = int.Parse(txtInterval.Text);

            dispatcherTimer.Interval = new TimeSpan(0, minutes, 0);
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            txtInterval.Text = (dispatcherTimer.Interval.Minutes + 5).ToString();
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (int.Parse(txtInterval.Text) - 5 > 0)
                txtInterval.Text = (dispatcherTimer.Interval.Minutes - 5).ToString();
        }

        private void btnRefreshSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                hosts[lsvOutput.SelectedIndex].Ping();

                UpdateOutput();
            }
            catch
            {
                MessageBox.Show("Nothing selected");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                hosts.RemoveAt(lsvOutput.SelectedIndex);

                UpdateOutput();
            }
            catch
            {
                MessageBox.Show("Nothing selected");
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            hosts.Clear();
            data.Rows.Clear();
        }

        private void btnEnable_Click(object sender, RoutedEventArgs e)
        {
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
                btnEnable.Content = "Enable Polling";
            }
            else
            {
                dispatcherTimer.Start();
                btnEnable.Content = "Disable Polling";
            }
        }

    }
}
