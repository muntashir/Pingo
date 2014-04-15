using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Pingo.Classes
{
    //Stores a List of all hosts and a ToString of each host as a DataTable
    public class HostList
    {
        //Objects
        Timers timers;
        MainWindow mainWindow;
        ProgressBarUpdater progressBarUpdater;
        ListViewHelper listViewHelper;

        //DataTable source for ListView
        protected DataTable data = new DataTable();

        //List to store all hosts
        protected List<Host> hosts;

        //Constructor
        public HostList(MainWindow mainWindow)
        {
            //Initialize hosts
            hosts = new List<Host>();

            //Initialize DataTable with columns
            data.Columns.Add("Hostname", typeof(string));
            data.Columns.Add("Status", typeof(string));
            data.Columns.Add("Timestamp", typeof(string));

            this.mainWindow = mainWindow;
            
            progressBarUpdater = new ProgressBarUpdater(mainWindow.progressBar, mainWindow);
        }

        public void InitializeTimers(Timers timers)
        {
            this.timers = timers;
        }

        public void InitializeListViewHelper(ListViewHelper listViewHelper)
        {
            this.listViewHelper = listViewHelper;
        }

        //Updates DataTable with contents of hosts
        public void UpdateData()
        {
            lock (Locks.updateLock)
            {
                //Clear old rows of DataTable
                data.Rows.Clear();

                //Create new rows with contents of hosts
                foreach (Host host in hosts)
                {
                    data.Rows.Add(host.ToString()[0], host.ToString()[1], host.ToString()[2]);
                }
            }
        }

        public List<Host> GetHostsAsList()
        {
            return hosts;
        }

        public DataTable GetHostsAsDataTable()
        {
            return data;
        }

        //Adds host
        public void AddHost(String hostname)
        {
            hosts.Add(new Host(hostname));
        }

        //Checks for duplicate host
        public bool IsDuplicate(string line)
        {
            foreach (Host host in hosts)
            {
                if (host.ToString()[0].ToUpper() == line.ToUpper())
                {
                    return true;
                }
            }

            return false;
        }

        //Pings all hosts or only hosts that have not been pinged if pingOnlyHosts is true. If pingOnlyHosts is true, numHostsEntered must specify how many hosts have not been pinged.
        public void PingHosts(bool pingOnlyNewHosts, double numHostsEntered)
        {
            lock (Locks.globalLock)
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
                                lock (Locks.threadLock)
                                {
                                    mainWindow.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        mainWindow.Title = "Pingo - Working";

                                        timers.DisableTimers();
                                    }));

                                    double i = 1.0;

                                    Parallel.ForEach(GetHostsAsList(), host =>
                                    {
                                        mainWindow.progressBar.Dispatcher.BeginInvoke(
                                            new Action(() =>
                                            {
                                                if (pingOnlyNewHosts == false)
                                                    progressBarUpdater.UpdateProgressBar(i, double.Parse(hosts.Count.ToString()));
                                                else
                                                    progressBarUpdater.UpdateProgressBar(i, numHostsEntered);
                                            }));

                                        if (host.IsNotPinged() && pingOnlyNewHosts == true)
                                        {
                                            i++;
                                            host.Ping();
                                        }
                                        else if (pingOnlyNewHosts == false)
                                        {
                                            i++;
                                            host.Ping();
                                        }
                                    });

                                    mainWindow.progressBar.Dispatcher.BeginInvoke(
                                        new Action(() =>
                                        {
                                            progressBarUpdater.ResetProgressBar();

                                            UpdateData();
                                        }));

                                    mainWindow.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        listViewHelper.ClearSort();

                                        mainWindow.Title = "Pingo - Idle";

                                        if (wasTimerEnabled)
                                            timers.EnableTimers();
                                        else
                                            mainWindow.lblNextUpdate.Content = "Polling disabled";
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
    }
}
