using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using WPFLocalizeExtension.Engine;

namespace MajdataEdit;

/// <summary>
///     App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    public App()
    {
        LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
    }

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception.GetType() == typeof(COMException) &&
            e.Exception.Message.IndexOf("UCEERR_RENDERTHREADFAILURE") != -1)
        {
            // 需要开启软件渲染
            MessageBox.Show(MajdataEdit.MainWindow.GetLocalizedString("SoftRenderError"),
                MajdataEdit.MainWindow.GetLocalizedString("Error"));
            Shutdown(114);
            return;
        }

        MessageBox.Show(e.Exception.Source + " At:\n" + e.Exception.Message + "\n" + e.Exception.StackTrace, "发生错误",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}