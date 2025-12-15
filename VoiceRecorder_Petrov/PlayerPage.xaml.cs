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
        private double _currentPosition = 0;

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
            CurrentTimeLabel.Text = "00:00";
        }

        // Воспроизведение/Пауза
        private void OnPlayPauseTapped(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Останавливаем
                StopPlayback();
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
                // Воспроизводим файл
                _player.Play(_recording.FilePath);
                _isPlaying = true;
                
                // Меняем иконку на паузу
                PlayIcon.IsVisible = false;
                PauseIcon.IsVisible = true;
                
                // Запускаем таймер обновления позиции (каждые 100 мс)
                _timer = new System.Threading.Timer(_ =>
                {
                    if (!_isDragging && _isPlaying)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            UpdatePosition();
                        });
                    }
                }, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Ошибка", $"Не удалось воспроизвести: {ex.Message}", "OK");
            }
        }

        // Обновление позиции
        private void UpdatePosition()
        {
            if (_currentPosition < _recording.DurationSeconds)
            {
                // Увеличиваем позицию на 0.1 секунды
                _currentPosition += 0.1;
                PositionSlider.Value = _currentPosition;
                
                // Обновляем время
                var currentSeconds = (int)_currentPosition;
                var minutes = currentSeconds / 60;
                var seconds = currentSeconds % 60;
                CurrentTimeLabel.Text = $"{minutes:00}:{seconds:00}";
            }
            else
            {
                // Воспроизведение закончилось - останавливаем
                StopPlayback();
                
                // Сбрасываем позицию
                _currentPosition = 0;
                PositionSlider.Value = 0;
                CurrentTimeLabel.Text = "00:00";
            }
        }

        // Останавливаем воспроизведение
        private void StopPlayback()
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
                DisplayAlertAsync("Ошибка", $"Ошибка остановки: {ex.Message}", "OK");
            }
        }

        // Начало перетаскивания ползунка
        private void OnSliderDragStarted(object sender, EventArgs e)
        {
            _isDragging = true;
        }

        // Конец перетаскивания ползунка
        private void OnSliderDragCompleted(object sender, EventArgs e)
        {
            _isDragging = false;
            
            // Обновляем позицию
            _currentPosition = PositionSlider.Value;
            
            var currentSeconds = (int)_currentPosition;
            var minutes = currentSeconds / 60;
            var seconds = currentSeconds % 60;
            CurrentTimeLabel.Text = $"{minutes:00}:{seconds:00}";
            
            // Если воспроизводится - перезапускаем
            if (_isPlaying)
            {
                StopPlayback();
                StartPlayback();
            }
        }

        // Закрытие плеера
        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // Останавливаем воспроизведение
            if (_isPlaying)
            {
                StopPlayback();
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
