using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;

namespace VoiceRecorder_Petrov
{
    // Страница плеера для воспроизведения записей
    public partial class PlayerPage : ContentPage
    {
        private readonly AudioRecording _recording;
        private readonly AudioService _audioService;
        private System.Threading.Timer? _timer;
        private bool _isPlaying = false;
        private bool _isUserSeeking = false;

        public PlayerPage(AudioRecording recording, AudioService audioService)
        {
            InitializeComponent();
            
            _recording = recording;
            _audioService = audioService;
            
            // Устанавливаем информацию о записи
            TitleLabel.Text = recording.Title;
            TotalTimeLabel.Text = recording.FormattedDuration;
            SeekSlider.Maximum = recording.DurationSeconds;
            
            // Автоматически начинаем воспроизведение
            StartPlayback();
        }

        // Начинаем воспроизведение
        private async void StartPlayback()
        {
            try
            {
                await _audioService.PlayRecording(_recording.FilePath);
                _isPlaying = true;
                
                // Показываем иконку паузы
                PlayIcon.IsVisible = false;
                PauseIcon.IsVisible = true;
                
                // Запускаем таймер для обновления прогресса
                _timer = new System.Threading.Timer(_ =>
                {
                    UpdateProgress();
                }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось воспроизвести: {ex.Message}", "OK");
            }
        }

        // Обновляем прогресс
        private void UpdateProgress()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isUserSeeking) return;
                
                // Получаем текущую позицию из сервиса
                var currentPosition = _audioService.GetCurrentPosition();
                
                // Обновляем UI
                CurrentTimeLabel.Text = FormatTime(currentPosition);
                SeekSlider.Value = currentPosition;
                ProgressBar.Progress = currentPosition / _recording.DurationSeconds;
                
                // Если воспроизведение завершено
                if (currentPosition >= _recording.DurationSeconds && _isPlaying)
                {
                    StopPlayback();
                }
            });
        }

        // Форматируем время в MM:SS
        private string FormatTime(double totalSeconds)
        {
            var minutes = (int)totalSeconds / 60;
            var seconds = (int)totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        // Нажатие на кнопку Пауза/Плей
        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Ставим на паузу
                _audioService.PausePlayback();
                _isPlaying = false;
                
                // Показываем иконку плей
                PlayIcon.IsVisible = true;
                PauseIcon.IsVisible = false;
            }
            else
            {
                // Возобновляем
                _audioService.ResumePlayback();
                _isPlaying = true;
                
                // Показываем иконку паузы
                PlayIcon.IsVisible = false;
                PauseIcon.IsVisible = true;
            }
        }

        // Перемотка назад на 10 секунд
        private void OnBackwardClicked(object sender, EventArgs e)
        {
            var newPosition = Math.Max(0, SeekSlider.Value - 10);
            SeekSlider.Value = newPosition;
            _audioService.SeekTo(newPosition);
        }

        // Перемотка вперед на 10 секунд
        private void OnForwardClicked(object sender, EventArgs e)
        {
            var newPosition = Math.Min(_recording.DurationSeconds, SeekSlider.Value + 10);
            SeekSlider.Value = newPosition;
            _audioService.SeekTo(newPosition);
        }

        // Пользователь двигает слайдер
        private void OnSeekSliderValueChanged(object sender, ValueChangedEventArgs e)
        {
            // Если слайдер двигается программно - игнорируем
            if (Math.Abs(e.NewValue - _audioService.GetCurrentPosition()) < 1)
                return;
            
            // Пользователь перематывает вручную
            _isUserSeeking = true;
            _audioService.SeekTo(e.NewValue);
            
            // Через небольшую задержку снова включаем автообновление
            Task.Delay(500).ContinueWith(_ =>
            {
                _isUserSeeking = false;
            });
        }

        // Остановка воспроизведения
        private void StopPlayback()
        {
            _isPlaying = false;
            _audioService.StopPlayback();
            
            // Показываем иконку плей
            PlayIcon.IsVisible = true;
            PauseIcon.IsVisible = false;
            
            // Сбрасываем позицию
            SeekSlider.Value = 0;
            CurrentTimeLabel.Text = "00:00";
            ProgressBar.Progress = 0;
        }

        // Очистка при закрытии страницы
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Останавливаем таймер
            _timer?.Dispose();
            _timer = null;
            
            // Останавливаем воспроизведение
            _audioService.StopPlayback();
        }
    }
}

