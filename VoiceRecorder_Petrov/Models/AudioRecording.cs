namespace VoiceRecorder_Petrov.Models
{
    // Модель аудиозаписи - описывает одну запись в списке.
    // Эти данные сохраняются в recordings.json и используются для отображения в UI.
    public class AudioRecording
    {
        // Уникальный ID (генерируется при создании объекта, нужен для удаления)
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Название записи (генерируется автоматически: "Запись от 15.12.2025 14:30")
        public string Title { get; set; } = string.Empty;
        
        // Полный путь к аудиофайлу на диске
        public string FilePath { get; set; } = string.Empty;
        
        // Дата и время создания записи
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Длительность записи в секундах (считаем при остановке записи)
        public int DurationSeconds { get; set; } = 0;
        
        // Размер файла в байтах
        public long FileSizeBytes { get; set; } = 0;

        // --- Форматированные свойства для UI ---
        
        // Длительность в виде MM:SS (например, "02:45")
        public string FormattedDuration
        {
            get
            {
                var minutes = DurationSeconds / 60;
                var seconds = DurationSeconds % 60;
                return $"{minutes:00}:{seconds:00}";
            }
        }

        // Дата в виде dd.MM.yyyy HH:mm (например, "15.12.2025 14:30")
        public string FormattedDate => CreatedDate.ToString("dd.MM.yyyy HH:mm");

        // Размер файла в человеко-читаемом виде (байт → КБ → МБ)
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
