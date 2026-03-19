using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GK_Antenna.Models;
using Newtonsoft.Json;

namespace GK_Antenna
{
    public partial class StatusPage : Page
    {
        private ClientWebSocket ws = new ClientWebSocket();

        public StatusPage()
        {
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

                while (ws.State == WebSocketState.Open)
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

                            if (response?.antennaData == null)
                                return;

                            UpdateConnectionBoxUI(response.antennaData);

                            UpdateTemperatureBoxUI(response.antennaData);

                            UpdateVoltageBoxUI(response.antennaData);

                            UpdateCurrentBoxUI(response.antennaData);

                            UpdateEsNoBoxUI(response.antennaData, response.multiModeReceiverData);
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

        //원래배경 색깔(하늘색)
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

            // 원
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

            // 10도 눈금 (36개)
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
        }


    }
}