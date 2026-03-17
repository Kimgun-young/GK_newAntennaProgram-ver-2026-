using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GK_Antenna
{
    internal class ApiService
    {
        private static readonly HttpClient client = new HttpClient();

        // 🔥 Disconnect 메소드
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
                // 🔥 일부러 throw 안함 (연결 안돼있어도 계속 진행해야 하니까)
            }
        }

        // 🔥 Connect 메소드 (자동 Disconnect 포함)
        public async Task<Root> Connect(string ip, string port)
        {
            string url = $"http://127.0.0.1:9999/api/connectByLAN?host={ip}&port={port}";

            try
            {
                // ✅ 1. 먼저 기존 연결 끊기
                await Disconnect();

                // (선택) 약간의 딜레이 → 서버 안정화
                await Task.Delay(200);

                // ✅ 2. 새 연결
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