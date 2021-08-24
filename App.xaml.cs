using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFLocalizeExtension.Engine;

namespace MajdataEdit
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
        }
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Source+" At:\n"+e.Exception.Message+"\n"+e.Exception.StackTrace, "发生错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
