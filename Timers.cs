using System;
using System.Windows.Threading;

namespace Pingo
{
    public class Timers
    {
        //Timer for automatic refreshes
        public DispatcherTimer updateTimer = new DispatcherTimer();

        //Timer to update textbox that displays time until next update
        public DispatcherTimer timeToNextUpdateTimer = new DispatcherTimer();

        //Time elapsed since last update
        public TimeSpan timeElapsed = new TimeSpan(0, 0, 0);

        //MainWindow object for access to controls
        protected MainWindow mainWindow;

        //Empty contructor to avoid null error
        public Timers()
        {
        }

        //Main constructor
        public Timers(MainWindow mainWindow)
        {
            //Initialize updateTimer and start it
            updateTimer.Tick += new EventHandler(updateTimer_Tick);
            updateTimer.Interval = new TimeSpan(0, 10, 0);
            updateTimer.Start();

            //Initialize timeToNextUpdateTimer and start it
            timeToNextUpdateTimer.Tick += new EventHandler(nextUpdate_Tick);
            timeToNextUpdateTimer.Interval = new TimeSpan(0, 0, 1);
            timeToNextUpdateTimer.Start();

            //Stores mainWindow object for access to window controls
            this.mainWindow = mainWindow;
        }

        //Updates time to next update textbox
        private void nextUpdate_Tick(object sender, object e)
        {
            mainWindow.lblNextUpdate.Content = "Next update in " + (updateTimer.Interval - timeElapsed);
            timeElapsed = timeElapsed.Add(new TimeSpan(0, 0, 1));
        }

        //Automatically update data and reset timeElapsed
        private void updateTimer_Tick(object sender, object e)
        {
            mainWindow.RefreshAll();
            timeElapsed = new TimeSpan(0, 0, 0);
        }
    }
}
