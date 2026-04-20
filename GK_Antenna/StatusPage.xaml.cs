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
                    Stroke = new SolidColorPaint(SKColors.DeepSkyBlue, 2),
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
                UpdateVoltageBoxUI(response.antennaData);
                UpdateCurrentBoxUI(response.antennaData);
                UpdateEsNoBoxUI(response.antennaData, response.multiModeReceiverData);
                UpdateSpeedGauge(response.gnssData.gpsSpeed);

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

        public void UpdateVoltageBoxUI(AntennaData data)
        {
            double voltage = data.antennaVoltage;
            VoltageText.Text = $"{voltage:F1} V";
            VoltageBox.Background = DefaultBrush;

            if(data.antennaState != 0)
            {
                VoltageText.Foreground = Brushes.Red;
                VoltageText.Text = "---";
            }
        }

        public void UpdateCurrentBoxUI(AntennaData data)
        {
            double current = data.antennaElectricity;
            CurrentText.Text = $"{current:F1} A";
            CurrentBox.Background = DefaultBrush;

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
            });
        }

        public void DrawCompass()
        {
            double centerX = 200;
            double centerY = 200;
            double radius = 180;

            var circle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = Brushes.Black,
                StrokeThickness = 3,
                Fill = Brushes.White
            };

            Canvas.SetLeft(circle, centerX - radius);
            Canvas.SetTop(circle, centerY - radius);
            CompassCanvas.Children.Add(circle);

            for (int i = 0; i < 360; i += 10)
            {
                double angleRad = i * Math.PI / 180;

                double inner = (i % 30 == 0) ? radius - 20 : radius - 10;
                double outer = radius;

                double x1 = centerX + inner * Math.Sin(angleRad);
                double y1 = centerY - inner * Math.Cos(angleRad);

                double x2 = centerX + outer * Math.Sin(angleRad);
                double y2 = centerY - outer * Math.Cos(angleRad);

                var tick = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = Brushes.Black,
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
                Foreground = Brushes.Black
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
            needleTop = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(200, 100),
                    new Point(185, 200),
                    new Point(215, 200)
                },
                Fill = Brushes.Red
            };

            needleBottom = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(200, 300),
                    new Point(185, 200),
                    new Point(215, 200)
                },
                Fill = Brushes.Blue
            };

            needleRotate = new RotateTransform(0, 200, 200);

            needleTop.RenderTransform = needleRotate;
            needleBottom.RenderTransform = needleRotate;

            CompassCanvas.Children.Add(needleTop);
            CompassCanvas.Children.Add(needleBottom);
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

            // 1. 온도 범위 설정
            const double minTemp = -60;
            const double maxTemp = 80;

            // 2. 눈금의 물리적 거리 (XAML의 Y축 0 ~ 350 거리와 일치)
            // 몸체 Height가 356이더라도, 눈금이 350 간격으로 그려졌다면 이 값을 350으로 유지해야 합니다.
            const double totalRangeHeight = 350;

            // 3. 온도 제한 (범위를 벗어나지 않게)
            double clampedTemp = Math.Max(minTemp, Math.Min(maxTemp, temp));

            // 4. 높이 비율 계산 
            // (현재온도 - (-60)) / (80 - (-60)) * 350
            double targetHeight = ((clampedTemp - minTemp) / (maxTemp - minTemp)) * totalRangeHeight;

            // 5. 애니메이션 적용
            DoubleAnimation anim = new DoubleAnimation
            {
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            TempLevelBar.BeginAnimation(Rectangle.HeightProperty, anim);
        }
        public void UpdateSpeedGauge(double speedValue)
        {
            // 1. 공식 적용: 220km/h일 때 100도, 240km/h일 때 120도가 나옵니다.
            double targetAngle = speedValue - 120;

            
            if (targetAngle < -120) targetAngle = -120; // 0km/h일 때
            if (targetAngle > 120) targetAngle = 120;   // 240km/h일 때

            // 3. UI 스레드에서 업데이트 수행
            Dispatcher.Invoke(() =>
            {
                // 바늘 회전 업데이트 (XAML의 RotateTransform 이름: NeedleRotation)
                if (NeedleRotation != null)
                {
                    NeedleRotation.Angle = targetAngle;
                }

                // 디지털 수치 텍스트 업데이트 (TextBlock 이름: SpeedText)
                if (speed != null)
                {
                    speed.Text = $"{speedValue:F1} km/h";
                }
            });
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

                Labels = new string[] {"00:00"}, // 초기 레이블 예시
                LabelsRotation = 20,
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
                SeparatorsPaint = new SolidColorPaint
                {
                    Color = SKColors.LightGray, // 가로선 색상
                    StrokeThickness = 1         // 두께
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

                max = 0; // 음수일 때 최대값 고정
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
