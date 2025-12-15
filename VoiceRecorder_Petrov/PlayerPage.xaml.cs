using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;

namespace VoiceRecorder_Petrov
{
    // ========================================
    // СТРАНИЦА ПЛЕЕРА
    // Отвечает за воспроизведение голосовых записей
    // Показывает информацию и управление воспроизведением
    // ========================================
    public partial class PlayerPage : ContentPage
    {
        // --- ПОЛЯ ---
        
        private readonly AudioRecording _recording;     // Запись которую воспроизводим
        private readonly AudioService _audioService;    // Сервис для воспроизведения
        private System.Threading.Timer? _timer;         // Таймер для отсчета времени
        private int _elapsedSeconds = 0;                // Сколько секунд прошло
        private bool _isPlaying = false;                // Флаг: играет или нет

        // --- КОНСТРУКТОР ---
        
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

        // --- МЕТОДЫ УПРАВЛЕНИЯ ---
        
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
                // Шаг 1: Сбрасываем счетчик
                _elapsedSeconds = 0;
                
                // Шаг 2: Останавливаем предыдущее (если было)
                _audioService.StopPlayback();
                
                // Шаг 3: Запускаем воспроизведение файла
                _audioService.PlayRecording(_recording.FilePath);
                
                _isPlaying = true;
                
                // Шаг 4: Меняем кнопку
                PlayPauseButton.Text = "Остановить";
                PlayPauseButton.BackgroundColor = Color.FromArgb("#FF3B30");  // Красная
                
                // Шаг 5: Запускаем таймер (каждую секунду)
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
            // Шаг 1: Останавливаем таймер
            _timer?.Dispose();
            _timer = null;
            
            // Шаг 2: Останавливаем аудио
            _audioService.StopPlayback();
            
            _isPlaying = false;
            
            // Шаг 3: Меняем кнопку
            PlayPauseButton.Text = "Воспроизвести заново";
            PlayPauseButton.BackgroundColor = Color.FromArgb("#007AFF");  // Синяя
        }

        // --- ОБРАБОТЧИКИ СОБЫТИЙ ---
        
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
