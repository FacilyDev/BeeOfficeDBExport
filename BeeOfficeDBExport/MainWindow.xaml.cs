using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BeeOfficeDBExport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InputOutputFolder.Text = System.IO.Path.GetDi‌​rectoryName(System.Di‌​agnostics.Process.Get‌​CurrentProcess().Main‌​Module.FileName);
        }

        private void StartExport_Click(object sender, RoutedEventArgs e)
        {

            
            string conn = InputConnectionString.Text;
            string filter = InputTableRangeFilter.Text;
            string logsys = InputLogicalSystemNumber.Text;
            bool includeDeleted = SelectorDeleted.IsChecked.Value;
            string outFolder = InputOutputFolder.Text;

            
            switch (ValidateInputParameters(conn, logsys, outFolder))
                { 
                // no errors
                case 0:
                    BeeOfficeDB.ExportSelectedTables(conn, filter, logsys, includeDeleted, outFolder);
                    break;
                case 1:
                    MessageBox.Show("Connection String cannot be empty!");
                    Keyboard.Focus(InputConnectionString);
                    break;
                case 2:
                    MessageBox.Show("Logical system cannot be empty and must be an integer!");
                    Keyboard.Focus(InputLogicalSystemNumber);
                    break;
                case 3:
                    MessageBox.Show("Output folder cannot be empty!");
                    Keyboard.Focus(InputOutputFolder);
                    break;
                }



    }

        // checks if all mandatory parameters were entered (returns 0), else returns number of first unfilled parameter
        private int ValidateInputParameters(string conn, string logsys, string outFolder)
        {
            if (conn == null || conn == "")
            {
                return 1;
            }

            // logsys must be a numeric value
            int numLogsys;
            if (logsys == null || logsys == "" || int.TryParse(logsys, out numLogsys) == false)
            {
                return 2;
            }

                if (outFolder == null || outFolder == "")
            {
                return 3;
            }

            return 0;

        }

        public void UpdateProgress(string leftPane, string centerPane, int finishedPercentage)
        {
            lblStatusLeft.Text = leftPane;
            lblStatusCenter.Text = centerPane;
            prgExportStatus.Value = finishedPercentage;

        }

     
    }
}
