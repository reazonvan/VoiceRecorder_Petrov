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
            // AppDataDirectory — “песочница” приложения: можно хранить записи без внешних разрешений.
            _recordingsFolder = Path.Combine(FileSystem.AppDataDirectory, "Recordings");
            
            // В JSON храним “каталог” записей (название/дата/длительность/путь к файлу).
            _dataFile = Path.Combine(FileSystem.AppDataDirectory, "recordings.json");
            
            // Папка создаётся при первом запуске.
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
                // Общая идея: файл кладём в папку приложения, а метаданные обновляем в JSON.
                var recordings = await LoadRecordingsFromFile();
                
                // Ограничение по количеству, чтобы папка со временем не разрасталась.
                if (recordings.Count >= 100)
                {
                    var oldestRecording = recordings.OrderBy(r => r.CreatedDate).First();
                    
                    // Удаляем и файл, и запись из списка.
                    if (File.Exists(oldestRecording.FilePath))
                    {
                        File.Delete(oldestRecording.FilePath);
                    }
                    
                    recordings.Remove(oldestRecording);
                }
                
                // Берём расширение исходного файла (.wav/.m4a/...), чтобы не ломать воспроизведение.
                var ext = Path.GetExtension(tempFilePath);
                if (string.IsNullOrWhiteSpace(ext))
                    ext = ".wav";

                // Делаем “читаемое” имя с датой/временем.
                var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                var newFilePath = Path.Combine(_recordingsFolder, fileName);

                // Переносим временный файл в постоянное место.
                File.Copy(tempFilePath, newFilePath, true);
                
                var fileInfo = new FileInfo(newFilePath);

                // Метаданные нужны для списка на главной странице.
                var recording = new AudioRecording
                {
                    Title = $"Запись от {DateTime.Now:dd.MM.yyyy HH:mm}",
                    FilePath = newFilePath,
                    CreatedDate = DateTime.Now,
                    DurationSeconds = durationSeconds,
                    FileSizeBytes = fileInfo.Length
                };
                
                recordings.Add(recording);
                
                // Записываем обновлённый каталог обратно в JSON.
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
                    // Не даём двум записям играть одновременно.
                    StopPlayback();
                    
#if ANDROID
                    lock (_playerLock)
                    {
                        MediaPlayer player = new MediaPlayer();
                        _androidPlayer = player;
                        player.SetDataSource(filePath);
                        
                        // Без Stream.* (чтобы не было конфликта с System.IO.Stream)
                        // AudioAttributes доступны с API 21+
                        if (OperatingSystem.IsAndroidVersionAtLeast(21))
                        {
                            var builder = new AudioAttributes.Builder();
                            builder.SetUsage(AudioUsageKind.Media);
                            builder.SetContentType(AudioContentType.Music);
                            var attributes = builder.Build();
                            if (attributes != null)
                                player.SetAudioAttributes(attributes);
                        }

                        // Когда дошли до конца - чистим ресурсы
                        player.Completion += (_, __) => StopPlayback();

                        player.Prepare();
                        player.Start();
                        IsCurrentlyPlaying = true;
                    }
#else
                    // Для не-Android используем плеер из плагина.
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

        // Останавливаем воспроизведение и освобождаем ресурсы.
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
                            // На Android важно вызвать Release(), иначе плеер может “держать” аудиофокус.
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
#if !ANDROID
                _currentPlayer = null;
#endif
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
