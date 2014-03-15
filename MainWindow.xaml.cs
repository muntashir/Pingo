using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace Pingo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isProcessRunning = false;

        HostList hostList = new HostList();
        Timers timers = new Timers();

        public MainWindow()
        {
            InitializeComponent();

            timers = new Timers(this);

            txtInput.Focus();
            txtInput.SelectAll();

            lsvOutput.ItemsSource = hostList.data.DefaultView;
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
                    String host = txtInput.Text;

                    Thread backgroundThread = new Thread(
                        new ThreadStart(() =>
                            {
                                isProcessRunning = true;

                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.Title = "Pingo - Working";
                                    ProgressBar1.Value = 100;
                                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                    TaskbarItemInfo.ProgressValue = 1;
                                }));

                                hostList.hosts.Add(new Host(host));

                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    hostList.UpdateData();
                                    this.Title = "Pingo - Idle";
                                    ProgressBar1.Value = 0;
                                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                                    TaskbarItemInfo.ProgressValue = 0;
                                }));

                                isProcessRunning = false;
                            }));

                    backgroundThread.Start();
                }
                else
                {
                    String[] delim = { "\r\n", " ", "'" };
                    String[] multiLineHost = txtInput.Text.Split(delim, StringSplitOptions.RemoveEmptyEntries);

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

                            foreach (String line in multiLineHost)
                            {
                                ProgressBar1.Dispatcher.BeginInvoke(
                                    new Action(() =>
                                    {
                                        ProgressBar1.Value = (i / double.Parse(multiLineHost.Count().ToString())) * 100.0;
                                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                        TaskbarItemInfo.ProgressValue = (i / double.Parse(multiLineHost.Count().ToString()));
                                    }));

                                i++;

                                hostList.hosts.Add(new Host(line));
                            }

                            ProgressBar1.Dispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                ProgressBar1.Value = 0;
                                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                                TaskbarItemInfo.ProgressValue = 0;
                                hostList.UpdateData();
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

        public void RefreshAll()
        {
            try
            {
                //Prevent multiple threads
                if (isProcessRunning)
                {
                    MessageBox.Show("A process is already running");
                    return;
                }

                //Set time elapsed to 0
                timers.timeElapsed = new TimeSpan(0, 0, 0);

                //Restart updateTimer
                if (timers.updateTimer.IsEnabled)
                {
                    timers.updateTimer.IsEnabled = false;
                    timers.updateTimer.IsEnabled = true;
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

                        foreach (Host host in hostList.hosts)
                        {
                            ProgressBar1.Dispatcher.BeginInvoke(
                                new Action(() =>
                                {
                                    ProgressBar1.Value = (i / double.Parse(hostList.hosts.Count().ToString())) * 100.0;
                                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                    TaskbarItemInfo.ProgressValue = (i / double.Parse(hostList.hosts.Count().ToString()));
                                }));

                            host.Ping();
                            i++;
                        }

                        ProgressBar1.Dispatcher.BeginInvoke(
                                                   new Action(() =>
                                                   {
                                                       ProgressBar1.Value = 0;
                                                       TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                                                       TaskbarItemInfo.ProgressValue = 0;
                                                       hostList.UpdateData();
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
            RefreshAll();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int minutes = int.Parse(txtInterval.Text);

            timers.updateTimer.Interval = new TimeSpan(0, minutes, 0);

            timers.timeElapsed = new TimeSpan(0, 0, 0);
        }

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            if (int.Parse(txtInterval.Text) + 5 < 60)
                txtInterval.Text = (timers.updateTimer.Interval.Minutes + 5).ToString();
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (int.Parse(txtInterval.Text) - 5 > 0)
                txtInterval.Text = (timers.updateTimer.Interval.Minutes - 5).ToString();
        }

        private void btnRefreshSelection_Click(object sender, RoutedEventArgs e)
        {
            int index = lsvOutput.SelectedIndex;

            try
            {
                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        isProcessRunning = true;

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Title = "Pingo - Working";
                            ProgressBar1.Value = 100;
                            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                            TaskbarItemInfo.ProgressValue = 1;
                        }));

                        if (index >= 0)
                            hostList.hosts[index].Ping();

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            hostList.UpdateData();
                            this.Title = "Pingo - Idle";
                            ProgressBar1.Value = 0;
                            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                            TaskbarItemInfo.ProgressValue = 0;
                        }));

                        isProcessRunning = false;
                    }));

                backgroundThread.Start();
            }
            catch
            {
                MessageBox.Show("Nothing selected");
            }
        }

        private void btnDeleteSelection_Click(object sender, RoutedEventArgs e)
        {

            if (isProcessRunning)
            {
                MessageBox.Show("A process is already running");
                return;
            }

            try
            {
                while (lsvOutput.SelectedItems.Count > 0)
                {
                    hostList.hosts.RemoveAt(lsvOutput.SelectedIndex);
                    hostList.UpdateData();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessRunning)
            {
                MessageBox.Show("A process is already running");
                return;
            }

            hostList.hosts.Clear();
            hostList.data.Rows.Clear();
        }

        private void btnTogglePolling_Click(object sender, RoutedEventArgs e)
        {
            if (timers.updateTimer.IsEnabled)
            {
                timers.updateTimer.IsEnabled = false;
                btnTogglePolling.Content = "Enable Polling";
                lblNextUpdate.Content = "Polling disabled";
                timers.timeToNextUpdateTimer.IsEnabled = false;
            }
            else
            {
                timers.updateTimer.IsEnabled = true;
                timers.timeToNextUpdateTimer.IsEnabled = true;
                btnTogglePolling.Content = "Disable Polling";
                timers.timeElapsed = new TimeSpan(0, 0, 0);
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessRunning)
            {
                MessageBox.Show("A process is already running");
                return;
            }

            hostList.Export();
        }

    }
}
