using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ContinuousShotApp.Utilities.ExceptionMessage
{
    public class ExceptionMessage
    {
        public static void ShowException(Exception ex, string methodName)
        {
            MessageBox.Show($"An error occured: {ex.Message}_{methodName}");
        }
    }
}
