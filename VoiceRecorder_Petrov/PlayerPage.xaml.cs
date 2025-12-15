using Plugin.AudioRecorder;
using VoiceRecorder_Petrov.Models;

namespace VoiceRecorder_Petrov
{
    // Страница плеера для воспроизведения записи
    public partial class PlayerPage : ContentPage
    {
        private readonly AudioRecording _recording;
        private readonly AudioPlayer _player;
        private bool _isPlaying = false;

        public PlayerPage(AudioRecording recording)
        {
            InitializeComponent();
            
            _recording = recording;
            _player = new AudioPlayer();
            
            // Устанавливаем информацию о записи
            TitleLabel.Text = _recording.Title;
            DateLabel.Text = _recording.FormattedDate;
            DurationLabel.Text = _recording.FormattedDuration;
        }

        // Воспроизведение/Пауза
        private void OnPlayPauseTapped(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Ставим на паузу (останавливаем)
                StopPlayback();
            }
            else
            {
                // Начинаем воспроизведение
                StartPlayback();
            }
        }

        // Начинаем воспроизведение
        private void StartPlayback()
        {
            try
            {
                // Воспроизводим файл
                _player.Play(_recording.FilePath);
                _isPlaying = true;
                
                // Меняем иконку на паузу
                PlayIcon.IsVisible = false;
                PauseIcon.IsVisible = true;
                StatusLabel.Text = "Воспроизведение...";
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Ошибка", $"Не удалось воспроизвести: {ex.Message}", "OK");
            }
        }

        // Останавливаем воспроизведение
        private void StopPlayback()
        {
            try
            {
                // AudioPlayer не имеет метода Stop, поэтому пересоздаем
                _isPlaying = false;
                
                // Меняем иконку на play
                PlayIcon.IsVisible = true;
                PauseIcon.IsVisible = false;
                StatusLabel.Text = "Остановлено";
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Ошибка", $"Ошибка остановки: {ex.Message}", "OK");
            }
        }

        // Закрытие плеера
        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // Останавливаем воспроизведение если играет
            if (_isPlaying)
            {
                StopPlayback();
            }
            
            // Закрываем страницу
            await Navigation.PopModalAsync();
        }
    }
}
