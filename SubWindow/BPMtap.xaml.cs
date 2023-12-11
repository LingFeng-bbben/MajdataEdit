using System.Windows;

namespace MajdataEdit;

/// <summary>
///     BPMtap.xaml 的交互逻辑
/// </summary>
public partial class BPMtap : Window
{
    private List<double> bpms = new();
    private DateTime lastTime = DateTime.MinValue;

    public BPMtap()
    {
        InitializeComponent();
    }

    private void Tap_Button_Click(object sender, RoutedEventArgs e)
    {
        if (lastTime != DateTime.MinValue)
        {
            var delta = (DateTime.Now - lastTime).TotalSeconds;
            bpms.Add(60d / delta);
        }

        lastTime = DateTime.Now;
        double sum = 0;
        if (bpms.Count <= 0) return;
        if (bpms.Count > 20) bpms.RemoveAt(0);
        foreach (var bpm in bpms) sum += bpm;
        var avg = sum / bpms.Count;

        Tap_Button.Content = string.Format("{0:N1}", avg);
    }

    private void Reset_Button_Click(object sender, RoutedEventArgs e)
    {
        bpms = new List<double>();
        lastTime = DateTime.MinValue;
        Tap_Button.Content = "Tap";
    }
}