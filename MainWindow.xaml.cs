using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 7;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth - 7;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            timers = new Timers(this);

            txtInput.Focus();
            txtInput.SelectAll();

            //Set ListView source
            lsvOutput.ItemsSource = hostList.data.DefaultView;
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            bool wasTimerEnabled = false;

            if (timers.updateTimer.IsEnabled == true)
                wasTimerEnabled = true;

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
                    if (isProcessRunning)
                    {
                        return;
                    }

                    String line = txtInput.Text;

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

                                    timers.DisableTimers();
                                }));

                                bool duplicate = false; 

                                foreach (Host host in hostList.hosts)
                                {
                                    if (host.ToString()[0] == line)
                                    {
                                        MessageBox.Show(line + " already added", null, MessageBoxButton.OK, MessageBoxImage.Error);
                                        duplicate = true;
                                    }
                                }

                                if (!duplicate)
                                    hostList.AddHost(line);

                                hostList.hosts[hostList.hosts.Count() - 1].Ping();

                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    hostList.UpdateData();
                                    this.Title = "Pingo - Idle";
                                    progressBar.Value = 0;
                                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                                    TaskbarItemInfo.ProgressValue = 0;

                                    if (wasTimerEnabled)
                                        timers.EnableTimers();
                                    else
                                        lblNextUpdate.Content = "Polling disabled";
                                }));

                                isProcessRunning = false;
                            }));

                    backgroundThread.IsBackground = true;
                    backgroundThread.Start();
                }
                else
                {
                    String[] delim = { "\r\n", " ", "'" };
                    String[] multiLineHost = txtInput.Text.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                    if (isProcessRunning)
                    {
                        return;
                    }

                    Thread backgroundThread = new Thread(
                        new ThreadStart(() =>
                        {
                            double i = 0.0;

                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.Title = "Pingo - Working";

                                timers.DisableTimers();
                            }));

                            isProcessRunning = true;

                            foreach (string line in multiLineHost)
                            {
                                bool duplicate = false; ;

                                foreach (Host host in hostList.hosts)
                                {
                                    if (host.ToString()[0] == line)
                                    {
                                        MessageBox.Show(line + " already added", null, MessageBoxButton.OK, MessageBoxImage.Error);
                                        duplicate = true;
                                    }
                                }

                                if (!duplicate)
                                    hostList.AddHost(line);
                            }

                            Parallel.ForEach(hostList.hosts, host =>
                            {
                                progressBar.Dispatcher.BeginInvoke(
                                    new Action(() =>
                                    {
                                        progressBar.Value = (i / double.Parse(multiLineHost.Count().ToString())) * 100.0;
                                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                        TaskbarItemInfo.ProgressValue = (i / double.Parse(multiLineHost.Count().ToString()));
                                    }));

                                if (host.IsNotPinged())
                                {
                                    i++;
                                    host.Ping();
                                }
                            });

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

                                if (wasTimerEnabled)
                                    timers.EnableTimers();
                                else
                                    lblNextUpdate.Content = "Polling disabled";
                            }));
                        }));

                    backgroundThread.IsBackground = true;
                    backgroundThread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            txtInput.Focus();
            txtInput.SelectAll();
        }

        public void RefreshAll()
        {
            bool wasTimerEnabled = false;

            if (timers.updateTimer.IsEnabled == true)
                wasTimerEnabled = true;

            try
            {
                if (isProcessRunning)
                {
                    return;
                }

                timers.ResetTimeElapsed();

                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        isProcessRunning = true;

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Title = "Pingo - Working";

                            timers.DisableTimers();
                        }));

                        double i = 1.0;

                        Parallel.ForEach(hostList.hosts, host =>
                        {
                            progressBar.Dispatcher.BeginInvoke(
                                new Action(() =>
                                {
                                    progressBar.Value = (i / double.Parse(hostList.hosts.Count().ToString())) * 100.0;
                                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                    TaskbarItemInfo.ProgressValue = (i / double.Parse(hostList.hosts.Count().ToString()));
                                }));

                            i++;

                            host.Ping();
                        });

                        progressBar.Dispatcher.BeginInvoke(
                                                   new Action(() =>
                                                   {
                                                       progressBar.Value = 0;
                                                       TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                                                       TaskbarItemInfo.ProgressValue = 0;
                                                       hostList.UpdateData();
                                                   }));

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Title = "Pingo - Idle";

                            if (wasTimerEnabled)
                                timers.EnableTimers();
                            else
                                lblNextUpdate.Content = "Polling disabled";
                        }));

                        isProcessRunning = false;
                    }));

                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefreshAll_Click(object sender, RoutedEventArgs e)
        {
            RefreshAll();
        }

        private void txtInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Get new polling interval from txtInterval
            int minutes = int.Parse(txtInterval.Text);

            //Reset time elapsed and change updateTimer
            timers.SetUpdateInterval(minutes);
            timers.ResetTimeElapsed();

            //Avoid null error when window is first loaded
            if (lblNextUpdate != null)
                timers.RestartTimers();
        }

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessRunning || timers.updateTimer.IsEnabled == false)
            {
                return;
            }

            //Add 5 minutes to the polling interval
            if (int.Parse(txtInterval.Text) + 5 < 60)
                txtInterval.Text = (timers.updateTimer.Interval.Minutes + 5).ToString();
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessRunning || timers.updateTimer.IsEnabled == false)
            {
                return;
            }

            //Subtract 5 minutes from the polling interval
            if (int.Parse(txtInterval.Text) - 5 > 0)
                txtInterval.Text = (timers.updateTimer.Interval.Minutes - 5).ToString();
        }

        private void btnRefreshSelection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isProcessRunning)
                {
                    return;
                }

                List<int> selectedIndices = new List<int>();

                for (int i = 0; i < lsvOutput.SelectedItems.Count; i++)
                {
                    selectedIndices.Add(lsvOutput.Items.IndexOf(lsvOutput.SelectedItems[i]));
                }

                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        isProcessRunning = true;

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Title = "Pingo - Working";
                            progressBar.Value = 0;
                            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                            TaskbarItemInfo.ProgressValue = 0;
                        }));

                        double progress = 1.0;

                        Parallel.For(0, selectedIndices.Count(), i =>
                            {
                                progressBar.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    progressBar.Value = (progress / double.Parse(selectedIndices.Count().ToString())) * 100.0;
                                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                    TaskbarItemInfo.ProgressValue = (progress / double.Parse(selectedIndices.Count().ToString()));
                                }));

                                progress++;

                                hostList.hosts[selectedIndices[i]].Ping();
                            });

                        progressBar.Dispatcher.BeginInvoke(
                                                   new Action(() =>
                                                   {
                                                       progressBar.Value = 0;
                                                       TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                                                       TaskbarItemInfo.ProgressValue = 0;
                                                       hostList.UpdateData();
                                                   }));

                        isProcessRunning = false;
                    }));

                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteSelection_Click(object sender, RoutedEventArgs e)
        {
            //Prevent multiple threads accessing data
            if (isProcessRunning)
            {
                return;
            }

            try
            {
                List<int> selectedIndices = new List<int>();

                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        isProcessRunning = true;

                        lsvOutput.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                for (int i = 0; i < lsvOutput.SelectedItems.Count; i++)
                                {
                                    selectedIndices.Add(lsvOutput.Items.IndexOf(lsvOutput.SelectedItems[i]));
                                }
                            }));

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            selectedIndices.Sort();

                            for (int i = 0; i < selectedIndices.Count(); i++)
                            {
                                hostList.hosts.RemoveAt(selectedIndices[i] - i);
                            }

                            hostList.UpdateData();
                        }));

                        isProcessRunning = false;
                    }));

                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessRunning)
            {
                return;
            }

            hostList.hosts.Clear();
            hostList.data.Rows.Clear();
        }

        private void btnTogglePolling_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessRunning)
            {
                return;
            }

            //Turns polling on/off
            if (timers.updateTimer.IsEnabled)
            {
                timers.DisableTimers();
                btnTogglePolling.Content = "Enable Polling";
                lblNextUpdate.Content = "Polling disabled";
            }
            else
            {
                timers.EnableTimers();
                btnTogglePolling.Content = "Disable Polling";
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessRunning)
            {
                return;
            }

            hostList.Export();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    {
                        WindowState = WindowState.Maximized;
                        break;
                    }
                case WindowState.Maximized:
                    {
                        WindowState = WindowState.Normal;
                        break;
                    }
            }
        }

        private void lsvOutput_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lsvOutput.SelectedItems.Count > 0)
            {
                try
                {
                    Clipboard.SetText(hostList.hosts[lsvOutput.SelectedIndex].ToString()[0]);
                    MessageBox.Show("Hostname copied to clipboard", null, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
            }
        }
    }
}
