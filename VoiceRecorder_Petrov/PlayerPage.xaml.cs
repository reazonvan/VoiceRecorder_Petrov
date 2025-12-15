using Plugin.AudioRecorder;
using VoiceRecorder_Petrov.Models;

namespace VoiceRecorder_Petrov
{
    // Страница плеера для воспроизведения записи
    public partial class PlayerPage : ContentPage
    {
        private readonly AudioRecording _recording;
        private readonly AudioPlayer _player;
        private System.Threading.Timer? _timer;
        private bool _isPlaying = false;
        private bool _isDragging = false;

        public PlayerPage(AudioRecording recording)
        {
            InitializeComponent();
            
            _recording = recording;
            _player = new AudioPlayer();
            
            // Устанавливаем информацию о записи
            TitleLabel.Text = _recording.Title;
            DateLabel.Text = _recording.FormattedDate;
            TotalTimeLabel.Text = _recording.FormattedDuration;
            PositionSlider.Maximum = _recording.DurationSeconds;
        }

        // Воспроизведение/Пауза
        private void OnPlayPauseTapped(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Ставим на паузу
                PausePlayback();
            }
            else
            {
                // Начинаем воспроизведение
                StartPlayback();
            }
        }

        // Начинаем воспроизведение
        private void StartPlayback()
        {
            try
            {
                _player.Play(_recording.FilePath);
                _isPlaying = true;
                
                // Меняем иконку на паузу
                PlayIcon.IsVisible = false;
                PauseIcon.IsVisible = true;
                
                // Запускаем таймер обновления позиции
                _timer = new System.Threading.Timer(_ =>
                {
                    if (!_isDragging)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            UpdatePosition();
                        });
                    }
                }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Ошибка", $"Не удалось воспроизвести: {ex.Message}", "OK");
            }
        }

        // Пауза
        private void PausePlayback()
        {
            try
            {
                // Останавливаем таймер
                _timer?.Dispose();
                _timer = null;
                
                _isPlaying = false;
                
                // Меняем иконку на play
                PlayIcon.IsVisible = true;
                PauseIcon.IsVisible = false;
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Ошибка", $"Ошибка паузы: {ex.Message}", "OK");
            }
        }

        // Обновление позиции воспроизведения
        private void UpdatePosition()
        {
            // Симуляция позиции (так как AudioPlayer не предоставляет текущую позицию)
            // В реальном приложении нужен плеер с поддержкой позиции
            if (PositionSlider.Value < _recording.DurationSeconds)
            {
                PositionSlider.Value += 0.1;
                
                var currentSeconds = (int)PositionSlider.Value;
                var minutes = currentSeconds / 60;
                var seconds = currentSeconds % 60;
                CurrentTimeLabel.Text = $"{minutes:00}:{seconds:00}";
            }
            else
            {
                // Воспроизведение закончилось
                PausePlayback();
                PositionSlider.Value = 0;
                CurrentTimeLabel.Text = "00:00";
            }
        }

        // Перемотка назад на 10 секунд
        private void OnBackwardClicked(object sender, EventArgs e)
        {
            var newPosition = Math.Max(0, PositionSlider.Value - 10);
            PositionSlider.Value = newPosition;
            
            // Если играет - перезапускаем с новой позиции
            if (_isPlaying)
            {
                RestartFromPosition(newPosition);
            }
        }

        // Перемотка вперед на 10 секунд
        private void OnForwardClicked(object sender, EventArgs e)
        {
            var newPosition = Math.Min(_recording.DurationSeconds, PositionSlider.Value + 10);
            PositionSlider.Value = newPosition;
            
            // Если играет - перезапускаем с новой позиции
            if (_isPlaying)
            {
                RestartFromPosition(newPosition);
            }
        }

        // Изменение позиции ползунком
        private void OnPositionChanged(object sender, ValueChangedEventArgs e)
        {
            if (_isDragging)
            {
                var currentSeconds = (int)e.NewValue;
                var minutes = currentSeconds / 60;
                var seconds = currentSeconds % 60;
                CurrentTimeLabel.Text = $"{minutes:00}:{seconds:00}";
            }
        }

        // Перезапуск с новой позиции
        private void RestartFromPosition(double position)
        {
            // Простая симуляция - перезапускаем аудио
            // AudioPlayer не поддерживает Seek, поэтому просто обновляем слайдер
            try
            {
                _player.Play(_recording.FilePath);
            }
            catch { }
        }

        // Закрытие плеера
        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // Останавливаем воспроизведение
            if (_isPlaying)
            {
                PausePlayback();
            }
            
            // Закрываем страницу
            await Navigation.PopModalAsync();
        }

        // Очистка при закрытии
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            _timer?.Dispose();
            _timer = null;
        }
    }
}

