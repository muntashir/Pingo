using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Pingo.Classes
{
    public class ListViewHelper
    {
        protected List<int> selectedIndices = new List<int>();

        protected GridViewColumnHeader lastHeaderClicked = null;
        protected ListSortDirection lastDirection = ListSortDirection.Ascending;

        protected SortDescription sd = new SortDescription();

        protected ICollectionView dataView;

        protected MainWindow mainWindow;
        protected HostList hostList;

        public ListViewHelper(MainWindow mainWindow, HostList hostList)
        {
            this.mainWindow = mainWindow;
            this.hostList = hostList;
        }

        public void UpdateSelectedIndices()
        {
            selectedIndices.Clear();

            for (int i = 0; i < mainWindow.lsvOutput.SelectedItems.Count; i++)
            {
                dataView.SortDescriptions.Clear();
                selectedIndices.Add(mainWindow.lsvOutput.Items.IndexOf(mainWindow.lsvOutput.SelectedItems[i]));

                if (sd.PropertyName != null)
                    dataView.SortDescriptions.Add(sd);
            }
        }

        public List<int> GetSelectedIndices()
        { return selectedIndices; }

        public GridViewColumnHeader LastHeaderClicked
        {
            get
            { return lastHeaderClicked; }
            set
            { lastHeaderClicked = value; }
        }

        public ListSortDirection LastSortDirection
        {
            get
            { return lastDirection; }
            set
            { lastDirection = value; }
        }

        public ICollectionView View
        {
            get
            { return dataView; }
            set
            { dataView = value; }
        }

        public void Sort(string sortBy, ListSortDirection direction)
        {
            dataView = CollectionViewSource.GetDefaultView(hostList.GetHostsAsDataTable().DefaultView);

            sortBy = sortBy.TrimStart();

            if (sortBy == "Last Updated")
                sortBy = "Timestamp";

            dataView.SortDescriptions.Clear();
            sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        public SortDescription CurrentSort
        {
            get { return sd; }
            set { sd = value; }
        }

        public void ClearSort()
        {
            if (lastHeaderClicked != null)
                lastHeaderClicked.Column.HeaderTemplate = null;

            lastHeaderClicked = null;
            lastDirection = ListSortDirection.Ascending;

            dataView.SortDescriptions.Clear();
        }
    }
}
