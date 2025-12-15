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

        // Конструктор: принимаем запись и сервис, настраиваем страницу
        public PlayerPage(AudioRecording recording, AudioService audioService)
        {
            InitializeComponent();
            
            _recording = recording;
            _audioService = audioService;
            
            // Отображаем информацию о записи (название, дата, размер, длительность)
            TitleLabel.Text = recording.Title;
            InfoLabel.Text = $"{recording.FormattedDate} • {recording.FormattedFileSize}";
            DurationLabel.Text = $"из {recording.FormattedDuration}";
            
            // Автоматически запускаем воспроизведение при открытии страницы
            StartPlayback();
        }

        // Обработчик нажатия на кнопку Play/Stop (переключатель)
        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Если сейчас играет - останавливаем
                StopPlayback();
            }
            else
            {
                // Если остановлено - запускаем заново
                StartPlayback();
            }
        }

        // Запускаем воспроизведение записи
        private void StartPlayback()
        {
            try
            {
                // Сбрасываем счётчик времени
                _elapsedSeconds = 0;
                
                // Останавливаем предыдущее воспроизведение (если было)
                _audioService.StopPlayback();
                
                // Запускаем воспроизведение файла через AudioService
                _audioService.PlayRecording(_recording.FilePath);
                
                _isPlaying = true;
                
                // Меняем кнопку на красную "Остановить"
                PlayPauseButton.Text = "Остановить";
                PlayPauseButton.BackgroundColor = Color.FromArgb("#FF3B30");
                
                // Запускаем таймер для отображения прогресса и авто-стопа в конце
                _timer = new System.Threading.Timer(_ =>
                {
                    _elapsedSeconds++;
                    
                    // Обновляем UI в главном потоке
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var minutes = _elapsedSeconds / 60;
                        var seconds = _elapsedSeconds % 60;
                        TimerLabel.Text = $"{minutes:00}:{seconds:00}";
                        
                        // Когда время воспроизведения достигло длительности записи - останавливаем
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
            
            // Возвращаем кнопку в исходное состояние (синяя "Воспроизвести заново")
            PlayPauseButton.Text = "Воспроизвести заново";
            PlayPauseButton.BackgroundColor = Color.FromArgb("#007AFF");
        }

        // Обработчик нажатия на крестик в углу (закрыть плеер)
        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // Останавливаем воспроизведение и чистим таймер
            _audioService.StopPlayback();
            _timer?.Dispose();
            _timer = null;
            
            // Закрываем страницу (возвращаемся на главную)
            await Navigation.PopAsync();
        }

        // Когда страница закрывается (override) - останавливаем всё, чтобы не играло в фоне
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            _timer?.Dispose();
            _timer = null;
            _audioService.StopPlayback();
        }
    }
}
