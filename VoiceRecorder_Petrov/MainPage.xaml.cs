using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;
using System.Threading.Tasks;

#if !ANDROID
using Plugin.AudioRecorder;
#endif

#if ANDROID
using Android.Media;
#endif

namespace VoiceRecorder_Petrov
{
    // Главная страница: запись с микрофона + список сохранённых записей.
    public partial class MainPage : ContentPage
    {
        private readonly AudioService _audioService;
        private System.Threading.Timer? _timer;
        private int _seconds = 0;
        private bool _isRecording = false;
        private bool _isBusy = false; // защита от двойного нажатия Start/Stop

#if !ANDROID
        // На не-Android используем плагин как простой вариант.
        private AudioRecorderService? _recorder;
        private string? _recordingFilePath = null;
        private Task<string>? _recordingTask = null;
#endif

#if ANDROID
        // На Android используем MediaRecorder: стабильнее и даёт предсказуемый файл.
        private MediaRecorder? _androidRecorder;
        private string? _androidTempFilePath; // временный файл в CacheDirectory
#endif

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

        // Начинаем запись с микрофона (вызывается по нажатию кнопки "Начать запись")
        private async Task StartRecording()
        {
            try
            {
                // Сначала проверяем разрешение на микрофон (обязательно для Android/iOS)
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Ошибка", "Нужно разрешение на микрофон", "OK");
                    return;
                }

                // Запускаем запись: на Android используем нативный MediaRecorder,
                // на остальных платформах - плагин Plugin.AudioRecorder
#if ANDROID
                if (!StartRecordingAndroid())
                {
                    await DisplayAlertAsync("Ошибка", "Не удалось начать запись (MediaRecorder)", "OK");
                    return;
                }
#else
                // Создаём рекордер из плагина
                _recorder = new AudioRecorderService
                {
                    StopRecordingOnSilence = false,      // Не останавливать при тишине
                    StopRecordingAfterTimeout = false    // Без лимита времени
                };

                _recordingFilePath = null;
                _recordingTask = null;
                try
                {
                    // StartRecording возвращает Task<string>, который завершится при вызове StopRecording
                    _recordingTask = await _recorder.StartRecording();
                }
                catch (Exception ex)
                {
                    _recorder = null;
                    await DisplayAlertAsync("Ошибка", $"Не удалось начать запись: {ex.Message}", "OK");
                    return;
                }
#endif

                // Устанавливаем флаг "идёт запись" и сбрасываем счётчик секунд
                _isRecording = true;
                _seconds = 0;
                
                // Меняем внешний вид кнопки: теперь она красная "Остановить запись"
                RecordButton.Text = "Остановить запись";
                RecordButton.BackgroundColor = Color.FromArgb("#FF3B30");  // Красная
                StatusLabel.Text = "Идет запись...";
                
                // Запускаем таймер, который каждую секунду обновляет счётчик времени на экране
                _timer = new System.Threading.Timer(_ =>
                {
                    _seconds++;
                    
                    // Обновляем UI в главном потоке (из фонового потока таймера обновлять нельзя)
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

        // Останавливаем запись и сохраняем файл (вызывается по нажатию кнопки "Остановить запись")
        private async Task StopRecording()
        {
            try
            {
                // Останавливаем таймер UI
                _timer?.Dispose();
                _timer = null;

                string? tempFilePath = null;

#if ANDROID
                // На Android останавливаем MediaRecorder и получаем путь к временному файлу
                tempFilePath = StopRecordingAndroid();
#else
                // На остальных платформах работаем с плагином
                if (_recorder != null)
                {
                    // Останавливаем запись
                    await _recorder.StopRecording();
                    
                    // Даём плагину время завершить запись в файл
                    await Task.Delay(1000);

                    // Пытаемся получить путь к файлу разными способами (у плагина бывают разные варианты)
                    if (_recordingTask != null)
                    {
                        try { tempFilePath = await _recordingTask; } catch { }
                    }

                    if (string.IsNullOrEmpty(tempFilePath))
                        tempFilePath = _recordingFilePath;

                    if (string.IsNullOrEmpty(tempFilePath))
                        tempFilePath = _recorder.GetAudioFilePath();

                    // Чистим за собой
                    _recordingFilePath = null;
                    _recordingTask = null;
                    try { (_recorder as IDisposable)?.Dispose(); } catch { }
                    _recorder = null;
                }
#endif

                // Иногда файл создаётся с задержкой - ждём до 2 секунд (8 раз по 250мс)
                for (int i = 0; i < 8 && !string.IsNullOrEmpty(tempFilePath) && !File.Exists(tempFilePath); i++)
                    await Task.Delay(250);

                // Если файл появился - сохраняем его через AudioService
                if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                {
                    await _audioService.SaveRecording(tempFilePath, _seconds);
                    await DisplayAlertAsync("Успех", "Запись сохранена!", "OK");
                    
                    // Обновляем список записей на экране
                    LoadRecordings();
                }
                else
                {
                    // Если файл не создался - показываем отладочную информацию
                    // (это помогает понять, что пошло не так)
#if ANDROID
                    await DisplayAlertAsync("Отладка",
                        $"Android tempFilePath: {tempFilePath ?? "null"}\n" +
                        $"Exists: {(tempFilePath != null && File.Exists(tempFilePath))}\n" +
                        $"Секунд: {_seconds}",
                        "OK");
#else
                    var taskInfo = _recordingTask == null
                        ? "(нет задачи)"
                        : (_recordingTask.IsCompleted ? (_recordingTask.Result ?? "null") : "(не выполнена)");

                    await DisplayAlertAsync("Отладка",
                        $"recordingFilePath: {_recordingFilePath ?? "null"}\n" +
                        $"GetAudioFilePath: {_recorder?.GetAudioFilePath() ?? "null"}\n" +
                        $"Task result: {taskInfo}\n" +
                        $"Секунд: {_seconds}",
                        "OK");
#endif
                }

                // Возвращаем кнопку в исходное состояние
                _isRecording = false;
                RecordButton.Text = "Начать запись";
                RecordButton.BackgroundColor = Color.FromArgb("#007AFF");  // Синяя
                StatusLabel.Text = "Готово к записи";
                TimerLabel.Text = "00:00";
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", $"Не удалось остановить запись: {ex.Message}", "OK");
                
                // Сбрасываем состояние даже при ошибке, чтобы пользователь мог попробовать снова
                _isRecording = false;
                RecordButton.Text = "Начать запись";
                RecordButton.BackgroundColor = Color.FromArgb("#007AFF");
                StatusLabel.Text = "Готово к записи";
            }
        }

#if ANDROID
        // Android-специфичная запись через MediaRecorder (более стабильная, чем плагин)
        private bool StartRecordingAndroid()
        {
            try
            {
                // Сначала чистим предыдущий рекордер (если был)
                try { _androidRecorder?.Reset(); } catch { }
                try { _androidRecorder?.Release(); } catch { }
                _androidRecorder = null;

                // Формируем путь для временного файла (в кэше приложения)
                var fileName = $"temp_recording_{DateTime.Now:yyyyMMdd_HHmmss}.m4a";
                _androidTempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Если вдруг файл уже существует - удаляем (чтобы не было конфликтов)
                if (File.Exists(_androidTempFilePath))
                    File.Delete(_androidTempFilePath);

                // Начиная с Android 12 (API 31) конструктор MediaRecorder() устарел,
                // нужно использовать конструктор с Context. Проверяем версию ОС runtime.
                if (OperatingSystem.IsAndroidVersionAtLeast(31))
                {
                    _androidRecorder = new MediaRecorder(Android.App.Application.Context);
                }
                else
                {
                    // На Android < 31 используем старый конструктор (подавляем warning компилятора)
#pragma warning disable CS0618
                    _androidRecorder = new MediaRecorder();
#pragma warning restore CS0618
                }
                
                // Настраиваем параметры записи
                _androidRecorder.SetAudioSource(AudioSource.Mic);           // Источник - микрофон
                _androidRecorder.SetOutputFormat(OutputFormat.Mpeg4);        // Формат контейнера
                _androidRecorder.SetAudioEncoder(AudioEncoder.Aac);          // Кодек AAC
                _androidRecorder.SetAudioEncodingBitRate(128000);            // 128 kbps
                _androidRecorder.SetAudioSamplingRate(44100);                // 44.1 кГц
                _androidRecorder.SetOutputFile(_androidTempFilePath);        // Куда писать

                // Готовим и запускаем запись
                _androidRecorder.Prepare();
                _androidRecorder.Start();
                return true;
            }
            catch
            {
                // Если что-то пошло не так - чистим и возвращаем false
                try { _androidRecorder?.Reset(); } catch { }
                try { _androidRecorder?.Release(); } catch { }
                _androidRecorder = null;
                _androidTempFilePath = null;
                return false;
            }
        }

        // Останавливаем запись на Android и возвращаем путь к созданному файлу
        private string? StopRecordingAndroid()
        {
            try
            {
                if (_androidRecorder == null)
                    return null;

                // Останавливаем и освобождаем ресурсы MediaRecorder
                try { _androidRecorder.Stop(); } catch { }
                try { _androidRecorder.Reset(); } catch { }
                try { _androidRecorder.Release(); } catch { }
                _androidRecorder = null;

                // Возвращаем путь к временному файлу (его потом скопирует AudioService)
                return _androidTempFilePath;
            }
            finally
            {
                // Не чистим _androidTempFilePath здесь - он нужен для SaveRecording
            }
        }
#endif

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
