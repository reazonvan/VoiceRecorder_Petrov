using Plugin.AudioRecorder;
using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;
using System.Threading.Tasks;

namespace VoiceRecorder_Petrov
{
    // ========================================
    // ГЛАВНАЯ СТРАНИЦА ПРИЛОЖЕНИЯ
    // Отвечает за запись голоса и отображение списка записей
    // ========================================
    public partial class MainPage : ContentPage
    {
        // --- ПОЛЯ КЛАССА ---
        
        private readonly AudioService _audioService;        // Сервис для работы с файлами и JSON
        private AudioRecorderService? _recorder;             // Объект для записи звука с микрофона
        private System.Threading.Timer? _timer;              // Таймер для отсчета времени записи
        private int _seconds = 0;                            // Счетчик секунд записи
        private bool _isRecording = false;                   // Флаг: идет запись или нет
        private string? _recordingFilePath = null;           // Путь к файлу (сохраняем при старте)
        private Task<string>? _recordingTask = null;         // Задача, которая вернет путь (ждем после Stop)
        private bool _isBusy = false;                        // Флаг: идет старт/стоп (защита от двойных нажатий)

        // --- КОНСТРУКТОР ---
        
        public MainPage()
        {
            InitializeComponent();
            
            _audioService = new AudioService();  // Создаем сервис
            LoadRecordings();                    // Загружаем список записей
        }

        // --- МЕТОДЫ РАБОТЫ СО СПИСКОМ ---
        
        // Загружаем все записи из JSON файла и показываем в списке
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

        // --- МЕТОДЫ ЗАПИСИ ---
        
        // Обработчик нажатия на кнопку записи
        private async void OnRecordButtonClicked(object sender, EventArgs e)
        {
            if (_isBusy)
                return;

            _isBusy = true;
            RecordButton.IsEnabled = false;

            if (_isRecording)
            {
                // Если запись идет - останавливаем и сохраняем
                await StopRecording();
            }
            else
            {
                // Если не идет - начинаем новую запись
                await StartRecording();
            }

            RecordButton.IsEnabled = true;
            _isBusy = false;
        }

