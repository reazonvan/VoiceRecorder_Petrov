using System.Text.Json;
using VoiceRecorder_Petrov.Models;

#if !ANDROID
using Plugin.AudioRecorder;
#endif

#if ANDROID
using Android.Media;
#endif

namespace VoiceRecorder_Petrov.Services
{
    // Сервис для работы с записями:
    // - хранение файлов в AppDataDirectory/Recordings
    // - метаданные в recordings.json
    // - воспроизведение выбранного файла
    public class AudioService
    {
        private readonly string _recordingsFolder;
        private readonly string _dataFile;

        // Флаг состояния нужен странице плеера/логике UI.
        private bool IsCurrentlyPlaying { get; set; } = false;

#if ANDROID
        // На Android используем MediaPlayer - он корректно и сразу останавливается,
        // в отличие от "killer" хака с AudioPlayer.
        private MediaPlayer? _androidPlayer;
        private readonly object _playerLock = new object();
#else
        private AudioPlayer? _currentPlayer;        // Плеер для воспроизведения (не Android)
#endif

        public AudioService()
        {
            // Путь к папке с записями (в данных приложения)
            _recordingsFolder = Path.Combine(FileSystem.AppDataDirectory, "Recordings");
            
            // Путь к JSON файлу с метаданными
            _dataFile = Path.Combine(FileSystem.AppDataDirectory, "recordings.json");
            
            // Создаем папку если её нет
            if (!Directory.Exists(_recordingsFolder))
            {
                Directory.CreateDirectory(_recordingsFolder);
            }
        }

        // --- МЕТОДЫ СОХРАНЕНИЯ ---
        
        // Сохраняем новую запись: копируем временный файл в постоянную папку и добавляем метаданные в JSON.
        // Ограничение: максимум 100 записей (чтобы не забить всё место).
        public async Task SaveRecording(string tempFilePath, int durationSeconds)
        {
            try
            {
                // Загружаем текущий список из JSON
                var recordings = await LoadRecordingsFromFile();
                
                // Проверяем лимит: если записей >= 100, удаляем самую старую
                if (recordings.Count >= 100)
                {
                    // Сортируем по дате и берём первую (самую старую)
                    var oldestRecording = recordings.OrderBy(r => r.CreatedDate).First();
                    
                    // Удаляем физический файл со старой записью
                    if (File.Exists(oldestRecording.FilePath))
                    {
                        File.Delete(oldestRecording.FilePath);
                    }
                    
                    // Убираем из списка
                    recordings.Remove(oldestRecording);
                }
                
                // Формируем имя файла с текущей датой (чтобы не было конфликтов)
                // Берём расширение из временного файла (.wav/.m4a/...), чтобы воспроизведение не ломалось
                var ext = Path.GetExtension(tempFilePath);
                if (string.IsNullOrWhiteSpace(ext))
                    ext = ".wav";

                var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                var newFilePath = Path.Combine(_recordingsFolder, fileName);

                // Копируем временный файл в папку Recordings
                File.Copy(tempFilePath, newFilePath, true);
                
                // Узнаём размер файла для отображения пользователю
                var fileInfo = new FileInfo(newFilePath);

                // Создаём объект с информацией о записи
                var recording = new AudioRecording
                {
                    Title = $"Запись от {DateTime.Now:dd.MM.yyyy HH:mm}",
                    FilePath = newFilePath,
                    CreatedDate = DateTime.Now,
                    DurationSeconds = durationSeconds,
                    FileSizeBytes = fileInfo.Length
                };
                
                // Добавляем новую запись в список
                recordings.Add(recording);
                
                // Сохраняем обновлённый список обратно в JSON
                await SaveRecordingsToFile(recordings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении записи: {ex.Message}");
            }
        }

        // --- МЕТОДЫ ЗАГРУЗКИ ---
        
        // Получаем все записи из JSON для отображения в списке на главной странице
        public async Task<List<AudioRecording>> GetAllRecordings()
        {
            try
            {
                // Читаем список из JSON файла
                var recordings = await LoadRecordingsFromFile();
                
                // Фильтруем: иногда файл может быть удалён вручную через проводник,
                // поэтому оставляем только те записи, для которых файлы реально существуют
                var validRecordings = recordings
                    .Where(r => File.Exists(r.FilePath))
                    .OrderByDescending(r => r.CreatedDate)  // Сортировка: новые записи сверху
                    .ToList();
                
                // Если нашлись "мёртвые" ссылки - обновляем JSON, чтобы там были только валидные записи
                if (validRecordings.Count != recordings.Count)
                {
                    await SaveRecordingsToFile(validRecordings);
                }
                
                return validRecordings;
            }
            catch (Exception)
            {
                // Если что-то пошло не так - возвращаем пустой список (лучше пустой список, чем краш)
                return new List<AudioRecording>();
            }
        }

        // --- МЕТОДЫ ВОСПРОИЗВЕДЕНИЯ ---
        
        // Воспроизводим выбранную запись (вызывается со страницы PlayerPage)
        public void PlayRecording(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Сначала останавливаем предыдущий плеер (если был), чтобы не было наложения
                    StopPlayback();
                    
#if ANDROID
                    // На Android используем нативный MediaPlayer - он надёжнее плагина
                    lock (_playerLock)
                    {
                        MediaPlayer player = new MediaPlayer();
                        _androidPlayer = player;
                        player.SetDataSource(filePath);
                        
                        // Настраиваем атрибуты аудио (только для API 21+, иначе будет краш)
                        // Указываем, что это медиа-контент (не рингтон/уведомление)
                        if (OperatingSystem.IsAndroidVersionAtLeast(21))
                        {
                            var builder = new AudioAttributes.Builder();
                            builder.SetUsage(AudioUsageKind.Media);
                            builder.SetContentType(AudioContentType.Music);
                            var attributes = builder.Build();
                            if (attributes != null)
                                player.SetAudioAttributes(attributes);
                        }

                        // Подписываемся на событие окончания воспроизведения
                        player.Completion += (_, __) => StopPlayback();

                        // Готовим плеер и запускаем
                        player.Prepare();
                        player.Start();
                        IsCurrentlyPlaying = true;
                    }
#else
                    // На остальных платформах используем плагин Plugin.AudioRecorder
                    _currentPlayer = new AudioPlayer();
                    _currentPlayer.Play(filePath);
                    IsCurrentlyPlaying = true;
#endif
                }
                else
                {
                    throw new Exception("Файл не найден");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка воспроизведения: {ex.Message}");
            }
        }

