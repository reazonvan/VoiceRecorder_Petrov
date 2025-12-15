using Plugin.AudioRecorder;
using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;

namespace VoiceRecorder_Petrov
{
    // Главная и единственная страница диктофона
    public partial class MainPage : ContentPage
    {
        // Сервис для работы с записями
        private readonly AudioService _audioService;
        
        // Recorder для записи звука
        private AudioRecorderService? _recorder;
        
        // Таймер для отображения времени записи
        private System.Threading.Timer? _timer;
        
        // Счетчик секунд
        private int _seconds = 0;
        
        // Флаг - идет ли запись сейчас
        private bool _isRecording = false;
        
        // Флаг - запись на паузе
        private bool _isPaused = false;

        public MainPage()
        {
            InitializeComponent();
            
            // Создаем сервис
            _audioService = new AudioService();
            
            // Загружаем список записей
            LoadRecordings();
        }

        // Загружаем все записи из файла
        private async void LoadRecordings()
        {
            try
            {
                var recordings = await _audioService.GetAllRecordings();
                RecordingsCollection.ItemsSource = recordings;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось загрузить записи: {ex.Message}", "OK");
            }
        }

        // Нажатие на кнопку записи
        private async void OnRecordButtonClicked(object sender, EventArgs e)
        {
            if (!_isRecording)
            {
                // Начинаем запись
                await StartRecording();
            }
            else if (_isPaused)
            {
                // Возобновляем запись (снимаем с паузы)
                ResumeRecording();
            }
            else
            {
                // Ставим на паузу
                PauseRecording();
            }
        }

        // Начинаем запись
        private async Task StartRecording()
        {
            try
            {
                // Запрашиваем разрешение на микрофон
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Ошибка", "Нужно разрешение на микрофон", "OK");
                    return;
                }

                // Создаем recorder
                _recorder = new AudioRecorderService
                {
                    StopRecordingOnSilence = false,
                    StopRecordingAfterTimeout = false
                };

                // Начинаем запись
                await _recorder.StartRecording();

                // Меняем состояние
                _isRecording = true;
                _isPaused = false;
                _seconds = 0;
                
                // Обновляем UI
                RecordButton.Text = "Пауза";
                RecordButton.BackgroundColor = Color.FromArgb("#FF9500");
                StatusLabel.Text = "Идет запись...";
                
                // Показываем кнопку завершения
                FinishButton.IsVisible = true;
                
                // Запускаем таймер (обновляется каждую секунду)
                _timer = new System.Threading.Timer(_ =>
                {
                    _seconds++;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var minutes = _seconds / 60;
                        var secs = _seconds % 60;
                        TimerLabel.Text = $"{minutes:00}:{secs:00}";
                    });
                }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось начать запись: {ex.Message}", "OK");
            }
        }

        // Ставим запись на паузу
        private void PauseRecording()
        {
            // Останавливаем таймер
            _timer?.Dispose();
            _timer = null;
            
            // Меняем флаг
            _isPaused = true;
            
            // Меняем кнопку
            RecordButton.Text = "Продолжить запись";
            RecordButton.BackgroundColor = Color.FromArgb("#34C759");
            StatusLabel.Text = "Запись на паузе";
        }

        // Возобновляем запись
        private void ResumeRecording()
        {
            // Меняем флаг
            _isPaused = false;
            
            // Меняем кнопку обратно
            RecordButton.Text = "Пауза";
            RecordButton.BackgroundColor = Color.FromArgb("#FF9500");
            StatusLabel.Text = "Идет запись...";
            
            // Запускаем таймер обратно (продолжаем с текущего значения _seconds)
            _timer = new System.Threading.Timer(_ =>
            {
                _seconds++;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var minutes = _seconds / 60;
                    var secs = _seconds % 60;
                    TimerLabel.Text = $"{minutes:00}:{secs:00}";
                });
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        // Останавливаем запись и сохраняем
        private async Task StopRecording()
        {
            try
            {
                // Останавливаем таймер
                _timer?.Dispose();
                _timer = null;

                if (_recorder != null)
                {
                    // Останавливаем запись
                    await _recorder.StopRecording();
                    
                    // Получаем путь к временному файлу
                    var tempFilePath = _recorder.GetAudioFilePath();
                    
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        // Сохраняем запись через сервис
                        await _audioService.SaveRecording(tempFilePath, _seconds);
                        
                        // Показываем сообщение
                        await DisplayAlertAsync("Успех", "Запись сохранена!", "OK");
                        
                        // Обновляем список
                        LoadRecordings();
                    }
                    else
                    {
                        await DisplayAlertAsync("Ошибка", "Файл записи не найден", "OK");
                    }
                }

                // Меняем состояние обратно
                _isRecording = false;
                _isPaused = false;
                RecordButton.Text = "Начать запись";
                RecordButton.BackgroundColor = Color.FromArgb("#007AFF");
                StatusLabel.Text = "Готово к записи";
                TimerLabel.Text = "00:00";
                
                // Скрываем кнопку завершения
                FinishButton.IsVisible = false;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось остановить запись: {ex.Message}", "OK");
            }
        }

        // Нажатие на кнопку воспроизведения
        private async void OnPlayButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Получаем параметр из TapGestureRecognizer
                var tappedEventArgs = e as TappedEventArgs;
                var recording = tappedEventArgs?.Parameter as AudioRecording;
                
                if (recording != null && File.Exists(recording.FilePath))
                {
                    // Останавливаем предыдущее воспроизведение (если было)
                    _audioService.StopPlayback();
                    
                    // Запускаем новое воспроизведение
                    _audioService.PlayRecording(recording.FilePath);
                    
                    // Показываем меню с кнопкой остановки
                    var action = await DisplayActionSheetAsync(
                        $"▶ Воспроизводится:\n{recording.Title}",
                        "Закрыть",
                        "Остановить воспроизведение",
                        $"Длительность: {recording.FormattedDuration}");
                    
                    // Если выбрали "Остановить" - останавливаем
                    if (action == "Остановить воспроизведение")
                    {
                        _audioService.StopPlayback();
                    }
                }
                else
                {
                    await DisplayAlertAsync("Ошибка", "Файл записи не найден", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось воспроизвести: {ex.Message}", "OK");
            }
        }

        // Нажатие на кнопку "Завершить запись"
        private async void OnFinishButtonClicked(object sender, EventArgs e)
        {
            // Завершаем запись и сохраняем
            await StopRecording();
        }

        // Нажатие на кнопку удаления
        private async void OnDeleteButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Получаем параметр из TapGestureRecognizer
                var tappedEventArgs = e as TappedEventArgs;
                var recording = tappedEventArgs?.Parameter as AudioRecording;
                
                if (recording != null)
                {
                    // Спрашиваем подтверждение
                    var confirm = await DisplayAlertAsync("Удаление", 
                        $"Удалить запись '{recording.Title}'?", 
                        "Да", 
                        "Нет");
                    
                    if (confirm)
                    {
                        // Удаляем через сервис
                        await _audioService.DeleteRecording(recording);
                        
                        // Обновляем список
                        LoadRecordings();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось удалить: {ex.Message}", "OK");
            }
        }
    }
}
