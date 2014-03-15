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
        //Flag for background thread
        protected bool isProcessRunning = false;

        //Objects
        protected HostList hostList = new HostList();
        protected Timers timers = new Timers();

        public MainWindow()
        {
            InitializeComponent();

            timers = new Timers(this);

            txtInput.Focus();
            txtInput.SelectAll();

            //Set ListView source
            lsvOutput.ItemsSource = hostList.data.DefaultView;
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            //Flag to store if multiple lines are entered
            bool multiline = false;

            try
            {
                //Checks if for multiple lines with multiple hosts
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

                                //Updates progress bars and sets window title
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.Title = "Pingo - Working";
                                    progressBar.Value = 100;
                                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                    TaskbarItemInfo.ProgressValue = 1;
                                }));

                                hostList.hosts.Add(new Host(host));

                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    hostList.UpdateData();
                                    this.Title = "Pingo - Idle";
                                    progressBar.Value = 0;
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
                                progressBar.Dispatcher.BeginInvoke(
                                    new Action(() =>
                                    {
                                        progressBar.Value = (i / double.Parse(multiLineHost.Count().ToString())) * 100.0;
                                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                        TaskbarItemInfo.ProgressValue = (i / double.Parse(multiLineHost.Count().ToString()));
                                    }));

                                i++;

                                hostList.hosts.Add(new Host(line));
                            }

                            progressBar.Dispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                progressBar.Value = 0;
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
                            progressBar.Dispatcher.BeginInvoke(
                                new Action(() =>
                                {
                                    progressBar.Value = (i / double.Parse(hostList.hosts.Count().ToString())) * 100.0;
                                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                    TaskbarItemInfo.ProgressValue = (i / double.Parse(hostList.hosts.Count().ToString()));
                                }));

                            host.Ping();
                            i++;
                        }

                        progressBar.Dispatcher.BeginInvoke(
                                                   new Action(() =>
                                                   {
                                                       progressBar.Value = 0;
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
            //Get new polling interval from txtInterval
            int minutes = int.Parse(txtInterval.Text);

            //Reset time elapsed and change updateTimer
            timers.updateTimer.Interval = new TimeSpan(0, minutes, 0);
            timers.timeElapsed = new TimeSpan(0, 0, 0);
        }

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            //Add 5 minutes to the polling interval
            if (int.Parse(txtInterval.Text) + 5 < 60)
                txtInterval.Text = (timers.updateTimer.Interval.Minutes + 5).ToString();
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            //Subtract 5 minutes from the polling interval
            if (int.Parse(txtInterval.Text) - 5 > 0)
                txtInterval.Text = (timers.updateTimer.Interval.Minutes - 5).ToString();
        }

        private void btnRefreshSelection_Click(object sender, RoutedEventArgs e)
        {
            //Get index of selected item
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
                            progressBar.Value = 100;
                            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                            TaskbarItemInfo.ProgressValue = 1;
                        }));

                        if (index >= 0)
                            hostList.hosts[index].Ping();

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            hostList.UpdateData();
                            this.Title = "Pingo - Idle";
                            progressBar.Value = 0;
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
            //Prevent multiple threads accessing data
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
            //Turns polling on/off
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
