using Microsoft.Extensions.Logging;

namespace VoiceRecorder_Petrov
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // ============================================
            // НАСТРОЙКИ ДЛЯ МАКСИМАЛЬНОЙ ПЛАВНОСТИ (120 FPS)
            // ============================================
            
            // Включаем аппаратное ускорение для всех платформ
            Microsoft.Maui.Handlers.ElementHandler.ElementMapper.AppendToMapping("EnableHardwareAcceleration", (handler, view) =>
            {
#if ANDROID
                // Android: включаем аппаратное ускорение для GPU рендеринга
                if (handler.PlatformView is Android.Views.View androidView)
                {
                    androidView.SetLayerType(Android.Views.LayerType.Hardware, null);
                }
#elif IOS || MACCATALYST
                // iOS: оптимизация отрисовки для высокой частоты
                if (handler.PlatformView is UIKit.UIView iosView)
                {
                    iosView.Layer.RasterizationScale = UIKit.UIScreen.MainScreen.Scale;
                    iosView.Layer.ShouldRasterize = false; // Для анимаций лучше выключить
                }
#endif
                // Windows автоматически использует максимальную частоту обновления
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
