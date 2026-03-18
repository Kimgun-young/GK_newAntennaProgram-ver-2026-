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
    }
}