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
        //Objects
        protected HostList hostList = new HostList();
        protected Timers timers = new Timers();
        protected IO io;

        //Locks
        object globalLock = new object();
        object threadLock = new object();

        //Constructor
        public MainWindow()
        {
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 7;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth - 7;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Initialize objects
            io = new IO(hostList);
            timers = new Timers(this);

            //Focus and highlight textbox
            txtInput.Focus();
            txtInput.SelectAll();

            //Set ListView source
            lsvOutput.ItemsSource = hostList.GetHostsAsDataTable().DefaultView;
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            lock (globalLock)
            {
                bool wasTimerEnabled = false;

                if (timers.updateTimer.IsEnabled == true)
                    wasTimerEnabled = true;

                try
                {
                    if (txtInput.Text.Trim() == "" || txtInput.Text == "Enter a hostname or IP address")
                        throw new Exception("Invalid Input");
                    else
                    {
                        String[] delim = { "\r\n", " ", "'" };
                        String[] multiLineHost = txtInput.Text.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                        Thread backgroundThread = new Thread(
                            new ThreadStart(() =>
                            {
                                try
                                {
                                    lock (threadLock)
                                    {
                                        double i = 0.0;

                                        this.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            this.Title = "Pingo - Working";

                                            timers.DisableTimers();
                                        }));

                                        //Stores all duplicates
                                        String duplicates = "";

                                        //Checks if each host is a duplicate, then adds it to hostList if it is not
                                        foreach (string line in multiLineHost)
                                        {
                                            if (hostList.IsDuplicate(line))
                                                duplicates += line + " ";
                                            else
                                                hostList.AddHost(line);
                                        }

                                        if (duplicates != "")
                                            MessageBox.Show(duplicates + "already added", null, MessageBoxButton.OK, MessageBoxImage.Error);

                                        //Pings hosts in parallel
                                        Parallel.ForEach(hostList.GetHostsAsList(), host =>
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

                                        this.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            this.Title = "Pingo - Idle";

                                            if (wasTimerEnabled)
                                                timers.EnableTimers();
                                            else
                                                lblNextUpdate.Content = "Polling disabled";
                                        }));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                                }
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
        }

        public void RefreshAll()
        {
            lock (globalLock)
            {
                bool wasTimerEnabled = false;

                if (timers.updateTimer.IsEnabled == true)
                    wasTimerEnabled = true;

                try
                {
                    timers.ResetTimeElapsed();

                    Thread backgroundThread = new Thread(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                lock (threadLock)
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        this.Title = "Pingo - Working";

                                        timers.DisableTimers();
                                    }));

                                    double i = 1.0;

                                    Parallel.ForEach(hostList.GetHostsAsList(), host =>
                                    {
                                        progressBar.Dispatcher.BeginInvoke(
                                            new Action(() =>
                                            {
                                                progressBar.Value = (i / double.Parse(hostList.GetHostsAsList().Count().ToString())) * 100.0;
                                                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                                TaskbarItemInfo.ProgressValue = (i / double.Parse(hostList.GetHostsAsList().Count().ToString()));
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
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }));

                    backgroundThread.IsBackground = true;
                    backgroundThread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            lock (globalLock)
            {
                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        lock (threadLock)
                        {
                            try
                            {
                                if (timers.updateTimer.IsEnabled == false)
                                {
                                    return;
                                }

                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    //Add 5 minutes to the polling interval
                                    if (int.Parse(txtInterval.Text) + 5 < 60)
                                        txtInterval.Text = (timers.updateTimer.Interval.Minutes + 5).ToString();
                                }));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }));

                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            lock (globalLock)
            {
                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        lock (threadLock)
                        {
                            try
                            {
                                if (timers.updateTimer.IsEnabled == false)
                                {
                                    return;
                                }

                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    //Subtract 5 minutes from the polling interval
                                    if (int.Parse(txtInterval.Text) - 5 > 0)
                                        txtInterval.Text = (timers.updateTimer.Interval.Minutes - 5).ToString();
                                }));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }));

                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
        }

        private void btnRefreshSelection_Click(object sender, RoutedEventArgs e)
        {
            lock (globalLock)
            {
                try
                {
                    List<int> selectedIndices = new List<int>();

                    for (int i = 0; i < lsvOutput.SelectedItems.Count; i++)
                    {
                        selectedIndices.Add(lsvOutput.Items.IndexOf(lsvOutput.SelectedItems[i]));
                    }

                    Thread backgroundThread = new Thread(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                lock (threadLock)
                                {
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

                                            hostList.GetHostsAsList()[selectedIndices[i]].Ping();
                                        });

                                    progressBar.Dispatcher.BeginInvoke(
                                        new Action(() =>
                                            {
                                                progressBar.Value = 0;
                                                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                                                TaskbarItemInfo.ProgressValue = 0;
                                                hostList.UpdateData();
                                            }));
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }));

                    backgroundThread.IsBackground = true;
                    backgroundThread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnDeleteSelection_Click(object sender, RoutedEventArgs e)
        {
            lock (globalLock)
            {
                try
                {
                    List<int> selectedIndices = new List<int>();

                    Thread backgroundThread = new Thread(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                lock (threadLock)
                                {
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
                                            hostList.GetHostsAsList().RemoveAt(selectedIndices[i] - i);
                                        }

                                        hostList.UpdateData();
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }));

                    backgroundThread.IsBackground = true;
                    backgroundThread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            lock (globalLock)
            {
                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        lock (threadLock)
                        {
                            try
                            {
                                hostList.GetHostsAsList().Clear();

                                this.Dispatcher.BeginInvoke(
                                    new Action(() =>
                                    {
                                        hostList.UpdateData();
                                    }));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }));

                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
        }

        private void btnTogglePolling_Click(object sender, RoutedEventArgs e)
        {
            lock (globalLock)
            {
                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        lock (threadLock)
                        {
                            try
                            {
                                //Turns polling on/off
                                if (timers.updateTimer.IsEnabled)
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        timers.DisableTimers();
                                        btnTogglePolling.Content = "Enable Polling";
                                        lblNextUpdate.Content = "Polling disabled";
                                    }));
                                }
                                else
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            timers.EnableTimers();
                                            btnTogglePolling.Content = "Disable Polling";
                                        }));
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }

                    }));

                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            lock (globalLock)
            {
                Thread backgroundThread = new Thread(
                    new ThreadStart(() =>
                    {
                        lock (threadLock)
                        {
                            io.Export();
                        }
                    }));

                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            lsvOutput.SelectedItems.Clear();

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
                    Clipboard.SetText(hostList.GetHostsAsList()[lsvOutput.SelectedIndex].ToString()[0]);
                    MessageBox.Show("Hostname copied to clipboard", null, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (progressBar.Value != 0)
            {
                btnDeleteAll.IsEnabled = false;
                btnDeleteSelected.IsEnabled = false;
                btnEnter.IsEnabled = false;
                btnExport.IsEnabled = false;
                btnRefreshAll.IsEnabled = false;
                btnRefreshSelected.IsEnabled = false;
                btnPlus.IsEnabled = false;
                btnMinus.IsEnabled = false;
                btnTogglePolling.IsEnabled = false;
            }
            else
            {
                btnDeleteAll.IsEnabled = true;
                btnDeleteSelected.IsEnabled = true;
                btnEnter.IsEnabled = true;
                btnExport.IsEnabled = true;
                btnRefreshAll.IsEnabled = true;
                btnRefreshSelected.IsEnabled = true;
                btnPlus.IsEnabled = true;
                btnMinus.IsEnabled = true;
                btnTogglePolling.IsEnabled = true;
            }
        }
    }
}
