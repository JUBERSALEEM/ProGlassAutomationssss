using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoiceMainView : UserControl
    {
        public ProformaInvoiceMainView()
        {
            InitializeComponent();
        }

        private void StatusBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ProformaInvoiceModel invoice)
            {
                // Find the DataGridRow containing this border
                var row = FindParent<DataGridRow>(border);
                if (row != null)
                {
                    row.IsSelected = true;

                    // Find the DataGrid
                    var dataGrid = FindParent<DataGrid>(border);
                    if (dataGrid != null)
                    {
                        // Find the Status column index (should be 6 based on column order)
                        int statusColumnIndex = 6;

                        // Get the cell info for the Status column
                        var cellInfo = new DataGridCellInfo(row, dataGrid.Columns[statusColumnIndex]);

                        // Set as current cell and begin edit
                        dataGrid.CurrentCell = cellInfo;
                        dataGrid.BeginEdit();
                    }
                }
            }
            e.Handled = true;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}