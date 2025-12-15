using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;

namespace VoiceRecorder_Petrov
{
    // Простая страница плеера для воспроизведения
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
            
            // Автоматически запускаем воспроизведение
            StartPlayback();
        }

        // Нажатие на кнопку Play/Stop
        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Останавливаем воспроизведение
                StopPlayback();
            }
            else
            {
                // Запускаем воспроизведение (заново или первый раз)
                StartPlayback();
            }
        }

        // Запускаем воспроизведение
        private void StartPlayback()
        {
            try
            {
                // Сбрасываем таймер
                _elapsedSeconds = 0;
                
                // Останавливаем предыдущее
                _audioService.StopPlayback();
                
                // Запускаем воспроизведение
                _audioService.PlayRecording(_recording.FilePath);
                
                _isPlaying = true;
                
                // Меняем кнопку на "Остановить"
                PlayPauseButton.Text = "Остановить";
                PlayPauseButton.BackgroundColor = Color.FromArgb("#FF3B30");
                
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

        // Останавливаем воспроизведение
        private void StopPlayback()
        {
            // Останавливаем таймер
            _timer?.Dispose();
            _timer = null;
            
            // Останавливаем аудио
            _audioService.StopPlayback();
            
            _isPlaying = false;
            
            // Меняем кнопку на "Воспроизвести заново"
            PlayPauseButton.Text = "Воспроизвести заново";
            PlayPauseButton.BackgroundColor = Color.FromArgb("#007AFF");
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
