using Microsoft.Extensions.DependencyInjection;
#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
#endif

namespace PromptManager
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            window.HandlerChanged += OnWindowHandlerChanged;
#endif

            return window;
        }

#if WINDOWS
        private static void OnWindowHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is not Microsoft.Maui.Controls.Window { Handler.PlatformView: Microsoft.UI.Xaml.Window nativeWindow })
            {
                return;
            }

            var windowHandle = WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }
#endif
    }
}
