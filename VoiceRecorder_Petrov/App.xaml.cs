using Microsoft.Extensions.DependencyInjection;

namespace VoiceRecorder_Petrov
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Стартовая навигация приложения.
            // Используем NavigationPage, чтобы можно было открывать PlayerPage через Navigation.PushAsync().
            var window = new Window(new NavigationPage(new MainPage())
            {
                BarBackground = new SolidColorBrush(Colors.Transparent),
                BarTextColor = Colors.Transparent
            });
            
            // Windows: фиксируем размер окна под “телефонный” формат для демонстрации.
            window.Width = 400;
            window.Height = 800;
            
            window.MinimumWidth = 400;
            window.MinimumHeight = 800;
            window.MaximumWidth = 400;
            window.MaximumHeight = 800;
            
            return window;
        }
    }
}