using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GK_Antenna.Models;
using Newtonsoft.Json;
using WebSocketSharp;

namespace GK_Antenna
{
    internal class ApiService
    {
        private static readonly ApiService _instance = new ApiService();
        public static ApiService Instance => _instance;

        private WebSocketSharp.WebSocket ws;

        public event Action<Root2> OnDataReceived;
        public Root2 CurrentData { get; private set; }

        private readonly HttpClient client = new HttpClient();

        private ApiService() { }

        public Task StartWebSocket()
        {
            return Task.Run(async () =>
            {
                try
                {
                    await WebSocket();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"웹소켓 오류: {ex.Message}");
                    });
                }
            });
        }

        public async Task WebSocket()
        {
            try
            {
                ws = new WebSocketSharp.WebSocket("ws://localhost:9999/wsApi");

                ws.OnMessage += webMessage;
                ws.Connect();
                //Console.ReadKey(true);
                await Task.Delay(Timeout.Infinite);


            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹소켓 작업2 중 오류 발생: {ex.Message}");
            }

        }

        public void webMessage(object sender, MessageEventArgs e)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<Root2>(e.Data);

                if (response != null)
                {
                    CurrentData = response;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnDataReceived?.Invoke(response);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JSON 파싱 오류: {ex.Message}");
            }
        }





        public async Task Disconnect()
        {
            string url = "http://127.0.0.1:9999/api/deviceDisconnect";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<Root>(json);

                if (result.code == 0)
                {
                    Console.WriteLine("연결 종료 성공");
                }
                else
                {
                    Console.WriteLine("이미 연결 없음");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disconnect 에러: " + ex.Message);
            }
        }

        public async Task<Root> Connect(string ip, string port)
        {
            string url = $"http://127.0.0.1:9999/api/connectByLAN?host={ip}&port={port}";

            try
            {
                await Disconnect();

                await Task.Delay(3000);

                HttpResponseMessage response = await client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<Root>(json);

                return result;
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    $"[HttpRequestException]\n\n" +
                    $"Message:\n{ex.Message}\n\n" +
                    $"Inner Exception:\n{ex.InnerException?.Message}\n\n" +
                    $"Request URL:\n{url}",
                    "API Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                throw;
            }
            catch (TaskCanceledException ex)
            {
                MessageBox.Show(
                    $"[Timeout Error]\n\n" +
                    $"Message:\n{ex.Message}\n\n" +
                    $"Request URL:\n{url}",
                    "Timeout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"[General Exception]\n\n" +
                    $"Message:\n{ex.Message}\n\n" +
                    $"StackTrace:\n{ex.StackTrace}\n\n" +
                    $"Inner Exception:\n{ex.InnerException?.Message}",
                    "Unknown Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                throw;
            }
        }



    }
}