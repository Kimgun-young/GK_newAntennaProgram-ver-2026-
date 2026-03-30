using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GK_Antenna
{
    internal class ApiService
    {
        private static readonly HttpClient client = new HttpClient();

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