using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
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
    public partial class StatusPage : Page
    {
        private ClientWebSocket ws = new ClientWebSocket();

        private DateTime _currentTime;

        private static double _currentValue;

        private ObservableCollection<double> _values;

        private System.Timers.Timer _timer;

        private ObservableCollection<string> _timeStamps = new ObservableCollection<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<ISeries> Series { get; set; }

        public StatusPage()
        {
            DataContext = this;
            _currentTime = DateTime.Now;

            // 초기 데이터 및 시리즈 설정
            _values = new ObservableCollection<double> { _currentValue };
            //선차트
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<double> { Values = _values, Stroke = new SolidColorPaint(SKColors.DeepSkyBlue, 2), Fill = null, GeometryFill = null,
                    GeometryStroke = null }
            };

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += UpdateData;

            InitializeComponent();
            _ = StartWebSocket();
            DrawCompass();
            CreateNeedle();
        }

        public async Task StartWebSocket()
        {
            try
            {
                await ws.ConnectAsync(new Uri("ws://localhost:9999/wsApi"), CancellationToken.None);

                MessageBox.Show("웹소켓 연결됨");

                var buffer = new byte[1024];

                while (ws.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var messageBuffer = new List<byte>();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        messageBuffer.AddRange(buffer.Take(result.Count));

                    } while (!result.EndOfMessage);

                    string message = Encoding.UTF8.GetString(messageBuffer.ToArray());

                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var response = JsonConvert.DeserializeObject<Root2>(message);

                            if (response?.antennaData == null || response?.imuData == null)
                                return;

                            // 상태 UI
                            UpdateConnectionBoxUI(response.antennaData);
                            UpdateTemperatureBoxUI(response.antennaData);
                            UpdateVoltageBoxUI(response.antennaData);
                            UpdateCurrentBoxUI(response.antennaData);
                            UpdateEsNoBoxUI(response.antennaData, response.multiModeReceiverData);
                            UpdateSpeedGauge(response.gnssData.gpsSpeed);

                            double cnrPower = response.multiModeReceiverData.mmrCnrPower;
                            _currentValue = cnrPower;
                            // 인공수평계
                            UpdateAttitude(
                                response.imuData.imuRoll,
                                response.imuData.imuPitch,
                                response.imuData.imuYaw
                            );

                            // 나침반 (Yaw)
                            UpdateCompass(response.imuData.imuYaw);

                            _timer.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("파싱 에러: " + ex.Message);
                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("웹소켓 오류: " + ex.Message);
            }
        }

        private Brush DefaultBrush = new SolidColorBrush(
        (Color)ColorConverter.ConvertFromString("#5A78C9"));

        public void UpdateConnectionBoxUI(AntennaData data)
        {
            if (data.antennaState == 0)
            {
                ConnectionText.Text = "OK";
                ConnectionBox.Background = Brushes.Green;
            }
            else
            {
                ConnectionText.Text = "NO";
                ConnectionBox.Background = Brushes.Red;
            }
        }

        public void UpdateTemperatureBoxUI(AntennaData data)
        {
            double temp = data.antennaTemperature;
            TemperatureText.Text = $"{temp:F1}°C";
            TemperatureBox.Background = data.antennaState == 0 ? DefaultBrush : Brushes.Red;
            UpdateTemperatureGauge(temp);
        }

        public void UpdateVoltageBoxUI(AntennaData data)
        {
            double voltage = data.antennaVoltage;
            VoltageText.Text = $"{voltage:F1} V";
            VoltageBox.Background = data.antennaState == 0 ? DefaultBrush : Brushes.Red;
        }

        public void UpdateCurrentBoxUI(AntennaData data)
        {
            double current = data.antennaElectricity;
            CurrentText.Text = $"{current:F1} A";
            CurrentBox.Background = data.antennaState == 0 ? DefaultBrush : Brushes.Red;
        }

        public void UpdateEsNoBoxUI(AntennaData antenna, MultiModeReceiverData mmr)
        {
            double esno = mmr.mmrCnrPower;
            EsNoText.Text = $"{esno:F1} dB";
            EsNoBox.Background = antenna.antennaState == 0 ? DefaultBrush : Brushes.Red;
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
            AzimuthText.Text = $"Azimuth: {azimuth:F1}°";
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
            if (YawValueText != null) YawValueText.Text = $"{yaw:F1}°";
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

            TempValueText.Text = $"{temp:F1}°C";

            const double minTemp = -60;
            const double maxTemp = 80;
            const double maxHeight = 400; // XAML 온도계 몸체 높이와 일치시킬 것

            double clampedTemp = Math.Max(minTemp, Math.Min(maxTemp, temp));
            double targetHeight = ((clampedTemp - minTemp) / (maxTemp - minTemp)) * maxHeight;

            // 부드러운 움직임을 위한 애니메이션
            DoubleAnimation anim = new DoubleAnimation
            {
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300), // 웹소켓 주기가 빠르면 300ms 정도가 적당함
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

                //   string message = e.Data;
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
            try
            {
                this.NavigationService.Navigate(new Uri("BeamSetting.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("페이지 이동 오류: " + ex.Message);
            }
        }

        private void StatusText_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.NavigationService.Navigate(new Uri("StatusPage.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("페이지 이동 오류: " + ex.Message);
            }
        }

    }
}
