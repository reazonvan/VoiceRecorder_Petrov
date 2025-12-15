using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;

namespace VoiceRecorder_Petrov
{
    // Страница плеера для воспроизведения записи
    public partial class PlayerPage : ContentPage
    {
        private readonly AudioRecording _recording;
        private readonly SimpleAudioPlayer _player;
        private bool _isDragging = false;
        private bool _isPlaying = false;

        public PlayerPage(AudioRecording recording)
        {
            InitializeComponent();
            
            _recording = recording;
            _player = new SimpleAudioPlayer();
            
            // Загружаем аудио файл
            _player.Load(_recording.FilePath, _recording.DurationSeconds);
            
            // Подписываемся на события
            _player.PositionChanged += OnPlayerPositionChanged;
            _player.PlaybackEnded += OnPlayerPlaybackEnded;
            
            // Устанавливаем информацию о записи
            TitleLabel.Text = _recording.Title;
            DateLabel.Text = _recording.FormattedDate;
            TotalTimeLabel.Text = _recording.FormattedDuration;
            PositionSlider.Maximum = _recording.DurationSeconds;
            CurrentTimeLabel.Text = "00:00";
        }

        // Обновление позиции от плеера
        private void OnPlayerPositionChanged(object? sender, double position)
        {
            if (!_isDragging)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PositionSlider.Value = position;
                    
                    var minutes = (int)position / 60;
                    var seconds = (int)position % 60;
                    CurrentTimeLabel.Text = $"{minutes:00}:{seconds:00}";
                });
            }
        }

        // Окончание воспроизведения
        private void OnPlayerPlaybackEnded(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isPlaying = false;
                PlayIcon.IsVisible = true;
                PauseIcon.IsVisible = false;
                PositionSlider.Value = 0;
                CurrentTimeLabel.Text = "00:00";
            });
        }

        // Воспроизведение/Пауза
        private void OnPlayPauseTapped(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                // Пауза (мгновенно!)
                _player.Pause();
                _isPlaying = false;
                PlayIcon.IsVisible = true;
                PauseIcon.IsVisible = false;
            }
            else
            {
                // Воспроизведение
                _player.Play();
                _isPlaying = true;
                PlayIcon.IsVisible = false;
                PauseIcon.IsVisible = true;
            }
        }

        // Начало перетаскивания ползунка
        private void OnSliderDragStarted(object sender, EventArgs e)
        {
            _isDragging = true;
        }

        // Конец перетаскивания ползунка
        private void OnSliderDragCompleted(object sender, EventArgs e)
        {
            _isDragging = false;
            
            // Перематываем на выбранную позицию
            _player.SeekTo(PositionSlider.Value);
            
            // Обновляем время
            var currentSeconds = (int)PositionSlider.Value;
            var minutes = currentSeconds / 60;
            var seconds = currentSeconds % 60;
            CurrentTimeLabel.Text = $"{minutes:00}:{seconds:00}";
        }

        // Закрытие плеера
        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // Останавливаем воспроизведение
            _player.Stop();
            
            // Закрываем страницу
            await Navigation.PopModalAsync();
        }

        // Очистка при закрытии
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            _player.Dispose();
        }
    }
}
