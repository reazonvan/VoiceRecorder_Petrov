using CommunityToolkit.Maui.Views;

namespace VoiceRecorder_Petrov.Services
{
    // Простой аудиоплеер с поддержкой паузы и перемотки
    // Использует MediaElement из CommunityToolkit.Maui
    public class SimpleAudioPlayer : IDisposable
    {
        // Главный элемент для воспроизведения аудио
        private MediaElement? _mediaElement;
        
        // Путь к текущему файлу
        private string? _currentFilePath;

        public SimpleAudioPlayer()
        {
            // Создаем MediaElement - это специальный элемент для аудио/видео
            _mediaElement = new MediaElement
            {
                // Не запускать автоматически
                ShouldAutoPlay = false,
                
                // Не показывать встроенные кнопки управления
                ShouldShowPlaybackControls = false,
                
                // Делаем невидимым (нам нужна только логика, не UI)
                IsVisible = false
            };
        }

        // Начинаем воспроизведение файла
        public async Task Play(string filePath)
        {
            try
            {
                // Если плеер не создан - создаем
                if (_mediaElement == null)
                {
                    _mediaElement = new MediaElement
                    {
                        ShouldAutoPlay = false,
                        ShouldShowPlaybackControls = false,
                        IsVisible = false
                    };
                }

                // Если это новый файл - загружаем его
                if (_currentFilePath != filePath)
                {
                    _currentFilePath = filePath;
                    
                    // Загружаем файл в MediaElement
                    _mediaElement.Source = MediaSource.FromFile(filePath);
                }

                // Запускаем воспроизведение
                _mediaElement.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                throw;
            }
            
            await Task.CompletedTask;
        }

        // Ставим на паузу
        public void Pause()
        {
            _mediaElement?.Pause();
        }

        // Возобновляем воспроизведение
        public void Resume()
        {
            _mediaElement?.Play();
        }

        // Останавливаем полностью
        public void Stop()
        {
            if (_mediaElement != null)
            {
                _mediaElement.Stop();
                _mediaElement.Source = null;
            }
            _currentFilePath = null;
        }

        // Перематываем на указанную секунду
        public void SeekTo(double seconds)
        {
            if (_mediaElement != null)
            {
                // SeekTo принимает TimeSpan
                _mediaElement.SeekTo(TimeSpan.FromSeconds(seconds));
            }
        }

        // Получаем текущую позицию воспроизведения (в секундах)
        public double GetCurrentPosition()
        {
            if (_mediaElement != null)
            {
                return _mediaElement.Position.TotalSeconds;
            }
            return 0;
        }

        // Получаем общую длительность файла (в секундах)
        public double GetDuration()
        {
            if (_mediaElement != null)
            {
                return _mediaElement.Duration.TotalSeconds;
            }
            return 0;
        }

        // Проверяем играет ли сейчас
        public bool IsPlaying
        {
            get
            {
                if (_mediaElement != null)
                {
                    // Проверяем состояние через ToString() чтобы избежать ошибок
                    var state = _mediaElement.CurrentState.ToString();
                    return state == "Playing";
                }
                return false;
            }
        }

        // Очистка ресурсов
        public void Dispose()
        {
            Stop();
            _mediaElement = null;
        }
    }
}
