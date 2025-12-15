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
    // ========================================
    // СЕРВИС ДЛЯ РАБОТЫ С АУДИОЗАПИСЯМИ
    // Отвечает за сохранение, загрузку, воспроизведение и удаление записей
    // Хранит данные: файлы в папке + информация в JSON
    // ========================================
    public class AudioService
    {
        // --- ПОЛЯ ---
        
        private readonly string _recordingsFolder;  // Папка для WAV файлов
        private readonly string _dataFile;          // Файл с информацией (JSON)
        // Флаг воспроизведения (сейчас не обязателен, но оставляем для понятности)
        // Чтобы не было предупреждения CS0414, используем свойство вместо поля.
        private bool IsCurrentlyPlaying { get; set; } = false;

#if ANDROID
        // На Android используем MediaPlayer - он корректно и сразу останавливается,
        // в отличие от "killer" хака с AudioPlayer.
        private MediaPlayer? _androidPlayer;
        private readonly object _playerLock = new object();
#else
        private AudioPlayer? _currentPlayer;        // Плеер для воспроизведения (не Android)
#endif

        // --- КОНСТРУКТОР ---
        
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
        
        // Сохраняем новую запись (копируем файл + добавляем в JSON)
        // Максимум 100 записей
        public async Task SaveRecording(string tempFilePath, int durationSeconds)
        {
            try
            {
                // Шаг 1: Загружаем существующие записи
                var recordings = await LoadRecordingsFromFile();
                
                // Шаг 2: Проверяем лимит (максимум 100 записей)
                if (recordings.Count >= 100)
                {
                    // Автоматически удаляем самую старую запись
                    var oldestRecording = recordings.OrderBy(r => r.CreatedDate).First();
                    
                    // Удаляем файл старой записи
                    if (File.Exists(oldestRecording.FilePath))
                    {
                        File.Delete(oldestRecording.FilePath);
                    }
                    
                    // Удаляем из списка
                    recordings.Remove(oldestRecording);
                }
                
                // Шаг 3: Создаем имя файла с датой
                // Берем расширение исходного файла (.wav/.m4a/и т.д.), чтобы не ломать воспроизведение
                var ext = Path.GetExtension(tempFilePath);
                if (string.IsNullOrWhiteSpace(ext))
                    ext = ".wav";

                var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                var newFilePath = Path.Combine(_recordingsFolder, fileName);

                // Шаг 4: Копируем временный файл в постоянное место
                File.Copy(tempFilePath, newFilePath, true);
                
                // Шаг 5: Получаем размер файла
                var fileInfo = new FileInfo(newFilePath);

                // Шаг 6: Создаем объект записи с информацией
                var recording = new AudioRecording
                {
                    Title = $"Запись от {DateTime.Now:dd.MM.yyyy HH:mm}",
                    FilePath = newFilePath,
                    CreatedDate = DateTime.Now,
                    DurationSeconds = durationSeconds,
                    FileSizeBytes = fileInfo.Length
                };
                
                // Шаг 7: Добавляем новую запись
                recordings.Add(recording);
                
                // Шаг 8: Сохраняем обратно в JSON
                await SaveRecordingsToFile(recordings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении записи: {ex.Message}");
            }
        }

        // --- МЕТОДЫ ЗАГРУЗКИ ---
        
        // Получаем все записи из JSON
        public async Task<List<AudioRecording>> GetAllRecordings()
        {
            try
            {
                // Загружаем из JSON файла
                var recordings = await LoadRecordingsFromFile();
                
                // Фильтруем - оставляем только те, у которых файлы существуют
                var validRecordings = recordings
                    .Where(r => File.Exists(r.FilePath))
                    .OrderByDescending(r => r.CreatedDate)  // Новые сверху
                    .ToList();
                
                // Если были записи без файлов - обновляем JSON
                if (validRecordings.Count != recordings.Count)
                {
                    await SaveRecordingsToFile(validRecordings);
                }
                
                return validRecordings;
            }
            catch (Exception)
            {
                return new List<AudioRecording>();
            }
        }

        // --- МЕТОДЫ ВОСПРОИЗВЕДЕНИЯ ---
        
        // Воспроизводим запись через AudioPlayer
        public void PlayRecording(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Останавливаем предыдущее воспроизведение
                    StopPlayback();
                    
#if ANDROID
                    lock (_playerLock)
                    {
                        _androidPlayer = new MediaPlayer();
                        _androidPlayer.SetDataSource(filePath);
                        
                        // Без Stream.* (чтобы не было конфликта с System.IO.Stream)
                        // AudioAttributes доступны с API 21+
                        _androidPlayer.SetAudioAttributes(
                            new AudioAttributes.Builder()
                                .SetUsage(AudioUsageKind.Media)
                                .SetContentType(AudioContentType.Music)
                                .Build());

                        // Когда дошли до конца - чистим ресурсы
                        _androidPlayer.Completion += (_, __) => StopPlayback();

                        _androidPlayer.Prepare();
                        _androidPlayer.Start();
                        IsCurrentlyPlaying = true;
                    }
#else
                    // Создаем новый плеер
                    _currentPlayer = new AudioPlayer();

                    // Запускаем воспроизведение
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

        // Останавливаем воспроизведение (создаем несколько плееров для гарантии)
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
                IsCurrentlyPlaying = false;
                _currentPlayer = null;
#endif
            }
            catch
            {
                _currentPlayer = null;
                IsCurrentlyPlaying = false;
            }
        }

        // --- МЕТОДЫ УДАЛЕНИЯ ---
        
        // Удаляем запись (файл + из JSON)
        public async Task DeleteRecording(AudioRecording recording)
        {
            try
            {
                // Удаляем файл с диска
                if (File.Exists(recording.FilePath))
                {
                    File.Delete(recording.FilePath);
                }

                // Загружаем список из JSON
                var recordings = await LoadRecordingsFromFile();
                
                // Удаляем запись из списка
                recordings.RemoveAll(r => r.Id == recording.Id);
                
                // Сохраняем обновленный список в JSON
                await SaveRecordingsToFile(recordings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при удалении: {ex.Message}");
            }
        }

        // --- ПРИВАТНЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С JSON ---
        
        // Загружаем список записей из JSON файла
        private async Task<List<AudioRecording>> LoadRecordingsFromFile()
        {
            try
            {
                // Если файл не существует - возвращаем пустой список
                if (!File.Exists(_dataFile))
                {
                    return new List<AudioRecording>();
                }

                // Читаем JSON из файла
                var json = await File.ReadAllTextAsync(_dataFile);
                
                // Преобразуем JSON в список объектов
                var recordings = JsonSerializer.Deserialize<List<AudioRecording>>(json);
                
                return recordings ?? new List<AudioRecording>();
            }
            catch (Exception)
            {
                return new List<AudioRecording>();
            }
        }

        // Сохраняем список записей в JSON файл
        private async Task SaveRecordingsToFile(List<AudioRecording> recordings)
        {
            try
            {
                // Преобразуем список в JSON (с отступами для читаемости)
                var json = JsonSerializer.Serialize(recordings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Записываем в файл
                await File.WriteAllTextAsync(_dataFile, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении в файл: {ex.Message}");
            }
        }
    }
}
