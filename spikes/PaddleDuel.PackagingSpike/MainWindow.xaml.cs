using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using Windows.Graphics;

namespace PaddleDuel.PackagingSpike;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        PackageIdentity.Text = Package.Current.Id.FullName;
        RuntimeDetails.Text =
            $"{RuntimeInformation.FrameworkDescription} | {RuntimeInformation.ProcessArchitecture}";
        AppWindow.Resize(new SizeInt32(800, 360));
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
