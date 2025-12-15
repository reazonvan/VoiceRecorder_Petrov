namespace VoiceRecorder_Petrov.Services
{
    // Простой кроссплатформенный аудиоплеер
    public class SimpleAudioPlayer : IDisposable
    {
        // Нативный плеер для Windows
#if WINDOWS
        private Windows.Media.Playback.MediaPlayer? _mediaPlayer;
#else
        private Plugin.AudioRecorder.AudioPlayer? _audioPlayer;
#endif

        private string? _currentFilePath;
        private System.Threading.Timer? _positionTimer;
        private double _durationSeconds = 0;
        
        // События
        public event EventHandler<double>? PositionChanged;
        public event EventHandler? PlaybackEnded;

        // Загружаем файл
        public void Load(string filePath, double durationSeconds)
        {
            _currentFilePath = filePath;
            _durationSeconds = durationSeconds;

#if WINDOWS
            // Создаем нативный Windows MediaPlayer
            _mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            _mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(
                new Uri(filePath));
            
            // Подписываемся на событие окончания
            _mediaPlayer.MediaEnded += (s, e) =>
            {
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
            };
#else
            _audioPlayer = new Plugin.AudioRecorder.AudioPlayer();
#endif
        }

        // Воспроизведение
        public void Play()
        {
#if WINDOWS
            _mediaPlayer?.Play();
            
            // Запускаем таймер обновления позиции
            _positionTimer = new System.Threading.Timer(_ =>
            {
                if (_mediaPlayer != null)
                {
                    var position = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
                    PositionChanged?.Invoke(this, position);
                    
                    // Проверяем окончание
                    if (position >= _durationSeconds)
                    {
                        Stop();
                        PlaybackEnded?.Invoke(this, EventArgs.Empty);
                    }
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
#else
            if (_audioPlayer != null && !string.IsNullOrEmpty(_currentFilePath))
            {
                _audioPlayer.Play(_currentFilePath);
            }
#endif
        }

        // Пауза (мгновенная!)
        public void Pause()
        {
#if WINDOWS
            _mediaPlayer?.Pause();
            _positionTimer?.Dispose();
            _positionTimer = null;
#else
            // На других платформах просто останавливаем
            _audioPlayer = null;
#endif
        }

        // Остановка
        public void Stop()
        {
#if WINDOWS
            _mediaPlayer?.Pause();
            _positionTimer?.Dispose();
            _positionTimer = null;
#else
            _audioPlayer = null;
#endif
        }

        // Перемотка
        public void SeekTo(double seconds)
        {
#if WINDOWS
            if (_mediaPlayer != null)
            {
                _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(seconds);
            }
#endif
        }

        // Текущая позиция
        public double GetPosition()
        {
#if WINDOWS
            return _mediaPlayer?.PlaybackSession.Position.TotalSeconds ?? 0;
#else
            return 0;
#endif
        }

        // Очистка
        public void Dispose()
        {
            _positionTimer?.Dispose();
            _positionTimer = null;

#if WINDOWS
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
#else
            _audioPlayer = null;
#endif
        }
    }
}

