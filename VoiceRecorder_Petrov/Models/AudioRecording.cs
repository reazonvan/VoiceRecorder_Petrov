namespace VoiceRecorder_Petrov.Models
{
    // Модель аудиозаписи - хранит всю информацию о записи
    public class AudioRecording
    {
        // Уникальный ID записи
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Название записи
        public string Title { get; set; } = string.Empty;

        // Путь к файлу на диске
        public string FilePath { get; set; } = string.Empty;

        // Дата создания
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Длительность в секундах
        public int DurationSeconds { get; set; } = 0;

        // Размер файла в байтах
        public long FileSizeBytes { get; set; } = 0;

        // Форматированная длительность для отображения (00:00)
        public string FormattedDuration
        {
            get
            {
                var minutes = DurationSeconds / 60;
                var seconds = DurationSeconds % 60;
                return $"{minutes:00}:{seconds:00}";
            }
        }

        // Форматированная дата для отображения
        public string FormattedDate => CreatedDate.ToString("dd.MM.yyyy HH:mm");

        // Форматированный размер файла для отображения (KB/MB)
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
