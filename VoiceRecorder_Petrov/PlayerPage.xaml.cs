using VoiceRecorder_Petrov.Models;
using VoiceRecorder_Petrov.Services;
using CommunityToolkit.Maui.Views;

namespace VoiceRecorder_Petrov
{
    // Страница плеера - воспроизводит аудиозаписи
    public partial class PlayerPage : ContentPage
    {
        private readonly AudioRecording _recording;
        private bool _isSeeking = false;

        public PlayerPage(AudioRecording recording, AudioService audioService)
        {
            InitializeComponent();
            
            _recording = recording;
            
            // Показываем информацию о записи
            TitleLabel.Text = recording.Title;
            InfoLabel.Text = $"{recording.FormattedDate} • {recording.FormattedFileSize}";
            TotalTimeLabel.Text = recording.FormattedDuration;
            
            // Устанавливаем максимум слайдера
            SeekSlider.Maximum = recording.DurationSeconds;
            
            // Загружаем аудио файл в плеер (ShouldAutoPlay=True, поэтому запустится сам)
            AudioPlayer.Source = MediaSource.FromFile(recording.FilePath);
            
            // Меняем кнопку на паузу (т.к. автоплей включен)
            PlayPauseButton.Text = "⏸";
        }

        // Событие - позиция воспроизведения изменилась
        private void OnPositionChanged(object sender, EventArgs e)
        {
            // Если пользователь двигает слайдер - не обновляем
            if (_isSeeking) return;
            
            // Получаем текущую позицию
            var currentSeconds = AudioPlayer.Position.TotalSeconds;
            
            // Обновляем время
            CurrentTimeLabel.Text = FormatTime(currentSeconds);
            
            // Обновляем слайдер
            SeekSlider.Value = currentSeconds;
            
            // Обновляем прогресс бар
            if (_recording.DurationSeconds > 0)
            {
                ProgressBar.Progress = currentSeconds / _recording.DurationSeconds;
            }
        }

        // Событие - воспроизведение закончилось
        private void OnMediaEnded(object sender, EventArgs e)
        {
            // Останавливаем плеер
            AudioPlayer.Stop();
            
            // Меняем кнопку на Play (теперь можно запустить заново)
            PlayPauseButton.Text = "▶";
            
            // Сбрасываем позицию в начало
            SeekSlider.Value = 0;
            CurrentTimeLabel.Text = "00:00";
            ProgressBar.Progress = 0;
        }

        // Форматируем секунды в MM:SS
        private string FormatTime(double totalSeconds)
        {
            var minutes = (int)totalSeconds / 60;
            var seconds = (int)totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        // Нажатие на кнопку Пауза/Плей
        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            // Проверяем состояние плеера
            var stateName = AudioPlayer.CurrentState.ToString();
            
            if (stateName == "Playing")
            {
                // Ставим на паузу
                AudioPlayer.Pause();
                PlayPauseButton.Text = "▶";
            }
            else if (stateName == "Paused")
            {
                // Возобновляем с того же места
                AudioPlayer.Play();
                PlayPauseButton.Text = "⏸";
            }
            else if (stateName == "Stopped")
            {
                // Запускаем заново с начала
                AudioPlayer.Source = MediaSource.FromFile(_recording.FilePath);
                AudioPlayer.Play();
                PlayPauseButton.Text = "⏸";
            }
            else
            {
                // На всякий случай - просто запускаем
                AudioPlayer.Play();
                PlayPauseButton.Text = "⏸";
            }
        }

        // Перемотка назад на 10 секунд
        private void OnBackwardClicked(object sender, EventArgs e)
        {
            // Получаем текущую позицию
            var currentSeconds = AudioPlayer.Position.TotalSeconds;
            
            // Вычисляем новую позицию (не меньше 0)
            var newSeconds = Math.Max(0, currentSeconds - 10);
            
            // Перематываем
            AudioPlayer.SeekTo(TimeSpan.FromSeconds(newSeconds));
        }

        // Перемотка вперед на 10 секунд
        private void OnForwardClicked(object sender, EventArgs e)
        {
            // Получаем текущую позицию
            var currentSeconds = AudioPlayer.Position.TotalSeconds;
            
            // Вычисляем новую позицию (не больше длительности)
            var newSeconds = Math.Min(_recording.DurationSeconds, currentSeconds + 10);
            
            // Перематываем
            AudioPlayer.SeekTo(TimeSpan.FromSeconds(newSeconds));
        }

        // Пользователь перетащил слайдер
        private void OnSeekDragCompleted(object sender, EventArgs e)
        {
            _isSeeking = true;
            
            // Перематываем аудио на позицию слайдера
            AudioPlayer.SeekTo(TimeSpan.FromSeconds(SeekSlider.Value));
            
            // Через 300ms разрешаем обновление
            Task.Delay(300).ContinueWith(_ => _isSeeking = false);
        }

        // При закрытии страницы - останавливаем плеер
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            AudioPlayer.Stop();
        }
    }
}

