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
                // Останавливаем предыдущее воспроизведение (если было)
                _audioService.StopPlayback();
                
                // Запускаем новое
                await _audioService.PlayRecording(_recording.FilePath);
                
                // Показываем иконку паузы
                PlayIcon.IsVisible = false;
                PauseIcon.IsVisible = true;
                
                // Запускаем таймер для обновления прогресса (каждые 200ms)
                _timer = new System.Threading.Timer(_ =>
                {
                    UpdateProgress();
                }, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200));
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
                try
                {
                    // Если пользователь двигает слайдер - не обновляем
                    if (_isUserSeeking) return;
                    
                    // Получаем текущую позицию из плеера
                    var currentPosition = _audioService.GetCurrentPosition();
                    var duration = _recording.DurationSeconds;
                    
                    // Обновляем UI
                    CurrentTimeLabel.Text = FormatTime(currentPosition);
                    SeekSlider.Value = currentPosition;
                    
                    // Обновляем прогресс бар
                    if (duration > 0)
                    {
                        ProgressBar.Progress = Math.Min(1.0, currentPosition / duration);
                    }
                    
                    // Если воспроизведение завершено - показываем Play
                    if (currentPosition >= duration && _audioService.IsPlaying())
                    {
                        PlayIcon.IsVisible = true;
                        PauseIcon.IsVisible = false;
                    }
                }
                catch { }
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
            try
            {
                if (_audioService.IsPlaying())
                {
                    // Ставим на паузу
                    _audioService.PausePlayback();
                    
                    // Показываем иконку плей
                    PlayIcon.IsVisible = true;
                    PauseIcon.IsVisible = false;
                }
                else
                {
                    // Возобновляем воспроизведение
                    _audioService.ResumePlayback();
                    
                    // Показываем иконку паузы
                    PlayIcon.IsVisible = false;
                    PauseIcon.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Ошибка", $"Ошибка управления: {ex.Message}", "OK");
            }
        }

        // Перемотка назад на 10 секунд
        private void OnBackwardClicked(object sender, EventArgs e)
        {
            try
            {
                var currentPos = _audioService.GetCurrentPosition();
                var newPosition = Math.Max(0, currentPos - 10);
                
                // Реально перематываем аудио
                _audioService.SeekTo(newPosition);
                
                // Обновляем UI сразу
                SeekSlider.Value = newPosition;
                CurrentTimeLabel.Text = FormatTime(newPosition);
            }
            catch { }
        }

        // Перемотка вперед на 10 секунд
        private void OnForwardClicked(object sender, EventArgs e)
        {
            try
            {
                var currentPos = _audioService.GetCurrentPosition();
                var newPosition = Math.Min(_recording.DurationSeconds, currentPos + 10);
                
                // Реально перематываем аудио
                _audioService.SeekTo(newPosition);
                
                // Обновляем UI сразу
                SeekSlider.Value = newPosition;
                CurrentTimeLabel.Text = FormatTime(newPosition);
            }
            catch { }
        }

        // Пользователь двигает слайдер
        private void OnSeekSliderValueChanged(object sender, ValueChangedEventArgs e)
        {
            try
            {
                // Ставим флаг что пользователь перематывает
                _isUserSeeking = true;
                
                // Реально перематываем аудио
                _audioService.SeekTo(e.NewValue);
                
                // Обновляем время
                CurrentTimeLabel.Text = FormatTime(e.NewValue);
                
                // Через 500ms снимаем флаг
                Task.Delay(500).ContinueWith(_ =>
                {
                    _isUserSeeking = false;
                });
            }
            catch { }
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
