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

        // 1. 매개변수 없는 기본 생성자 (MainWindow에서 호출할 때 대비)
        public StatusPage() : this(true) { }

        // 2. 핵심 생성자: 페이지 이동 시 호출됨
        public StatusPage(bool isConnect)
        {
            InitializeComponent(); // UI 요소 초기화 (가장 먼저)
            DataContext = this;
            _currentTime = DateTime.Now;

            // 그래프 시리즈 초기 설정
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

            // 타이머 초기 설정 (1초 간격)
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += UpdateData;

            // 나침반 및 바늘 생성 로직
            DrawCompass();
            CreateNeedle();


           

            // 연결 상태일 때 즉시 기동 로직
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
            string logPath = @"C:\GlobalKonet SW\GK antenna SW\GK_NewAntennaProgram\GK_Antenna\GK_Antenna\WebServer\log\server.log";

            Task.Run(() =>
            {
                try
                {
                    using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream, Encoding.Default)) // 한글 깨짐 방지 위해 Default 권장
                    {
                        // [수정] 무조건 끝으로 가지 말고, 끝에서 약 5000바이트(약 20~50줄) 전으로 이동
                        long startPos = Math.Max(0, stream.Length - 5000);
                        stream.Seek(startPos, SeekOrigin.Begin);

                        // 첫 줄은 중간부터 읽힐 수 있으니 한 번 버림
                        if (startPos > 0) reader.ReadLine();

                        while (true)
                        {
                            string line = reader.ReadLine();
                            if (line != null)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    openlistbox.Items.Add(line);
                                    // 자동 스크롤
                                    if (openlistbox.Items.Count > 0)
                                        openlistbox.ScrollIntoView(openlistbox.Items[openlistbox.Items.Count - 1]);
                                });
                            }
                            else
                            {
                                Thread.Sleep(200); // CPU 점유율 방지
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
            // 이벤트 재등록
            ApiService.Instance.OnDataReceived -= HandleAntennaData;
            ApiService.Instance.OnDataReceived += HandleAntennaData;

            // ⭐ 마지막 데이터 즉시 반영
            if (ApiService.Instance.CurrentData != null)
            {
                HandleAntennaData(ApiService.Instance.CurrentData);
            }

            // 타이머 재시작
            if (_timer != null && !_timer.Enabled)
                _timer.Start();
        }

        private void HandleAntennaData(Root2 response)
        {
            // 수신된 데이터를 UI 스레드에서 업데이트
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

                // 만약 타이머가 멈춰있다면 다시 시작
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
            iroll.Foreground= Brushes.White;
            ihead.Foreground= Brushes.White;

            ipitch.Text = imuData.imuPitch.ToString("0.00");
            iroll.Text = imuData.imuRoll.ToString("0.00");
            double yaw = imuData.imuYaw;
            ihead.Text = (yaw >= 359.99 ? 0 : yaw).ToString("0.00");
        }


        public void UpdateVoltageText(AntennaData data)
        {
            double voltage = data.antennaVoltage;
            VoltageText.Text = $"{voltage:F1} V";
            

            if(data.antennaState != 0)
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
            double size = Math.Min(centerX, centerY) * 0.6; // 바늘 길이

            needleTop = new Polygon
            {
                Points = new PointCollection
        {
            new Point(centerX,          centerY - size),
            new Point(centerX - 15,     centerY),
            new Point(centerX + 15,     centerY)
        },
                Fill = Brushes.Red
            };

            needleBottom = new Polygon
            {
                Points = new PointCollection
        {
            new Point(centerX,          centerY + size),
            new Point(centerX - 15,     centerY),
            new Point(centerX + 15,     centerY)
        },
                Fill = Brushes.Blue
            };

            needleRotate = new RotateTransform(0, centerX, centerY);
            needleTop.RenderTransform = needleRotate;
            needleBottom.RenderTransform = needleRotate;

            CompassCanvas.Children.Add(needleBottom);
            CompassCanvas.Children.Add(needleTop);
        }

        public void UpdateCompass(double azimuth)
        {
            needleRotate.Angle = azimuth;
            double displayAzimuth = azimuth >= 359.99 ? 0 : azimuth;
            AzimuthText.Text = $"Azimuth: {displayAzimuth:F1}°";
        }

        public void UpdateAttitude(double roll, double pitch, double yaw)
        {
            // 1. Roll (회전) 연동
            if (RollTransform != null) RollTransform.Angle = roll;

            // 2. Pitch (배경 및 수평선) 연동
            if (SkyRect != null && GroundRect != null && HorizonLine != null)
            {
                // 눈금 일치: 10도 눈금이 중앙(150)에서 30px 떨어져 있으므로 1도당 3px 이동
                double offset = pitch * 3;

                if (pitch >= 30)
                {
                    // 30도 이상: 전체 하늘색 (단색)
                    SkyRect.Height = 300;
                    GroundRect.Height = 0;
                    HorizonLine.Visibility = Visibility.Collapsed;
                }
                else if (pitch <= -30)
                {
                    // -30도 이하: 전체 땅색 (단색)
                    SkyRect.Height = 0;
                    GroundRect.Height = 300;
                    HorizonLine.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // 범위 내: 수평선이 눈금을 가리키며 배경 비율 조절
                    double newSkyHeight = 150 + offset;
                    SkyRect.Height = newSkyHeight;
                    GroundRect.Height = 300 - newSkyHeight;

                    // 수평선을 하늘/땅 경계선에 위치시킴
                    Canvas.SetTop(HorizonLine, newSkyHeight - 1.5);
                    HorizonLine.Visibility = Visibility.Visible;
                }
            }

            // 3. 텍스트 정보 업데이트
            if (RollValueText != null) RollValueText.Text = $"{roll:F1}°";
            if (PitchValueText != null) PitchValueText.Text = $"{pitch:F1}°";
            if (yaw >= 359.99)
                yaw = 0;

            if (YawValueText != null)
                YawValueText.Text = $"{yaw:F1}°";
          }
        

        // 위에서 만든 메서드를 호출하는 래퍼 메서드 (기존 구조 유지)
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

            const double barBottom = 354.0;   // 튜브 내부 바닥 (Canvas.Top 고정)
            const double minTempY = 350.0;    // -60 눈금 Y위치
            const double maxTempY = 0.0;      // 80 눈금 Y위치

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

            const double barBottom = 354.0;      // Canvas.Top 고정 (튜브 바닥)
            const double minValY = 326.67;       // -120 눈금 Y위치
            const double maxValY = 0.0;          // 30 눈금 Y위치

            // -120일 때 바가 튜브 바닥(354)에서 -120 눈금(326.67)까지만 채움
            const double bottomOffset = barBottom - minValY; // 약 27.33px
            const double totalRangeHeight = minValY - maxValY; // 326.67px

            double clamped = Math.Max(minVal, Math.Min(maxVal, esno));
            double ratio = (clamped - minVal) / (maxVal - minVal);

            // esno=-120 이면 bottomOffset만큼, esno=30 이면 354px
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
                //string message = e.Data;
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
                    // _currentValue에 실시간 데이터 소스로부터의 값을 할당
                    // _currentValue = GetRealTimeValue(); // 예: 센서 데이터 또는 외부 API 호출

                    // 3초마다 values, timestamps 초기화
                    if (_currentTime.Second % 40 == 0)
                    {
                        _values.Clear();
                        _timeStamps.Clear();
                    }


                    // _currentValue 값을 그래프에 추가'
                    _values.Add(_currentValue);
                    _timeStamps.Add(_currentTime.ToString("HH:mm:ss"));
                    // 오래된 데이터를 제거하여 일정 개수만 유지
                    /*if (_values.Count > 15)
                    {
                        _values.RemoveAt(0);
                    }*/

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
            // XAxes의 Labels를 최신 시간 값으로 업데이트
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
                    // -100 <= value <= -30 : 간격 10
                    min = -100;
                    step = 20;
                }
                else if (value < -100)
                {
                    // value < -100 : 간격 30
                    min = value;
                    step = 30;
                }
                else
                {
                    // -30 < value < 0 : 간격 5, 최소값 -30
                    min = -30;
                    step = 5;
                }

                max = 5; // 음수일 때 최대값 고정
            }
            else
            {
                step = 2;

                if (value > 10)
                {
                    // value > 10 : 최소값 = value - 10, 최대값 = value
                    min = value - 10;
                    max = value;
                }
                else
                {
                    // 0 <= value <= 10 : 최소값 = 0, 최대값 = 10
                    min = 0;
                    max = 10;
                }
            }

            // Y축 적용
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
                SpeedometerCanvas.Children.Insert(0, rect); // 바늘 아래에 삽입
            }
        }

        private void DrawLabels()
        {
            // (속도값, Canvas.Left, Canvas.Top)
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
                SpeedometerCanvas.Children.Insert(0, tb); // 바늘 아래에 삽입
            }
        }

        private void TopBar_MouseEnter(object sender, MouseEventArgs e)
        {
            DropBar.Visibility = Visibility.Visible;
        }

        private void TopBar_MouseLeave(object sender, MouseEventArgs e)
        {
            // 바로 사라지지 않게 약간 딜레이 느낌 필요하면 나중에 개선 가능
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
