// Views/DailyWorksView.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using ProGlassAutomation.ViewModels;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views
{
    public partial class DailyWorksView : UserControl
    {
        public DailyWorksView()
        {
            InitializeComponent();
            DataContext = new DailyWorksViewModel();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm)
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is DataRowView dataRow)
                {
                    var work = new DailyWork
                    {
                        Id = Convert.ToInt32(dataRow["Id"]),
                        Date = Convert.ToDateTime(dataRow["Date"]),
                        UpdateDate = Convert.ToDateTime(dataRow["UpdateDate"]),
                        Company = dataRow["Company"].ToString(),
                        PINumber = dataRow["PINumber"].ToString(),
                        CustomerReference = dataRow["CustomerReference"].ToString(),
                        TypeOfWork = dataRow["TypeOfWork"].ToString(),
                        ProductionStatus = dataRow["ProductionStatus"].ToString(),
                        DailyReportStatus = dataRow["DailyReportStatus"].ToString(),
                        Qty = Convert.ToInt32(dataRow["Qty"]),
                        SQM = Convert.ToDouble(dataRow["SQM"]),
                        Status = dataRow["Status"].ToString(),
                        Salesman = dataRow["Salesman"].ToString(),
                        Color = dataRow["Color"].ToString(),
                        Notes = dataRow["Notes"].ToString()
                    };
                    vm.SelectedWork = work;
                }

                if (vm.SelectedWork != null)
                {
                    vm.EditCommand.Execute(null);
                }
            }
        }
    }
}