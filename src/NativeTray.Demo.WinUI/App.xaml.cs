using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using NativeTray.Demo.WinUI;
using Windows.Storage;
using Windows.Storage.Streams;
using Application = Microsoft.UI.Xaml.Application;

namespace WinUIApp1;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host { get; }

    public static T GetService<T>() where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static Window MainWindow { get; } = new MainWindow();

    public App()
    {
        TrayIconManager.Start();
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            ///
        }).
        Build();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainWindow.AppWindow.Show();
    }
}

file static class ResourceHelper
{
    public static Stream GetStream(string uriString)
    {
        StorageFile file = StorageFile.GetFileFromApplicationUriAsync(new Uri(uriString)).AsTask().GetAwaiter().GetResult();
        IRandomAccessStream randomAccessStream = file.OpenAsync(FileAccessMode.Read).AsTask().GetAwaiter().GetResult();
        Stream stream = randomAccessStream.AsStreamForRead();
        return stream;
    }
}
