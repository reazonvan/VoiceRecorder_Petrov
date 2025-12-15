using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;

namespace VoiceRecorder_Petrov
{
    // Страница воспроизведения выбранной записи (без сложного seek'а: старт/стоп и таймер).
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
            
            // Автоматически запускаем при открытии
            StartPlayback();
        }

        // Обработчик кнопки Play/Stop
        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Если играет - останавливаем
                StopPlayback();
            }
            else
            {
                // Если не играет - запускаем (заново)
                StartPlayback();
            }
        }

        // Запускаем воспроизведение
        private void StartPlayback()
        {
            try
            {
                _elapsedSeconds = 0;
                _audioService.StopPlayback();
                _audioService.PlayRecording(_recording.FilePath);
                
                _isPlaying = true;
                
                PlayPauseButton.Text = "Остановить";
                PlayPauseButton.BackgroundColor = Color.FromArgb("#FF3B30");  // Красная
                
                // Таймер нужен только для отображения времени и авто-стопа по длительности.
                _timer = new System.Threading.Timer(_ =>
                {
                    _elapsedSeconds++;
                    
                    // Обновляем UI
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var minutes = _elapsedSeconds / 60;
                        var seconds = _elapsedSeconds % 60;
                        TimerLabel.Text = $"{minutes:00}:{seconds:00}";
                        
                        // Если достигли конца - автоматически останавливаем
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
            _timer?.Dispose();
            _timer = null;
            
            _audioService.StopPlayback();
            
            _isPlaying = false;
            
            PlayPauseButton.Text = "Воспроизвести заново";
            PlayPauseButton.BackgroundColor = Color.FromArgb("#007AFF");  // Синяя
        }

        // Нажатие на крестик (закрыть плеер)
        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // Останавливаем воспроизведение
            _audioService.StopPlayback();
            _timer?.Dispose();
            _timer = null;
            
            // Закрываем страницу
            await Navigation.PopAsync();
        }

        // Когда страница закрывается - останавливаем все
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            _timer?.Dispose();
            _timer = null;
            _audioService.StopPlayback();
        }
    }
}
