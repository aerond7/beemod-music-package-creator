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
        private const double HandleHitWidth = 18;
        private const double HandleStrokeWidth = 4;

        private readonly DispatcherTimer playbackTimer;
        private float[] peaks = [];
        private string? filePath;
        private double durationSeconds;
        private double playheadSeconds;
        private AudioLoopPoints? loopPoints;
        private AudioLoopPoints? sourceLoopPoints;
        private DragHandle dragHandle = DragHandle.None;
        private AudioFileReader? playbackReader;
        private WaveOutEvent? playbackOutput;
        private Rectangle? loopRegion;
        private Line? startHandleLine;
        private Line? endHandleLine;
        private Line? playheadLine;
        private Polygon? startHandleThumb;
        private Polygon? endHandleThumb;
        private Polygon? playheadThumb;
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
                this.playheadSeconds = this.loopPoints.StartSeconds;
                this.peaks = ReadPeaks(selectedFilePath);
                EmptyText.Visibility = Visibility.Collapsed;
                StatusText.Text = BuildStatusText(info);
                UpdateTextBoxes();
                DrawEditor();
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
            this.playheadSeconds = 0;
            this.loopPoints = null;
            this.sourceLoopPoints = null;
            this.peaks = [];
            this.loopRegion = null;
            this.startHandleLine = null;
            this.endHandleLine = null;
            this.playheadLine = null;
            this.startHandleThumb = null;
            this.endHandleThumb = null;
            this.playheadThumb = null;
            EmptyText.Visibility = Visibility.Visible;
            StatusText.Text = "";
            StartTextBox.Text = "";
            EndTextBox.Text = "";
            WaveCanvas.Children.Clear();
            OverlayCanvas.Children.Clear();
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

        private void DrawEditor()
        {
            WaveCanvas.Children.Clear();
            OverlayCanvas.Children.Clear();
            this.loopRegion = null;
            this.startHandleLine = null;
            this.endHandleLine = null;
            this.playheadLine = null;
            this.startHandleThumb = null;
            this.endHandleThumb = null;
            this.playheadThumb = null;

            if (this.loopPoints is null || this.peaks.Length == 0 || OverlayCanvas.ActualWidth <= 0 || OverlayCanvas.ActualHeight <= 0)
            {
                return;
            }

            DrawWaveformPath();
            CreateOverlayElements();
            UpdateOverlayVisuals();
        }

        private void DrawWaveformPath()
        {
            var width = OverlayCanvas.ActualWidth;
            var height = OverlayCanvas.ActualHeight;
            var middle = height / 2;
            var geometry = new StreamGeometry();

            using (var context = geometry.Open())
            {
                for (var i = 0; i < this.peaks.Length; i++)
                {
                    var x = i * width / Math.Max(1, this.peaks.Length - 1);
                    var amplitude = Math.Max(1, this.peaks[i] * (height - 16) / 2);
                    context.BeginFigure(new Point(x, middle - amplitude), isFilled: false, isClosed: false);
                    context.LineTo(new Point(x, middle + amplitude), isStroked: true, isSmoothJoin: false);
                }
            }

            geometry.Freeze();
            WaveCanvas.Children.Add(new System.Windows.Shapes.Path
            {
                Data = geometry,
                Stroke = new SolidColorBrush(Color.FromRgb(96, 125, 139)),
                StrokeThickness = 1,
                SnapsToDevicePixels = true
            });
        }

        private void CreateOverlayElements()
        {
            this.loopRegion = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(46, 76, 175, 80)),
                IsHitTestVisible = false
            };
            OverlayCanvas.Children.Add(this.loopRegion);

            this.startHandleLine = CreateHandleLine(Colors.LimeGreen, "Loop start");
            this.endHandleLine = CreateHandleLine(Colors.OrangeRed, "Loop end");
            this.playheadLine = CreateHandleLine(Color.FromRgb(255, 193, 7), "Playhead");
            this.playheadLine.StrokeThickness = 2;

            this.startHandleThumb = CreateThumb(Colors.LimeGreen, "Loop start");
            this.endHandleThumb = CreateThumb(Colors.OrangeRed, "Loop end");
            this.playheadThumb = CreateThumb(Color.FromRgb(255, 193, 7), "Playhead");

            OverlayCanvas.Children.Add(this.startHandleLine);
            OverlayCanvas.Children.Add(this.endHandleLine);
            OverlayCanvas.Children.Add(this.playheadLine);
            OverlayCanvas.Children.Add(this.startHandleThumb);
            OverlayCanvas.Children.Add(this.endHandleThumb);
            OverlayCanvas.Children.Add(this.playheadThumb);
        }

        private static Line CreateHandleLine(Color color, string tooltip)
            => new()
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = HandleStrokeWidth,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true,
                ToolTip = tooltip
            };

        private static Polygon CreateThumb(Color color, string tooltip)
            => new()
            {
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1,
                IsHitTestVisible = false,
                ToolTip = tooltip
            };

        private void UpdateOverlayVisuals()
        {
            if (this.loopPoints is null)
            {
                return;
            }

            if (this.loopRegion is null ||
                this.startHandleLine is null ||
                this.endHandleLine is null ||
                this.playheadLine is null ||
                this.startHandleThumb is null ||
                this.endHandleThumb is null ||
                this.playheadThumb is null)
            {
                DrawEditor();
                return;
            }

            var height = OverlayCanvas.ActualHeight;
            var selectedLeft = SecondsToX(this.loopPoints.StartSeconds);
            var selectedRight = SecondsToX(this.loopPoints.EndSeconds);
            var playheadX = SecondsToX(this.playheadSeconds);

            this.loopRegion.Width = Math.Max(1, selectedRight - selectedLeft);
            this.loopRegion.Height = height;
            Canvas.SetLeft(this.loopRegion, selectedLeft);
            Canvas.SetTop(this.loopRegion, 0);

            SetLine(this.startHandleLine, selectedLeft, height);
            SetLine(this.endHandleLine, selectedRight, height);
            SetLine(this.playheadLine, playheadX, height);

            SetThumb(this.startHandleThumb, selectedLeft, height, ThumbPlacement.Top);
            SetThumb(this.endHandleThumb, selectedRight, height, ThumbPlacement.Bottom);
            SetThumb(this.playheadThumb, playheadX, height, ThumbPlacement.Top);
        }

        private static void SetLine(Line line, double x, double height)
        {
            line.X1 = x;
            line.X2 = x;
            line.Y1 = 0;
            line.Y2 = height;
        }

        private static void SetThumb(Polygon thumb, double x, double height, ThumbPlacement placement)
        {
            thumb.Points = placement == ThumbPlacement.Top
                ? new PointCollection
                {
                    new Point(x - 7, 0),
                    new Point(x + 7, 0),
                    new Point(x, 11)
                }
                : new PointCollection
                {
                    new Point(x - 7, height),
                    new Point(x + 7, height),
                    new Point(x, height - 11)
                };
        }

        private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.loopPoints is null)
            {
                return;
            }

            var x = e.GetPosition(OverlayCanvas).X;
            this.dragHandle = GetDragHandle(x);
            OverlayCanvas.CaptureMouse();
            ApplyDrag(x);
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.loopPoints is null)
            {
                return;
            }

            var x = e.GetPosition(OverlayCanvas).X;
            if (this.dragHandle == DragHandle.None)
            {
                OverlayCanvas.Cursor = GetDragHandle(x) is DragHandle.Start or DragHandle.End
                    ? Cursors.SizeWE
                    : Cursors.Hand;
                return;
            }

            ApplyDrag(x);
        }

        private void OverlayCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.dragHandle == DragHandle.None)
            {
                OverlayCanvas.Cursor = Cursors.Arrow;
            }
        }

        private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.dragHandle = DragHandle.None;
            OverlayCanvas.ReleaseMouseCapture();
        }

        private void OverlayCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
            => DrawEditor();

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
            else if (this.dragHandle == DragHandle.Playhead)
            {
                SeekPlayback(seconds);
                return;
            }

            this.loopPoints = NormalizeLoop(this.loopPoints);
            UpdateTextBoxes();
            UpdateOverlayVisuals();
            LoopPointsChanged?.Invoke(this.loopPoints.Clone());
        }

        private DragHandle GetDragHandle(double x)
        {
            if (this.loopPoints is null)
            {
                return DragHandle.None;
            }

            var startDistance = Math.Abs(x - SecondsToX(this.loopPoints.StartSeconds));
            var endDistance = Math.Abs(x - SecondsToX(this.loopPoints.EndSeconds));
            var playheadDistance = Math.Abs(x - SecondsToX(this.playheadSeconds));
            var nearestDistance = Math.Min(startDistance, Math.Min(endDistance, playheadDistance));

            if (nearestDistance <= HandleHitWidth)
            {
                if (nearestDistance == startDistance)
                {
                    return DragHandle.Start;
                }

                if (nearestDistance == endDistance)
                {
                    return DragHandle.End;
                }
            }

            return DragHandle.Playhead;
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
            this.playheadSeconds = Clamp(this.playheadSeconds, 0, this.durationSeconds);
            UpdateTextBoxes();
            UpdateOverlayVisuals();
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
            SeekPlayback(this.loopPoints.StartSeconds);
            UpdateTextBoxes();
            UpdateOverlayVisuals();
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
                CurrentTime = TimeSpan.FromSeconds(Clamp(this.playheadSeconds, 0, this.durationSeconds))
            };
            this.playbackOutput = new WaveOutEvent();
            this.playbackOutput.Init(this.playbackReader);
            this.playbackOutput.Play();
            this.playbackTimer.Start();
            UpdateOverlayVisuals();
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

        private void SeekPlayback(double seconds)
        {
            this.playheadSeconds = Clamp(seconds, 0, this.durationSeconds);
            if (this.playbackReader is not null)
            {
                this.playbackReader.CurrentTime = TimeSpan.FromSeconds(this.playheadSeconds);
            }

            UpdateOverlayVisuals();
        }

        private void PlaybackTimer_Tick(object? sender, EventArgs e)
        {
            if (this.playbackReader is null || this.loopPoints is null)
            {
                StopPlayback();
                return;
            }

            this.playheadSeconds = Clamp(this.playbackReader.CurrentTime.TotalSeconds, 0, this.durationSeconds);
            UpdateOverlayVisuals();

            if (this.playheadSeconds < this.loopPoints.EndSeconds)
            {
                return;
            }

            SeekPlayback(this.loopPoints.StartSeconds);
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

            return seconds / this.durationSeconds * OverlayCanvas.ActualWidth;
        }

        private double XToSeconds(double x)
        {
            if (OverlayCanvas.ActualWidth <= 0)
            {
                return 0;
            }

            return Clamp(x / OverlayCanvas.ActualWidth * this.durationSeconds, 0, this.durationSeconds);
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
            End,
            Playhead
        }

        private enum ThumbPlacement
        {
            Top,
            Bottom
        }
    }
}
