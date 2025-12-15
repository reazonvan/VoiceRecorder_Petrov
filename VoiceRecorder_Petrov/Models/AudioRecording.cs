namespace VoiceRecorder_Petrov.Models
{
    // Метаданные одной записи: то, что сохраняем в JSON и показываем в списке.
    public class AudioRecording
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int DurationSeconds { get; set; } = 0;
        public long FileSizeBytes { get; set; } = 0;

        // Для UI: MM:SS.
        public string FormattedDuration
        {
            get
            {
                var minutes = DurationSeconds / 60;
                var seconds = DurationSeconds % 60;
                return $"{minutes:00}:{seconds:00}";
            }
        }

        // Для UI: dd.MM.yyyy HH:mm.
        public string FormattedDate => CreatedDate.ToString("dd.MM.yyyy HH:mm");

        // Для UI: байт/КБ/МБ.
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