        // Начинаем запись с микрофона
        private async Task StartRecording()
        {
            try
            {
                // Шаг 1: Запрашиваем разрешение на микрофон
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Ошибка", "Нужно разрешение на микрофон", "OK");
                    return;
                }

                // Шаг 2: Создаем объект для записи
                _recorder = new AudioRecorderService
                {
                    StopRecordingOnSilence = false,      // Не останавливать при тишине
                    StopRecordingAfterTimeout = false    // Не останавливать по таймауту
                };

                // Шаг 3: ВАЖНО
                // Ждем только "старт" (первый await) — так мы точно знаем, что запись реально началась.
                // Путь (string) придет позже, после StopRecording(), через _recordingTask.
                _recordingFilePath = null;
                _recordingTask = null;
                try
                {
                    _recordingTask = await _recorder.StartRecording(); // Task<string> (завершится после Stop)
                }
                catch (Exception ex)
                {
                    // Если не смогли стартовать запись — НЕ переводим UI в режим "идет запись"
                    _recorder = null;
                    await DisplayAlertAsync("Ошибка", $"Не удалось начать запись: {ex.Message}", "OK");
                    return;
                }

                // Шаг 4: Меняем состояние приложения
                _isRecording = true;
                _seconds = 0;
                
                // Шаг 5: Обновляем кнопку
                RecordButton.Text = "Остановить запись";
                RecordButton.BackgroundColor = Color.FromArgb("#FF3B30");  // Красная
                StatusLabel.Text = "Идет запись...";
                
                // Шаг 6: Запускаем таймер (каждую секунду обновляет экран)
                _timer = new System.Threading.Timer(_ =>
                {
                    _seconds++;  // Увеличиваем счетчик
                    
                    // Обновляем UI (должно быть в главном потоке)
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

        // Останавливаем запись и сохраняем файл
        private async Task StopRecording()
        {
            try
            {
                // Шаг 1: Останавливаем таймер
                _timer?.Dispose();
                _timer = null;

                if (_recorder != null)
                {
                    // Шаг 2: Останавливаем запись
                    await _recorder.StopRecording();
                    
                    // Шаг 3: Даем БОЛЬШЕ времени на сохранение (1 секунда)
                    await Task.Delay(1000);
                    
                    // Шаг 4: Получаем путь (самый надежный способ — результат _recordingTask)
                    string? tempFilePath = null;
                    if (_recordingTask != null)
                    {
                        try { tempFilePath = await _recordingTask; } catch { }
                    }
                    
                    // Резерв 1: сохраненный путь (если был)
                    if (string.IsNullOrEmpty(tempFilePath))
                        tempFilePath = _recordingFilePath;

                    // Резерв 2: GetAudioFilePath()
                    if (string.IsNullOrEmpty(tempFilePath))
                        tempFilePath = _recorder.GetAudioFilePath();

                    // Даем файлу появиться (8 раз по 250 мс = до 2 секунд)
                    for (int i = 0; i < 8 && !string.IsNullOrEmpty(tempFilePath) && !File.Exists(tempFilePath); i++)
                        await Task.Delay(250);
                    
                    // Шаг 5: Проверяем файл
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        // Файл найден - сохраняем!
                        await _audioService.SaveRecording(tempFilePath, _seconds);
                        await DisplayAlertAsync("Успех", "Запись сохранена!", "OK");
                        LoadRecordings();
                    }
                    else
                    {
                        // Показываем отладочную информацию
                        var taskInfo = _recordingTask == null
                            ? "(нет задачи)"
                            : (_recordingTask.IsCompleted ? (_recordingTask.Result ?? "null") : "(не выполнена)");

                        await DisplayAlertAsync("Отладка", 
                            $"recordingFilePath: {_recordingFilePath ?? "null"}\n" +
                            $"GetAudioFilePath: {_recorder.GetAudioFilePath() ?? "null"}\n" +
                            $"Task result: {taskInfo}\n" +
                            $"Секунд: {_seconds}", 
                            "OK");
                    }
                    
                    // Шаг 6: освобождаем рекордер (иначе следующий старт иногда ломается)
                    _recordingFilePath = null;
                    _recordingTask = null;
                    try { (_recorder as IDisposable)?.Dispose(); } catch { }
                    _recorder = null;
                }

                // Шаг 7: Сбрасываем состояние
                _isRecording = false;
                RecordButton.Text = "Начать запись";
                RecordButton.BackgroundColor = Color.FromArgb("#007AFF");  // Синяя
                StatusLabel.Text = "Готово к записи";
                TimerLabel.Text = "00:00";
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось остановить запись: {ex.Message}", "OK");
                
                // Сбрасываем состояние даже при ошибке
                _isRecording = false;
                RecordButton.Text = "Начать запись";
                RecordButton.BackgroundColor = Color.FromArgb("#007AFF");
                StatusLabel.Text = "Готово к записи";
            }
        }

        // --- МЕТОДЫ ВОСПРОИЗВЕДЕНИЯ ---
        
        // Обработчик нажатия на кнопку Play (открывает плеер)
        private async void OnPlayButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Получаем запись из параметра кнопки
                var tappedEventArgs = e as TappedEventArgs;
                var recording = tappedEventArgs?.Parameter as AudioRecording;
                
                if (recording != null && File.Exists(recording.FilePath))
                {
                    // Открываем страницу плеера
                    await Navigation.PushAsync(new PlayerPage(recording, _audioService));
                }
                else
                {
                    await DisplayAlertAsync("Ошибка", "Файл записи не найден", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось открыть плеер: {ex.Message}", "OK");
            }
        }

        // --- МЕТОДЫ УДАЛЕНИЯ ---
        
        // Обработчик нажатия на кнопку Delete (удаляет запись)
        private async void OnDeleteButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Получаем запись из параметра кнопки
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
                        // Удаляем файл + запись из JSON
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
