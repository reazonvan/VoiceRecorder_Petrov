using Plugin.AudioRecorder;
using System.Text.Json;
using VoiceRecorder_Petrov.Models;

namespace VoiceRecorder_Petrov.Services
{
    // Простой сервис для работы с аудиозаписями
    public class AudioService
    {
        // Папка где храним все записи
        private readonly string _recordingsFolder;
        
        // Файл с информацией о записях (JSON)
        private readonly string _dataFile;
        
        // Плеер с поддержкой паузы и перемотки
        private readonly SimpleAudioPlayer _player;

        public AudioService()
        {
            // Создаем папку для записей в данных приложения
            _recordingsFolder = Path.Combine(FileSystem.AppDataDirectory, "Recordings");
            
            // Файл для хранения информации о записях
            _dataFile = Path.Combine(FileSystem.AppDataDirectory, "recordings.json");
            
            // Создаем папку если её нет
            if (!Directory.Exists(_recordingsFolder))
            {
                Directory.CreateDirectory(_recordingsFolder);
            }
            
            // Создаем плеер
            _player = new SimpleAudioPlayer();
        }

        // Сохраняем новую запись
        public async Task SaveRecording(string tempFilePath, int durationSeconds)
        {
            try
            {
                // Создаем имя файла с датой и временем
                var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
                var newFilePath = Path.Combine(_recordingsFolder, fileName);

                // Копируем временный файл в постоянное место
                File.Copy(tempFilePath, newFilePath, true);
                
                // Получаем размер файла
                var fileInfo = new FileInfo(newFilePath);

                // Создаем объект записи
                var recording = new AudioRecording
                {
                    Title = $"Запись от {DateTime.Now:dd.MM.yyyy HH:mm}",
                    FilePath = newFilePath,
                    CreatedDate = DateTime.Now,
                    DurationSeconds = durationSeconds,
                    FileSizeBytes = fileInfo.Length
                };

                // Загружаем существующие записи
                var recordings = await LoadRecordingsFromFile();
                
                // Добавляем новую
                recordings.Add(recording);
                
                // Сохраняем обратно в файл
                await SaveRecordingsToFile(recordings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении записи: {ex.Message}");
            }
        }

        // Получаем все записи
        public async Task<List<AudioRecording>> GetAllRecordings()
        {
            try
            {
                var recordings = await LoadRecordingsFromFile();
                
                // Фильтруем - оставляем только те, у которых файлы существуют
                var validRecordings = recordings
                    .Where(r => File.Exists(r.FilePath))
                    .OrderByDescending(r => r.CreatedDate)
                    .ToList();
                
                // Если есть записи без файлов - удаляем их из списка
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

        // Воспроизводим запись
        public async Task PlayRecording(string filePath)
        {
            try
            {
                await _player.Play(filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при воспроизведении: {ex.Message}");
            }
        }

        // Ставим на паузу
        public void PausePlayback()
        {
            _player.Pause();
        }

        // Возобновляем воспроизведение
        public void ResumePlayback()
        {
            _player.Resume();
        }

        // Останавливаем воспроизведение
        public void StopPlayback()
        {
            _player.Stop();
        }

        // Получаем текущую позицию
        public double GetCurrentPosition()
        {
            return _player.GetCurrentPosition();
        }

        // Получаем длительность
        public double GetDuration()
        {
            return _player.GetDuration();
        }

        // Проверяем играет ли
        public bool IsPlaying()
        {
            return _player.IsPlaying;
        }

        // Перематываем на указанную позицию
        public void SeekTo(double seconds)
        {
            _player.SeekTo(seconds);
        }

        // Удаляем запись
        public async Task DeleteRecording(AudioRecording recording)
        {
            try
            {
                // Удаляем файл с диска
                if (File.Exists(recording.FilePath))
                {
                    File.Delete(recording.FilePath);
                }

                // Загружаем список
                var recordings = await LoadRecordingsFromFile();
                
                // Удаляем запись из списка
                recordings.RemoveAll(r => r.Id == recording.Id);
                
                // Сохраняем обновленный список
                await SaveRecordingsToFile(recordings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при удалении: {ex.Message}");
            }
        }

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
                
                // Десериализуем из JSON в список объектов
                var recordings = JsonSerializer.Deserialize<List<AudioRecording>>(json);
                
                return recordings ?? new List<AudioRecording>();
            }
            catch (Exception)
            {
                // Если ошибка - возвращаем пустой список
                return new List<AudioRecording>();
            }
        }

        // Сохраняем список записей в JSON файл
        private async Task SaveRecordingsToFile(List<AudioRecording> recordings)
        {
            try
            {
                // Сериализуем список в JSON с красивым форматированием
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
