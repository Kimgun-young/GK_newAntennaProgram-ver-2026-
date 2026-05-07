using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GK_Antenna.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using WebSocketSharp;

namespace GK_Antenna
{
    public partial class StatusPage : Page, INotifyPropertyChanged
    {
        private DateTime _currentTime;
        private static double _currentValue;
        private ObservableCollection<double> _values;
        private ObservableCollection<string> _timeStamps = new ObservableCollection<string>();
        private System.Timers.Timer _timer;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<ISeries> Series { get; set; }

        public StatusPage() : this(true) { }

        public StatusPage(bool isConnect)
        {
            InitializeComponent();
            DataContext = this;
            _currentTime = DateTime.Now;

            _values = new ObservableCollection<double> { _currentValue };
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<double> {
                    Values = _values,
                    Stroke = new SolidColorPaint(SKColor.Parse("#FFA500"), 3),
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null
                }
            };

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += UpdateData;

            DrawCompass();
            CreateNeedle();

            if (isConnect)
            {
                ApiService.Instance.StartWebSocket();

                if (!_timer.Enabled) _timer.Start();

                _ = WebSocket2();

                StartLatestLogMonitoring();

            }

            this.Loaded += StatusPage_Loaded;

        }

        public void StartLatestLogMonitoring()
        {
            string logPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "WebServer",
                "log",
                "server.log");
            Task.Run(() =>
            {
                try
                {
                    using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream, Encoding.Default))
                    {
                        long startPos = Math.Max(0, stream.Length - 5000);
                        stream.Seek(startPos, SeekOrigin.Begin);

                        if (startPos > 0) reader.ReadLine();

                        while (true)
                        {
                            string line = reader.ReadLine();
                            if (line != null)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    openlistbox.Items.Add(line);
                                    if (openlistbox.Items.Count > 0)
                                        openlistbox.ScrollIntoView(openlistbox.Items[openlistbox.Items.Count - 1]);
                                });
                            }
                            else
                            {
                                Thread.Sleep(200);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(() => openlistbox.Items.Add("에러 발생: " + ex.Message));
                }
            });
        }

        private void StatusPage_Loaded(object sender, RoutedEventArgs e)
        {
            ApiService.Instance.OnDataReceived -= HandleAntennaData;
            ApiService.Instance.OnDataReceived += HandleAntennaData;

            if (ApiService.Instance.CurrentData != null)
            {
                HandleAntennaData(ApiService.Instance.CurrentData);
            }

            if (_timer != null && !_timer.Enabled)
                _timer.Start();
        }

        private void HandleAntennaData(Root2 response)
        {
            Dispatcher.Invoke(() =>
            {
                if (response?.antennaData == null) return;

                UpdateConnectionBoxUI(response.antennaData);
                UpdateTemperatureBoxUI(response.antennaData);
                UpdateVoltageText(response.antennaData);
                UpdateCurrentText(response.antennaData);
                UpdateEsNoBoxUI(response.antennaData, response.multiModeReceiverData);
                UpdateSpeedGauge(response.gnssData.gpsSpeed);
                UpdateGPSBoxUI(response.antennaData, response.gnssData);
                UpdateIMUBoxUI(response.antennaData, response.imuData);
                _currentValue = response.multiModeReceiverData.mmrCnrPower;

                UpdateAttitude(
                    response.imuData.imuRoll,
                    response.imuData.imuPitch,
                    response.imuData.imuYaw
                );

                UpdateCompass(response.imuData.imuYaw);

                if (!_timer.Enabled) _timer.Start();
            });
        }


        private Brush DefaultBrush = new SolidColorBrush(
        (Color)ColorConverter.ConvertFromString("#121212"));

        public void UpdateConnectionBoxUI(AntennaData data)
        {
            if (data.antennaState == 0)
            {
                ConnectionText.Text = "OK";
                ConnectionText.Foreground = Brushes.Lime;
                ConnectionBox.Background = DefaultBrush;
                ConnectionBox.BorderBrush = Brushes.Lime;
            }
            else
            {
                ConnectionText.Text = "---";
                ConnectionText.Foreground = Brushes.Red;
                ConnectionBox.Background = DefaultBrush;
            }
        }

        public void UpdateTemperatureBoxUI(AntennaData data)
        {
            double temp = data.antennaTemperature;

            TemperatureBox.Background = DefaultBrush;

            if (data.antennaState != 0)
            {
                TemperatureText.Text = "---";
                TemperatureText.Foreground = Brushes.Red;
            }
            else if (temp >= 60 || temp <= -10)
            {
                TemperatureText.Text = $"{temp:F1}°C";
                TemperatureText.Foreground = Brushes.Red;
            }
            else
            {
                TemperatureText.Text = $"{temp:F1}°C";
                TemperatureText.Foreground = Brushes.Lime;
            }

            UpdateTemperatureGauge(temp);
        }

        public void UpdateGPSBoxUI(AntennaData antennaData, GnssData gnssData)
        {
            if (antennaData.antennaState != 0)
            {
                glong.Foreground = Brushes.Red;
                glat.Foreground = Brushes.Red;
                galt.Foreground = Brushes.Red;

                glong.Text = "---";
                glat.Text = "---";
                galt.Text = "---";

                GPS_Label.Foreground = Brushes.Red;
                GPS_Label.Text = "GPS(Off)";

                return;
            }

            glong.Foreground = Brushes.White;
            glat.Foreground = Brushes.White;
            galt.Foreground = Brushes.White;

            if (gnssData.state == 1)
            {
                glong.Text = gnssData.gpsLongitude.ToString("0.00");
                glat.Text = gnssData.gpsLatitude.ToString("0.00");
            }
            else
            {
                glat.Text = 37.392402.ToString("0.00");
                glong.Text = 126.959046.ToString("0.00");
            }

            galt.Text = gnssData.gpsAltitude.ToString("0.00");

            if (gnssData.state == 0)
            {
                GPS_Label.Foreground = Brushes.Red;
                GPS_Label.Text = "GPS(Invalid)";
            }
            else if (gnssData.state == 1)
            {
                GPS_Label.Foreground = Brushes.Lime;
                GPS_Label.Text = "GPS(Valid)";
            }
            else
            {
                GPS_Label.Foreground = Brushes.Red;
                GPS_Label.Text = "GPS(Disconnect)";
            }
        }

        public void UpdateIMUBoxUI(AntennaData antennaData, ImuData imuData)
        {
            if (antennaData.antennaState != 0)
            {
                ipitch.Foreground = Brushes.Red;
                ipitch.Text = "---";

                iroll.Foreground = Brushes.Red;
                iroll.Text = "---";

                ihead.Foreground = Brushes.Red;
                ihead.Text = "---";

                return;
            }

            ipitch.Foreground = Brushes.White;
            iroll.Foreground = Brushes.White;
            ihead.Foreground = Brushes.White;

            ipitch.Text = imuData.imuPitch.ToString("0.00");
            iroll.Text = imuData.imuRoll.ToString("0.00");
            double yaw = imuData.imuYaw;
            ihead.Text = (yaw >= 359.99 ? 0 : yaw).ToString("0.00");
        }


        public void UpdateVoltageText(AntennaData data)
        {
            double voltage = data.antennaVoltage;
            VoltageText.Text = $"{voltage:F1} V";


            if (data.antennaState != 0)
            {
                VoltageText.Foreground = Brushes.Red;
                VoltageText.Text = "---";
            }
        }

        public void UpdateCurrentText(AntennaData data)
        {
            double current = data.antennaElectricity;
            CurrentText.Text = $"{current:F1} A";


            if (data.antennaState != 0)
            {
                CurrentText.Foreground = Brushes.Red;
                CurrentText.Text = "---";
            }
        }

        public void UpdateEsNoBoxUI(AntennaData antenna, MultiModeReceiverData mmr)
        {
            double esno = mmr.mmrCnrPower;

            this.Dispatcher.Invoke(() =>
            {
                EsNoBox.Background = DefaultBrush;

                if (antenna.antennaState != 0)
                {
                    EsNoText.Text = "---";
                    EsNoText.Foreground = Brushes.Red;
                }
                else
                {
                    EsNoText.Text = $"{esno:F1}";

                    bool isNormal = (esno >= -110 && esno <= -11) || (esno >= -9);

                    if (isNormal)
                    {
                        EsNoText.Foreground = Brushes.Lime;
                    }
                    else
                    {
                        EsNoText.Foreground = Brushes.Red;
                    }
                }

                UpdateEsNoGauge(antenna, mmr);
            });


        }

        public void DrawCompass()
        {
            double centerX = CompassCanvas.Width / 2;
            double centerY = CompassCanvas.Height / 2;
            double radius = Math.Min(centerX, centerY) - 10;

            var circle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = Brushes.White,
                StrokeThickness = 3,

            };
            Canvas.SetLeft(circle, centerX - radius);
            Canvas.SetTop(circle, centerY - radius);
            CompassCanvas.Children.Add(circle);

            for (int i = 0; i < 360; i += 10)
            {
                double angleRad = i * Math.PI / 180;
                double inner = (i % 30 == 0) ? radius - 20 : radius - 10;

                var tick = new Line
                {
                    X1 = centerX + inner * Math.Sin(angleRad),
                    Y1 = centerY - inner * Math.Cos(angleRad),
                    X2 = centerX + radius * Math.Sin(angleRad),
                    Y2 = centerY - radius * Math.Cos(angleRad),
                    Stroke = Brushes.White,
                    StrokeThickness = (i % 30 == 0) ? 3 : 1
                };
                CompassCanvas.Children.Add(tick);
            }

            AddDirectionLabels(centerX, centerY, radius);
        }

        private void AddDirectionLabels(double centerX, double centerY, double radius)
        {
            double offset = 40;

            AddDirection("N", centerX, centerY - radius + offset);
            AddDirection("S", centerX, centerY + radius - offset);
            AddDirection("E", centerX + radius - offset, centerY);
            AddDirection("W", centerX - radius + offset, centerY);
        }

        private void AddDirection(string text, double x, double y)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = tb.DesiredSize;

            Canvas.SetLeft(tb, x - size.Width / 2);
            Canvas.SetTop(tb, y - size.Height / 2);

            CompassCanvas.Children.Add(tb);
        }

        private Polygon needleTop;
        private Polygon needleBottom;
        private RotateTransform needleRotate;

        public void CreateNeedle()
        {
            double centerX = CompassCanvas.Width / 2;
            double centerY = CompassCanvas.Height / 2;
            double size = Math.Min(centerX, centerY) * 0.65;
            double width = 14;

            LinearGradientBrush redGradient = new LinearGradientBrush();
            redGradient.StartPoint = new Point(0, 0.5);
            redGradient.EndPoint = new Point(1, 0.5);
            redGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 50, 50), 0.0));
            redGradient.GradientStops.Add(new GradientStop(Color.FromRgb(220, 0, 0), 0.5));
            redGradient.GradientStops.Add(new GradientStop(Color.FromRgb(200, 0, 0), 1.0));

            LinearGradientBrush blueGradient = new LinearGradientBrush();
            blueGradient.StartPoint = new Point(0, 0.5);
            blueGradient.EndPoint = new Point(1, 0.5);
            blueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(50, 50, 255), 0.0));
            blueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 220), 0.5));
            blueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 180), 1.0));

            needleTop = new Polygon
            {
                Points = new PointCollection
        {
            new Point(centerX,         centerY - size),
            new Point(centerX - width, centerY + 0.5),
            new Point(centerX + width, centerY + 0.5)
        },
                Fill = redGradient,
                StrokeThickness = 0
            };

            needleBottom = new Polygon
            {
                Points = new PointCollection
        {
            new Point(centerX,         centerY + size),
            new Point(centerX - width, centerY - 0.5),
            new Point(centerX + width, centerY - 0.5)
        },
                Fill = blueGradient,
                StrokeThickness = 0
            };

            needleRotate = new RotateTransform(0, centerX, centerY);

            needleTop.RenderTransform = needleRotate;
            needleBottom.RenderTransform = needleRotate;

            CompassCanvas.Children.Add(needleBottom);
            CompassCanvas.Children.Add(needleTop);

            Ellipse centerPin = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Silver,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 1,
                Margin = new Thickness(centerX - 3, centerY - 3, 0, 0)
            };
            CompassCanvas.Children.Add(centerPin);
        }


        public void UpdateCompass(double azimuth)
        {
            needleRotate.Angle = azimuth;
            double displayAzimuth = azimuth >= 359.99 ? 0 : azimuth;
            AzimuthText.Text = $"Azimuth: {displayAzimuth:F1}°";
        }

        public void UpdateAttitude(double roll, double pitch, double yaw)
        {
            if (RollTransform != null) RollTransform.Angle = roll;

            if (SkyRect != null && GroundRect != null && HorizonLine != null)
            {
                double offset = pitch * 3;

                if (pitch >= 30)
                {
                    SkyRect.Height = 300;
                    GroundRect.Height = 0;
                    HorizonLine.Visibility = Visibility.Collapsed;
                }
                else if (pitch <= -30)
                {
                    SkyRect.Height = 0;
                    GroundRect.Height = 300;
                    HorizonLine.Visibility = Visibility.Collapsed;
                }
                else
                {
                    double newSkyHeight = 150 + offset;
                    SkyRect.Height = newSkyHeight;
                    GroundRect.Height = 300 - newSkyHeight;

                    Canvas.SetTop(HorizonLine, newSkyHeight - 1.5);
                    HorizonLine.Visibility = Visibility.Visible;
                }
            }

            if (RollValueText != null) RollValueText.Text = $"{roll:F1}°";
            if (PitchValueText != null) PitchValueText.Text = $"{pitch:F1}°";
            if (yaw >= 359.99)
                yaw = 0;

            if (YawValueText != null)
                YawValueText.Text = $"{yaw:F1}°";
        }


        public void UpdateAttitude(ImuData data)
        {
            if (data == null) return;
            UpdateAttitude(data.imuRoll, data.imuPitch, data.imuYaw);
        }

        private void UpdateTemperatureGauge(double temp)
        {
            if (TempLevelBar == null) return;

            const double minTemp = -60;
            const double maxTemp = 80;

            const double barBottom = 354.0;
            const double minTempY = 350.0;
            const double maxTempY = 0.0;

            const double bottomOffset = barBottom - minTempY;
            const double totalRangeHeight = minTempY - maxTempY;

            double clamped = Math.Max(minTemp, Math.Min(maxTemp, temp));
            double ratio = (clamped - minTemp) / (maxTemp - minTemp);

            double targetHeight = bottomOffset + (ratio * totalRangeHeight);

            Canvas.SetTop(TempLevelBar, barBottom);

            DoubleAnimation anim = new DoubleAnimation
            {
                From = TempLevelBar.ActualHeight,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            TempLevelBar.BeginAnimation(Rectangle.HeightProperty, anim);
        }
        public void UpdateSpeedGauge(double speedValue)
        {
            double targetAngle = speedValue - 120;


            if (targetAngle < -120) targetAngle = -120;
            if (targetAngle > 120) targetAngle = 120;

            Dispatcher.Invoke(() =>
            {
                if (NeedleRotation != null)
                {
                    NeedleRotation.Angle = targetAngle;
                }

                if (speed != null)
                {
                    speed.Text = $"{speedValue:F1} km/h";
                }
            });
        }

        private void UpdateEsNoGauge(AntennaData antenna, MultiModeReceiverData mmr)
        {
            double esno = mmr.mmrCnrPower;
            if (EsNoLevelBar == null) return;

            const double minVal = -120;
            const double maxVal = 30;

            const double barBottom = 354.0;
            const double minValY = 326.67;
            const double maxValY = 0.0;

            const double bottomOffset = barBottom - minValY;
            const double totalRangeHeight = minValY - maxValY;

            double clamped = Math.Max(minVal, Math.Min(maxVal, esno));
            double ratio = (clamped - minVal) / (maxVal - minVal);

            double targetHeight = bottomOffset + (ratio * totalRangeHeight);

            Canvas.SetTop(EsNoLevelBar, barBottom);

            EsNoLevelBar.BeginAnimation(Rectangle.HeightProperty, null);
            DoubleAnimation anim = new DoubleAnimation
            {
                From = EsNoLevelBar.ActualHeight,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            EsNoLevelBar.BeginAnimation(Rectangle.HeightProperty, anim);
        }


        private WebSocketSharp.WebSocket ws2;

        public async Task WebSocket2()
        {


            ws2 = new WebSocketSharp.WebSocket("ws://localhost:9999/wsMessage");

            //ws.OnMessage;
            ws2.OnMessage += amipMessage2;
            ws2.Connect();
            // Console.ReadKey(true);
            await Task.Delay(Timeout.Infinite);


        }

        private void amipMessage2(object sender, MessageEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate ()
            {
                openlistbox.Items.Add(e.Data);
            }));
        }

        private void amipClearDown(object sender, MouseEventArgs e)
        {

            openlistbox.Items.Clear();

        }

        public Axis[] XAxes { get; set; } =
        {
            new Axis
            {
                Labels = new string[] {"00:00"},
                LabelsRotation = 20,

                LabelsPaint = new SolidColorPaint
                {
                    Color = SKColors.White,
                    StrokeThickness = 3
                }
            }
        };

        public Axis[] YAxes { get; set; } =
        {
            new Axis
            {
                MinLimit = null,
                MaxLimit = null,
                MinStep = 2,
                ForceStepToMin = true,
                Labeler = value => value.ToString("0"),

                LabelsPaint = new SolidColorPaint
                {
                    Color = SKColors.White
                },

                SeparatorsPaint = new SolidColorPaint
                {
                    Color = SKColors.White,
                    StrokeThickness = 3
                }
            }
        };
        private void UpdateData(object sender, ElapsedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {

                    _currentTime = _currentTime.AddSeconds(1);

                    if (_currentTime.Second % 40 == 0)
                    {
                        _values.Clear();
                        _timeStamps.Clear();
                    }


                    _values.Add(_currentValue);
                    _timeStamps.Add(_currentTime.ToString("HH:mm:ss"));


                    UpdateChartLimits(_currentValue);

                    OnPropertyChanged(nameof(Series));
                    UpdateXAxes();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"예외 발생: {ex.Message}");
                }
            });
        }

        private void UpdateXAxes()
        {
            XAxes[0].Labels = _timeStamps.ToArray();
        }

        private void UpdateChartLimits(double value)
        {
            double min;
            double max;
            double step;

            if (value < 0)
            {
                if (value >= -100 && value <= -30)
                {
                    min = -100;
                    step = 20;
                }
                else if (value < -100)
                {
                    min = value;
                    step = 30;
                }
                else
                {
                    min = -30;
                    step = 5;
                }

                max = 5;
            }
            else
            {
                step = 2;

                if (value > 10)
                {
                    min = value - 10;
                    max = value;
                }
                else
                {
                    min = 0;
                    max = 10;
                }
            }

            YAxes[0].MinLimit = min;
            YAxes[0].MaxLimit = max;
            YAxes[0].MinStep = step;
        }

        private void SpeedometerCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            DrawTicks();
            DrawLabels();
        }

        private void DrawTicks()
        {
            for (int angle = -120; angle <= 120; angle += 2)
            {
                bool isMajor = angle % 10 == 0;

                var rect = new Rectangle
                {
                    Fill = isMajor ? Brushes.White
                                        : new SolidColorBrush(Color.FromRgb(0xA0, 0xAE, 0xC0)),
                    Width = isMajor ? 3 : 1,
                    Height = isMajor ? 20 : 8,
                    RenderTransformOrigin = new Point(0.5, isMajor ? 8 : 20),
                    RenderTransform = new RotateTransform(angle)
                };

                Canvas.SetLeft(rect, isMajor ? 223.5 : 224.5);
                Canvas.SetTop(rect, 40);
                SpeedometerCanvas.Children.Insert(0, rect);
            }
        }

        private void DrawLabels()
        {
            var labels = new (int val, double left, double top)[]
            {
                (0,   108, 258), (20,  94,  211), (40,  94,  167),
                (60,  109, 124), (80,  138,  90), (100, 167,  70),
                (120, 208,  64), (140, 254,  70), (160, 291,  92),
                (180, 315, 122), (200, 332, 165), (220, 332, 211),
                (240, 315, 257)
            };

            foreach (var (val, left, top) in labels)
            {
                var tb = new TextBlock
                {
                    Text = val.ToString(),
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Width = val >= 100 ? 31 : double.NaN
                };

                Canvas.SetLeft(tb, left);
                Canvas.SetTop(tb, top);
                SpeedometerCanvas.Children.Insert(0, tb);
            }
        }

        private void TopBar_MouseEnter(object sender, MouseEventArgs e)
        {
            DropBar.Visibility = Visibility.Visible;
        }

        private void TopBar_MouseLeave(object sender, MouseEventArgs e)
        {
            DropBar.Visibility = Visibility.Collapsed;
        }

        private void DropBar_MouseEnter(object sender, MouseEventArgs e)
        {
            DropBar.Visibility = Visibility.Visible;
        }

        private void DropBar_MouseLeave(object sender, MouseEventArgs e)
        {
            DropBar.Visibility = Visibility.Collapsed;
        }

        private void BeamSettingText_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new BeamSettingPage();
        }

        private void IP_SettingText_Click(Object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new IpSettingPage();
        }

        private void StatusText_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new StatusPage();
        }


        private void MapText_Click(System.Object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new MapPage();
        }

        private void CompanyInfoText_Click(System.Object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new CompanyInfoPage();
        }

    }
}