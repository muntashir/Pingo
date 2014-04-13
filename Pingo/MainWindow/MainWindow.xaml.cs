﻿using Pingo.Classes;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        protected ProgressBarUpdater progressBarUpdater;
        protected ListViewHelper listViewHelper;

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
            listViewHelper = new ListViewHelper(this, hostList);
            progressBarUpdater = new ProgressBarUpdater(progressBar, this);

            //Focus and highlight textbox
            txtInput.Focus();
            txtInput.SelectAll();

            //Set ListView source
            lsvOutput.ItemsSource = hostList.GetHostsAsDataTable().DefaultView;
        }

        //Header click event
        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (progressBar.Value > 0)
                return;

            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null && headerClicked.Role != GridViewColumnHeaderRole.Padding)
            {
                string header = headerClicked.Column.Header as string;

                if (headerClicked != listViewHelper.LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                    listViewHelper.Sort(header, direction);
                }
                else
                {
                    if (listViewHelper.LastSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                        listViewHelper.Sort(header, direction);
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate = null;
                        direction = ListSortDirection.Ascending;
                        headerClicked = null;
                        listViewHelper.LastHeaderClicked = null;

                        listViewHelper.View = CollectionViewSource.GetDefaultView(hostList.GetHostsAsDataTable().DefaultView);
                        listViewHelper.View.SortDescriptions.Clear();
                        listViewHelper.View.Refresh();

                        listViewHelper.CurrentSort = new SortDescription();
                    }
                }

                if (headerClicked != null)
                {
                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else if (direction == ListSortDirection.Descending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }
                }

                //Remove arrow from previously sorted header 
                if (listViewHelper.LastHeaderClicked != null && listViewHelper.LastHeaderClicked != headerClicked)
                {
                    listViewHelper.LastHeaderClicked.Column.HeaderTemplate = null;
                }

                listViewHelper.LastHeaderClicked = headerClicked;
                listViewHelper.LastSortDirection = direction;
            }
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
                                                duplicates += line + ", ";
                                            else
                                                hostList.AddHost(line);
                                        }

                                        if (duplicates != "")
                                        {
                                            duplicates = duplicates.Remove(duplicates.Length - 2);
                                            MessageBox.Show("Hosts " + duplicates + " already added", null, MessageBoxButton.OK, MessageBoxImage.Error);
                                        }

                                        //Pings hosts in parallel
                                        Parallel.ForEach(hostList.GetHostsAsList(), host =>
                                        {
                                            progressBar.Dispatcher.BeginInvoke(
                                                new Action(() =>
                                                {
                                                    progressBarUpdater.UpdateProgressBar(i, i / double.Parse(multiLineHost.Count().ToString()));
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
                                            progressBarUpdater.ResetProgressBar();

                                            hostList.UpdateData();
                                        }));

                                        this.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            listViewHelper.ClearSort();

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
                                                progressBarUpdater.UpdateProgressBar(i, double.Parse(hostList.GetHostsAsList().Count().ToString()));
                                            }));

                                        i++;

                                        host.Ping();
                                    });

                                    progressBar.Dispatcher.BeginInvoke(
                                        new Action(() =>
                                        {
                                            progressBarUpdater.ResetProgressBar();

                                            hostList.UpdateData();
                                        }));

                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        //Remove sort arrow
                                        listViewHelper.ClearSort();

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
                    Thread backgroundThread = new Thread(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                lock (threadLock)
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        listViewHelper.UpdateSelectedIndices();

                                        this.Title = "Pingo - Working";
                                        progressBar.Value = 0;
                                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                        TaskbarItemInfo.ProgressValue = 0;
                                    }));

                                    double progress = 1.0;

                                    Parallel.For(0, listViewHelper.GetSelectedIndices().Count(), i =>
                                        {
                                            progressBar.Dispatcher.BeginInvoke(new Action(() =>
                                            {
                                                progressBarUpdater.UpdateProgressBar(progress, double.Parse(listViewHelper.GetSelectedIndices().Count().ToString()));
                                            }));

                                            progress++;

                                            hostList.GetHostsAsList()[listViewHelper.GetSelectedIndices()[i]].Ping();
                                        });

                                    progressBar.Dispatcher.BeginInvoke(
                                        new Action(() =>
                                            {
                                                progressBarUpdater.ResetProgressBar();

                                                hostList.UpdateData();

                                                if (listViewHelper.CurrentSort.PropertyName != null)
                                                {
                                                    listViewHelper.View.SortDescriptions.Clear();
                                                    listViewHelper.View.SortDescriptions.Add(listViewHelper.CurrentSort);
                                                    listViewHelper.View.Refresh();
                                                }
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
                    Thread backgroundThread = new Thread(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                lock (threadLock)
                                {
                                    lsvOutput.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        listViewHelper.UpdateSelectedIndices();
                                        listViewHelper.View.SortDescriptions.Clear();
                                    }));

                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        listViewHelper.GetSelectedIndices().Sort();

                                        for (int i = 0; i < listViewHelper.GetSelectedIndices().Count(); i++)
                                        {
                                            hostList.GetHostsAsList().RemoveAt(listViewHelper.GetSelectedIndices()[i] - i);
                                        }

                                        hostList.UpdateData();
                                        if (listViewHelper.CurrentSort.PropertyName != null)
                                            listViewHelper.View.SortDescriptions.Add(listViewHelper.CurrentSort);
                                        listViewHelper.View.Refresh();
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

        private void btnDeleteAll_Click(object sender, RoutedEventArgs e)
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

                                        //Remove sort arrow
                                        if (listViewHelper.LastHeaderClicked != null)
                                            listViewHelper.LastHeaderClicked.Column.HeaderTemplate = null;

                                        listViewHelper.LastHeaderClicked = null;
                                        listViewHelper.LastSortDirection = ListSortDirection.Ascending;
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
                    listViewHelper.View.SortDescriptions.Clear();
                    string copy = hostList.GetHostsAsList()[lsvOutput.Items.IndexOf(lsvOutput.SelectedItems[0])].ToString()[0];
                    Clipboard.SetText(copy);
                    if (listViewHelper.CurrentSort.PropertyName != null)
                        listViewHelper.View.SortDescriptions.Add(listViewHelper.CurrentSort);
                    MessageBox.Show("Hostname " + copy + " copied to clipboard", null, MessageBoxButton.OK, MessageBoxImage.Information);
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
