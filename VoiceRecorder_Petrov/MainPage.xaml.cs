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

        // Начинаем запись с микрофона
        private async Task StartRecording()
        {
            try
            {
                // Разрешение на микрофон.
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Ошибка", "Нужно разрешение на микрофон", "OK");
                    return;
                }

                // Старт записи (Android: MediaRecorder, остальные: Plugin.AudioRecorder).
#if ANDROID
                if (!StartRecordingAndroid())
                {
                    await DisplayAlertAsync("Ошибка", "Не удалось начать запись (MediaRecorder)", "OK");
                    return;
                }
#else
                _recorder = new AudioRecorderService
                {
                    StopRecordingOnSilence = false,
                    StopRecordingAfterTimeout = false
                };

                _recordingFilePath = null;
                _recordingTask = null;
                try
                {
                    _recordingTask = await _recorder.StartRecording(); // Task<string> (завершится после Stop)
                }
                catch (Exception ex)
                {
                    _recorder = null;
                    await DisplayAlertAsync("Ошибка", $"Не удалось начать запись: {ex.Message}", "OK");
                    return;
                }
#endif

                _isRecording = true;
                _seconds = 0;
                
                RecordButton.Text = "Остановить запись";
                RecordButton.BackgroundColor = Color.FromArgb("#FF3B30");  // Красная
                StatusLabel.Text = "Идет запись...";
                
                // Таймер только для UI.
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
                _timer?.Dispose();
                _timer = null;

                string? tempFilePath = null;

#if ANDROID
                tempFilePath = StopRecordingAndroid();
#else
                if (_recorder != null)
                {
                    await _recorder.StopRecording();
                    await Task.Delay(1000);

                    if (_recordingTask != null)
                    {
                        try { tempFilePath = await _recordingTask; } catch { }
                    }

                    if (string.IsNullOrEmpty(tempFilePath))
                        tempFilePath = _recordingFilePath;

                    if (string.IsNullOrEmpty(tempFilePath))
                        tempFilePath = _recorder.GetAudioFilePath();

                    _recordingFilePath = null;
                    _recordingTask = null;
                    try { (_recorder as IDisposable)?.Dispose(); } catch { }
                    _recorder = null;
                }
#endif

                // Даем файлу появиться (до 2 секунд)
                for (int i = 0; i < 8 && !string.IsNullOrEmpty(tempFilePath) && !File.Exists(tempFilePath); i++)
                    await Task.Delay(250);

                if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                {
                    await _audioService.SaveRecording(tempFilePath, _seconds);
                    await DisplayAlertAsync("Успех", "Запись сохранена!", "OK");
                    LoadRecordings();
                }
                else
                {
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

#if ANDROID
        // Android-запись через MediaRecorder: меньше сюрпризов с путём/созданием файла.
        private bool StartRecordingAndroid()
        {
            try
            {
                // На всякий случай чистим старое
                try { _androidRecorder?.Reset(); } catch { }
                try { _androidRecorder?.Release(); } catch { }
                _androidRecorder = null;

                // Создаем путь, куда ПРЯМО сейчас будем писать файл
                var fileName = $"temp_recording_{DateTime.Now:yyyyMMdd_HHmmss}.m4a";
                _androidTempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Если вдруг такой файл уже есть - удаляем
                if (File.Exists(_androidTempFilePath))
                    File.Delete(_androidTempFilePath);

                // Начиная с Android 12 (API 31) рекомендуется конструктор с Context,
                // но на более старых версиях его нет — выбираем по runtime-версии ОС.
                if (OperatingSystem.IsAndroidVersionAtLeast(31))
                {
                    _androidRecorder = new MediaRecorder(Android.App.Application.Context);
                }
                else
                {
                    // MediaRecorder() помечен obsolete начиная с Android 31, но на <31 это корректный путь.
#pragma warning disable CS0618
                    _androidRecorder = new MediaRecorder();
#pragma warning restore CS0618
                }
                _androidRecorder.SetAudioSource(AudioSource.Mic);
                _androidRecorder.SetOutputFormat(OutputFormat.Mpeg4);
                _androidRecorder.SetAudioEncoder(AudioEncoder.Aac);
                _androidRecorder.SetAudioEncodingBitRate(128000);
                _androidRecorder.SetAudioSamplingRate(44100);
                _androidRecorder.SetOutputFile(_androidTempFilePath);

                _androidRecorder.Prepare();
                _androidRecorder.Start();
                return true;
            }
            catch
            {
                // Если старт не удался - чистим
                try { _androidRecorder?.Reset(); } catch { }
                try { _androidRecorder?.Release(); } catch { }
                _androidRecorder = null;
                _androidTempFilePath = null;
                return false;
            }
        }

        private string? StopRecordingAndroid()
        {
            try
            {
                if (_androidRecorder == null)
                    return null;

                try { _androidRecorder.Stop(); } catch { }
                try { _androidRecorder.Reset(); } catch { }
                try { _androidRecorder.Release(); } catch { }
                _androidRecorder = null;

                return _androidTempFilePath;
            }
            finally
            {
                // оставляем _androidTempFilePath до SaveRecording
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
