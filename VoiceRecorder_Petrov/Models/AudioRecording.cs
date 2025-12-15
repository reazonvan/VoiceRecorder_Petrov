namespace VoiceRecorder_Petrov.Models
{
    // ========================================
    // МОДЕЛЬ АУДИОЗАПИСИ
    // Хранит всю информацию об одной голосовой записи
    // Используется для отображения в списке и сохранения в JSON
    // ========================================
    public class AudioRecording
    {
        // --- ОСНОВНЫЕ СВОЙСТВА ---
        
        public string Id { get; set; } = Guid.NewGuid().ToString();  // Уникальный ID
        public string Title { get; set; } = string.Empty;             // Название записи
        public string FilePath { get; set; } = string.Empty;          // Путь к WAV файлу
        public DateTime CreatedDate { get; set; } = DateTime.Now;     // Дата создания
        public int DurationSeconds { get; set; } = 0;                 // Длительность в секундах
        public long FileSizeBytes { get; set; } = 0;                  // Размер файла в байтах

        // --- ФОРМАТИРОВАННЫЕ СВОЙСТВА (для отображения) ---
        
        // Длительность в формате MM:SS (например: 02:15)
        public string FormattedDuration
        {
            get
            {
                var minutes = DurationSeconds / 60;
                var seconds = DurationSeconds % 60;
                return $"{minutes:00}:{seconds:00}";
            }
        }

        // Дата в формате DD.MM.YYYY HH:MM (например: 15.12.2025 14:30)
        public string FormattedDate => CreatedDate.ToString("dd.MM.yyyy HH:mm");

        // Размер файла в понятном виде (байты → КБ → МБ)
        public string FormattedFileSize
        {
            get
            {
                if (FileSizeBytes < 1024)
                    return $"{FileSizeBytes} байт";
                else if (FileSizeBytes < 1024 * 1024)
                    return $"{FileSizeBytes / 1024.0:F1} КБ";
                else
                    return $"{FileSizeBytes / (1024.0 * 1024.0):F1} МБ";
            }
        }
    }
}
