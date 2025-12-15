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
            // Используем NavigationPage вместо Shell - убирает надпись "Home"
            var window = new Window(new NavigationPage(new MainPage())
            {
                BarBackground = new SolidColorBrush(Colors.Transparent),
                BarTextColor = Colors.Transparent
            });
            
            // Устанавливаем размер окна как у мобильного телефона
            window.Width = 400;
            window.Height = 800;
            
            // Фиксируем размер окна
            window.MinimumWidth = 400;
            window.MinimumHeight = 800;
            window.MaximumWidth = 400;
            window.MaximumHeight = 800;
            
            return window;
        }
    }
}