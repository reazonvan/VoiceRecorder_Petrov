using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;

namespace VoiceRecorder_Petrov
{
    // Простая страница плеера
    public partial class PlayerPage : ContentPage
    {
        private readonly AudioRecording _recording;
        private readonly AudioService _audioService;
        private System.Threading.Timer? _timer;
        private int _elapsedSeconds = 0;
        private bool _isPlaying = false;

        public PlayerPage(AudioRecording recording, AudioService audioService)
        {
            InitializeComponent();
            
            _recording = recording;
            _audioService = audioService;
            
            // Показываем информацию о записи
            TitleLabel.Text = recording.Title;
            InfoLabel.Text = $"{recording.FormattedDate} • {recording.FormattedFileSize}";
            DurationLabel.Text = $"из {recording.FormattedDuration}";
        }

        // Нажатие на кнопку Play/Pause
        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Ставим на паузу
                PausePlayback();
            }
            else
            {
                // Запускаем или возобновляем
                StartPlayback();
            }
        }

        // Запускаем воспроизведение
        private void StartPlayback()
        {
            try
            {
                // Останавливаем предыдущее
                _audioService.StopPlayback();
                
                // Запускаем воспроизведение
                _audioService.PlayRecording(_recording.FilePath);
                
                _isPlaying = true;
                
                // Меняем кнопку
                PlayPauseButton.Text = "Пауза";
                PlayPauseButton.BackgroundColor = Color.FromArgb("#FF9500");
                
                // Запускаем таймер (обновляется каждую секунду)
                _timer = new System.Threading.Timer(_ =>
                {
                    _elapsedSeconds++;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var minutes = _elapsedSeconds / 60;
                        var seconds = _elapsedSeconds % 60;
                        TimerLabel.Text = $"{minutes:00}:{seconds:00}";
                        
                        // Если достигли конца - останавливаем
                        if (_elapsedSeconds >= _recording.DurationSeconds)
                        {
                            StopPlayback();
                        }
                    });
                }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Ошибка", $"Не удалось воспроизвести: {ex.Message}", "OK");
            }
        }

        // Ставим на паузу
        private void PausePlayback()
        {
            // Останавливаем таймер
            _timer?.Dispose();
            _timer = null;
            
            // Останавливаем аудио
            _audioService.StopPlayback();
            
            _isPlaying = false;
            
            // Меняем кнопку
            PlayPauseButton.Text = "Продолжить";
            PlayPauseButton.BackgroundColor = Color.FromArgb("#34C759");
        }

        // Полная остановка
        private void StopPlayback()
        {
            // Останавливаем таймер
            _timer?.Dispose();
            _timer = null;
            
            // Останавливаем аудио
            _audioService.StopPlayback();
            
            _isPlaying = false;
            _elapsedSeconds = 0;
            
            // Меняем кнопку обратно
            PlayPauseButton.Text = "Воспроизвести заново";
            PlayPauseButton.BackgroundColor = Color.FromArgb("#007AFF");
            TimerLabel.Text = "00:00";
        }

        // Нажатие на крестик (закрыть плеер)
        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // Останавливаем воспроизведение
            _audioService.StopPlayback();
            
            // Останавливаем таймер
            _timer?.Dispose();
            _timer = null;
            
            // Закрываем страницу
            await Navigation.PopAsync();
        }

        // При закрытии страницы
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Останавливаем все
            _timer?.Dispose();
            _timer = null;
            _audioService.StopPlayback();
        }
    }
}

