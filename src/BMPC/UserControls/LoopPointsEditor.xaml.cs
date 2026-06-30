using BMPC.Audio;
using BMPC.Audio.Objects;
using NAudio.Wave;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BMPC.UserControls
{
    public partial class LoopPointsEditor : UserControl
    {
        private const int PeakCount = 900;
        private const double MinimumLoopSeconds = 0.05;

        private readonly DispatcherTimer playbackTimer;
        private float[] peaks = [];
        private string? filePath;
        private double durationSeconds;
        private AudioLoopPoints? loopPoints;
        private AudioLoopPoints? sourceLoopPoints;
        private DragHandle dragHandle = DragHandle.None;
        private AudioFileReader? playbackReader;
        private WaveOutEvent? playbackOutput;
        private bool isUpdatingText;

        public LoopPointsEditor()
        {
            InitializeComponent();
            this.playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(35)
            };
            this.playbackTimer.Tick += PlaybackTimer_Tick;
            ClearAudio();
        }

        public event Action<AudioLoopPoints?>? LoopPointsChanged;

        public void LoadAudio(string? selectedFilePath, AudioLoopPoints? initialLoopPoints)
        {
            StopPlayback();

            if (string.IsNullOrWhiteSpace(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                ClearAudio();
                return;
            }

            try
            {
                var info = AudioLoopTransformer.ReadLoopInfo(selectedFilePath);
                this.filePath = selectedFilePath;
                this.durationSeconds = info.DurationSeconds;
                this.sourceLoopPoints = info.ExistingLoopPoints?.Clone();
                this.loopPoints = NormalizeLoop(initialLoopPoints ?? this.sourceLoopPoints ?? CreateFullTrackLoop());
                this.peaks = ReadPeaks(selectedFilePath);
                EmptyText.Visibility = Visibility.Collapsed;
                StatusText.Text = BuildStatusText(info);
                UpdateTextBoxes();
                DrawWaveform();
                LoopPointsChanged?.Invoke(this.loopPoints.Clone());
            }
            catch (Exception ex)
            {
                ClearAudio();
                StatusText.Text = ex.Message;
            }
        }

        private void ClearAudio()
        {
            this.filePath = null;
            this.durationSeconds = 0;
            this.loopPoints = null;
            this.sourceLoopPoints = null;
            this.peaks = [];
            EmptyText.Visibility = Visibility.Visible;
            StatusText.Text = "";
            StartTextBox.Text = "";
            EndTextBox.Text = "";
            WaveCanvas.Children.Clear();
            LoopPointsChanged?.Invoke(null);
        }

        private float[] ReadPeaks(string selectedFilePath)
        {
            var result = new float[PeakCount];
            using var reader = new AudioFileReader(selectedFilePath);
            var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels / 10];
            var totalSamples = Math.Max(1, reader.Length / Math.Max(1, reader.WaveFormat.BitsPerSample / 8));
            var samplesPerPeak = Math.Max(1, totalSamples / PeakCount);
            long sampleIndex = 0;
            int read;

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    var peakIndex = (int)Math.Min(PeakCount - 1, sampleIndex / samplesPerPeak);
                    result[peakIndex] = Math.Max(result[peakIndex], Math.Abs(buffer[i]));
                    sampleIndex++;
                }
            }

            return result;
        }

        private void DrawWaveform()
        {
            WaveCanvas.Children.Clear();
            if (this.loopPoints is null || this.peaks.Length == 0 || WaveCanvas.ActualWidth <= 0 || WaveCanvas.ActualHeight <= 0)
            {
                return;
            }

            var width = WaveCanvas.ActualWidth;
            var height = WaveCanvas.ActualHeight;
            var middle = height / 2;
            var selectedLeft = SecondsToX(this.loopPoints.StartSeconds);
            var selectedRight = SecondsToX(this.loopPoints.EndSeconds);

            var selectedRegion = new Rectangle
            {
                Width = Math.Max(1, selectedRight - selectedLeft),
                Height = height,
                Fill = new SolidColorBrush(Color.FromArgb(38, 76, 175, 80))
            };
            Canvas.SetLeft(selectedRegion, selectedLeft);
            Canvas.SetTop(selectedRegion, 0);
            WaveCanvas.Children.Add(selectedRegion);

            var waveformBrush = new SolidColorBrush(Color.FromRgb(96, 125, 139));
            for (var i = 0; i < this.peaks.Length; i++)
            {
                var x = i * width / Math.Max(1, this.peaks.Length - 1);
                var amplitude = Math.Max(1, this.peaks[i] * (height - 16) / 2);
                WaveCanvas.Children.Add(new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = middle - amplitude,
                    Y2 = middle + amplitude,
                    Stroke = waveformBrush,
                    StrokeThickness = 1
                });
            }

            AddHandle(selectedLeft, Colors.LimeGreen);
            AddHandle(selectedRight, Colors.OrangeRed);
        }

        private void AddHandle(double x, Color color)
        {
            var handle = new Line
            {
                X1 = x,
                X2 = x,
                Y1 = 0,
                Y2 = WaveCanvas.ActualHeight,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 3
            };
            WaveCanvas.Children.Add(handle);
        }

        private void WaveCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.loopPoints is null)
            {
                return;
            }

            var x = e.GetPosition(WaveCanvas).X;
            var startX = SecondsToX(this.loopPoints.StartSeconds);
            var endX = SecondsToX(this.loopPoints.EndSeconds);
            this.dragHandle = Math.Abs(x - startX) <= Math.Abs(x - endX) ? DragHandle.Start : DragHandle.End;
            WaveCanvas.CaptureMouse();
            ApplyDrag(x);
        }

        private void WaveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.dragHandle == DragHandle.None)
            {
                return;
            }

            ApplyDrag(e.GetPosition(WaveCanvas).X);
        }

        private void WaveCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.dragHandle = DragHandle.None;
            WaveCanvas.ReleaseMouseCapture();
        }

        private void WaveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
            => DrawWaveform();

        private void ApplyDrag(double x)
        {
            if (this.loopPoints is null)
            {
                return;
            }

            var seconds = XToSeconds(x);
            if (this.dragHandle == DragHandle.Start)
            {
                this.loopPoints.StartSeconds = Math.Min(seconds, this.loopPoints.EndSeconds - MinimumLoopSeconds);
            }
            else if (this.dragHandle == DragHandle.End)
            {
                this.loopPoints.EndSeconds = Math.Max(seconds, this.loopPoints.StartSeconds + MinimumLoopSeconds);
            }

            this.loopPoints = NormalizeLoop(this.loopPoints);
            UpdateTextBoxes();
            DrawWaveform();
            LoopPointsChanged?.Invoke(this.loopPoints.Clone());
        }

        private void LoopTextBox_LostFocus(object sender, RoutedEventArgs e)
            => ApplyTextBoxes();

        private void LoopTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyTextBoxes();
                e.Handled = true;
            }
        }

        private void ApplyTextBoxes()
        {
            if (this.loopPoints is null || this.isUpdatingText)
            {
                return;
            }

            if (!TryParseSeconds(StartTextBox.Text, out var start) || !TryParseSeconds(EndTextBox.Text, out var end))
            {
                UpdateTextBoxes();
                return;
            }

            this.loopPoints.StartSeconds = start;
            this.loopPoints.EndSeconds = end;
            this.loopPoints = NormalizeLoop(this.loopPoints);
            UpdateTextBoxes();
            DrawWaveform();
            LoopPointsChanged?.Invoke(this.loopPoints.Clone());
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
            => StartPlayback();

        private void StopButton_Click(object sender, RoutedEventArgs e)
            => StopPlayback();

        private void UseSourceButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.filePath is null)
            {
                return;
            }

            if (this.sourceLoopPoints is null)
            {
                ResetLoop(CreateFullTrackLoop());
                return;
            }

            ResetLoop(this.sourceLoopPoints);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.filePath is null)
            {
                return;
            }

            ResetLoop(CreateFullTrackLoop());
        }

        private void ResetLoop(AudioLoopPoints points)
        {
            this.loopPoints = NormalizeLoop(points);
            UpdateTextBoxes();
            DrawWaveform();
            LoopPointsChanged?.Invoke(this.loopPoints.Clone());
        }

        private void StartPlayback()
        {
            if (this.filePath is null || this.loopPoints is null)
            {
                return;
            }

            StopPlayback();
            this.playbackReader = new AudioFileReader(this.filePath)
            {
                CurrentTime = TimeSpan.FromSeconds(this.loopPoints.StartSeconds)
            };
            this.playbackOutput = new WaveOutEvent();
            this.playbackOutput.Init(this.playbackReader);
            this.playbackOutput.Play();
            this.playbackTimer.Start();
        }

        private void StopPlayback()
        {
            this.playbackTimer.Stop();
            this.playbackOutput?.Stop();
            this.playbackOutput?.Dispose();
            this.playbackOutput = null;
            this.playbackReader?.Dispose();
            this.playbackReader = null;
        }

        private void PlaybackTimer_Tick(object? sender, EventArgs e)
        {
            if (this.playbackReader is null || this.loopPoints is null)
            {
                StopPlayback();
                return;
            }

            if (this.playbackReader.CurrentTime.TotalSeconds < this.loopPoints.EndSeconds)
            {
                return;
            }

            if (LoopPreviewCheckBox.IsChecked == true)
            {
                this.playbackReader.CurrentTime = TimeSpan.FromSeconds(this.loopPoints.StartSeconds);
                return;
            }

            StopPlayback();
        }

        private void Root_Unloaded(object sender, RoutedEventArgs e)
            => StopPlayback();

        private AudioLoopPoints CreateFullTrackLoop()
            => new()
            {
                IsEnabled = true,
                StartSeconds = 0,
                EndSeconds = this.durationSeconds
            };

        private AudioLoopPoints NormalizeLoop(AudioLoopPoints points)
        {
            var start = Clamp(points.StartSeconds, 0, this.durationSeconds);
            var end = Clamp(points.EndSeconds, 0, this.durationSeconds);
            if (end - start < MinimumLoopSeconds)
            {
                start = 0;
                end = this.durationSeconds;
            }

            return new AudioLoopPoints
            {
                IsEnabled = true,
                StartSeconds = start,
                EndSeconds = end
            };
        }

        private void UpdateTextBoxes()
        {
            if (this.loopPoints is null)
            {
                return;
            }

            this.isUpdatingText = true;
            StartTextBox.Text = FormatSeconds(this.loopPoints.StartSeconds);
            EndTextBox.Text = FormatSeconds(this.loopPoints.EndSeconds);
            this.isUpdatingText = false;
        }

        private double SecondsToX(double seconds)
        {
            if (this.durationSeconds <= 0)
            {
                return 0;
            }

            return seconds / this.durationSeconds * WaveCanvas.ActualWidth;
        }

        private double XToSeconds(double x)
        {
            if (WaveCanvas.ActualWidth <= 0)
            {
                return 0;
            }

            return Clamp(x / WaveCanvas.ActualWidth * this.durationSeconds, 0, this.durationSeconds);
        }

        private static bool TryParseSeconds(string value, out double seconds)
            => double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out seconds)
                || double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds);

        private static string FormatSeconds(double seconds)
            => seconds.ToString("0.000", CultureInfo.CurrentCulture);

        private static double Clamp(double value, double min, double max)
            => Math.Min(Math.Max(value, min), max);

        private static string BuildStatusText(AudioLoopInfo info)
        {
            var source = info.ExistingLoopPoints is null ? "no source loop" : "source loop found";
            return $"{info.DurationSeconds:0.000}s, {info.SampleRate} Hz, {source}";
        }

        private enum DragHandle
        {
            None,
            Start,
            End
        }
    }
}
