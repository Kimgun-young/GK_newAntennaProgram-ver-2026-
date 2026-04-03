using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GK_Antenna.Models;
using Newtonsoft.Json;

namespace GK_Antenna
{
    internal class ApiService
    {
        private static readonly ApiService _instance = new ApiService();
        public static ApiService Instance => _instance;

        private static readonly HttpClient client = new HttpClient();
        private ClientWebSocket ws = new ClientWebSocket();

        // ⭐ 데이터를 페이지들에 전달하기 위한 이벤트 (파싱된 객체를 던짐)
        public event Action<Root2> OnDataReceived;

        // 최신 데이터를 보관 (새 페이지가 열렸을 때 즉시 보여주기 위함)
        public Root2 CurrentData { get; private set; }

        private ApiService() { }

        public async Task StartWebSocket()
        {
            if (ws.State == WebSocketState.Open) return;

            try
            {
                ws = new ClientWebSocket(); // 재연결을 위해 새로 생성
                await ws.ConnectAsync(new Uri("ws://localhost:9999/wsApi"), CancellationToken.None);

                // ⭐ 데이터 수신 루프를 별도 스레드에서 실행
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("웹소켓 연결 오류: " + ex.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 8];
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var messageBuffer = new List<byte>();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        messageBuffer.AddRange(buffer.Take(result.Count));
                    } while (!result.EndOfMessage);

                    string message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                    var response = JsonConvert.DeserializeObject<Root2>(message);

                    if (response != null)
                    {
                        CurrentData = response; // 최신 데이터 업데이트
                        // ⭐ 등록된 모든 페이지(StatusPage 등)에 알림 발송
                        OnDataReceived?.Invoke(response);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("수신 루프 에러: " + ex.Message);
                    break;
                }
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

                await Task.Delay(200);

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<Root>(json);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("API Connection Failed: " + ex.Message);
            }
        }

        

    }
}