        // Останавливаем воспроизведение (вызывается при закрытии плеера или начале нового воспроизведения)
        public void StopPlayback()
        {
            try
            {
#if ANDROID
                lock (_playerLock)
                {
                    try
                    {
                        if (_androidPlayer != null)
                        {
                            // Аккуратно останавливаем MediaPlayer: Stop → Reset → Release
                            // Каждый вызов в try/catch, т.к. при некоторых состояниях могут быть исключения
                            try { _androidPlayer.Stop(); } catch { }
                            try { _androidPlayer.Reset(); } catch { }
                            try { _androidPlayer.Release(); } catch { }
                            _androidPlayer = null;
                        }
                    }
                    finally
                    {
                        IsCurrentlyPlaying = false;
                    }
                }
#else
                // На не-Android платформах просто обнуляем плеер
                IsCurrentlyPlaying = false;
                _currentPlayer = null;
#endif
            }
            catch
            {
                // Если что-то пошло не так - всё равно сбрасываем флаг
#if !ANDROID
                _currentPlayer = null;
#endif
                IsCurrentlyPlaying = false;
            }
        }

        // --- МЕТОДЫ УДАЛЕНИЯ ---
        
        // Удаляем запись: и физический файл, и метаданные из JSON
        public async Task DeleteRecording(AudioRecording recording)
        {
            try
            {
                // Сначала удаляем сам аудиофайл с диска
                if (File.Exists(recording.FilePath))
                {
                    File.Delete(recording.FilePath);
                }

                // Теперь убираем информацию о записи из JSON
                var recordings = await LoadRecordingsFromFile();
                
                // Ищем по ID и удаляем (ID генерируется при создании записи)
                recordings.RemoveAll(r => r.Id == recording.Id);
                
                // Сохраняем обновлённый список
                await SaveRecordingsToFile(recordings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при удалении: {ex.Message}");
            }
        }

        // --- ПРИВАТНЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С JSON ---
        
        // Загружаем список записей из JSON (внутренний метод, вызывается другими методами сервиса)
        private async Task<List<AudioRecording>> LoadRecordingsFromFile()
        {
            try
            {
                // Если это первый запуск приложения - JSON файла ещё нет
                if (!File.Exists(_dataFile))
                {
                    return new List<AudioRecording>();
                }

                // Читаем содержимое файла
                var json = await File.ReadAllTextAsync(_dataFile);
                
                // Десериализуем: превращаем JSON-строку в список объектов AudioRecording
                var recordings = JsonSerializer.Deserialize<List<AudioRecording>>(json);
                
                return recordings ?? new List<AudioRecording>();
            }
            catch (Exception)
            {
                // При любой ошибке (битый JSON, права доступа и т.д.) возвращаем пустой список
                return new List<AudioRecording>();
            }
        }

        // Сохраняем список записей в JSON файл (вызывается после добавления/удаления записи)
        private async Task SaveRecordingsToFile(List<AudioRecording> recordings)
        {
            try
            {
                // Сериализуем: превращаем список объектов в JSON-строку
                // WriteIndented = true делает JSON читаемым (с отступами и переносами)
                var json = JsonSerializer.Serialize(recordings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Записываем JSON в файл
                await File.WriteAllTextAsync(_dataFile, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении в файл: {ex.Message}");
            }
        }
    }
}
