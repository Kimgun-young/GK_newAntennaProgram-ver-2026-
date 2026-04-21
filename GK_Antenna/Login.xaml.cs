using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace GK_Antenna
{
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
        }

        private string filePath = "ip.txt";
        private string defaultIp = "192.168.0.2";


        private void StartWebServer()
        {
            string batPath = @"C:\GlobalKonet SW\GK antenna SW\GK_NewAntennaProgram\GK_Antenna\GK_Antenna\WebServer\start.bat";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = batPath,
                    WorkingDirectory = Path.GetDirectoryName(batPath),
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebServer 실행 실패: {ex.Message}");
            }
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string companyCode = CompanyCodeBox.Text?.Trim() ?? "";
            string ip = AntennaIpBox.Text?.Trim() ?? "";
            string port = "5500";

            bool isValid = true;

            if (string.IsNullOrWhiteSpace(companyCode))
            {
                CompanyCodemsg.Content = "Please Enter Company Code";
                isValid = false;
            }
            else
            {
                CompanyVerifier verifier = new CompanyVerifier();
                if (!verifier.Verify(companyCode))
                {
                    CompanyCodemsg.Content = "Invalid Company Code";
                    isValid = false;
                }
                else
                {
                    CompanyCodemsg.Content = "";
                }
            }

            Regex ipRegex = new Regex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");

            if (!ipRegex.IsMatch(ip))
            {
                ipmsg.Content = "Please Check The Ip Address";
                isValid = false;
            }
            else
            {
                ipmsg.Content = "";
            }

            

            if (!isValid)
                return;

            OkButton.IsEnabled = false;

            try
            {
                StartWebServer();
                await Task.Delay(1000);

                ApiService api = ApiService.Instance;
                Root result = await api.Connect(ip, port);

                bool isSuccess = result.code == 0 ||
                                 (result.msg != null && result.msg.Contains("repeat"));

                if (isSuccess)
                {
                    string msg = result.msg.Contains("repeat")
                        ? "Already Connected"
                        : "Connection Success";

                    await alertt(@"\ant-design--check-circle-filled (1).png", msg);

                    _ = ApiService.Instance.StartWebSocket();
                    NavigationService?.Navigate(new StatusPage());
                }
                else
                {
                    await alertt(@"\ant-design--close-circle-filled.png", result.msg);
                    OkButton.IsEnabled = true;

                }
            }
            catch (Exception ex)
            {
                await alertt(@"\ant-design--close-circle-filled.png", ex.Message);
                OkButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public class CompanyVerifier
        {
            private readonly string _jsonPath =
                @"C:\GlobalKonet SW\GK antenna SW\GK_NewAntennaProgram\GK_Antenna\GK_Antenna\WebServer\config\CompanyCode.json";

            public bool Verify(string inputCompanyCode)
            {
                if (!File.Exists(_jsonPath)) return false;

                string json = File.ReadAllText(_jsonPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<CompanyConfig>(json);

                if (config == null) return false;

                string cleanedInput = inputCompanyCode.Trim();
                string inputHash = ComputeSHA256(cleanedInput);

                return inputHash.Equals(config.companyCodeHash?.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            public class CompanyConfig
            {
                public string companyName { get; set; }
                public string companyCodeHash { get; set; }
            }

            private string ComputeSHA256(string input)
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = sha256.ComputeHash(bytes);

                    StringBuilder builder = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        builder.Append(b.ToString("x2"));
                    }
                    return builder.ToString();
                }
            }
        }

        private void IPTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ipmsg != null)
            {
                if (AntennaIpBox.Text == "")
                {
                    ipmsg.Content = "Please Enter The Ipv4 Address";
                }
                else
                {
                    Regex regex = new Regex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
                    bool isMatch = regex.IsMatch(AntennaIpBox.Text);
                    ipmsg.Content = isMatch ? "" : "Please Check The Ip Address";
                }
            }
        }

        

        private void CompanyCodeBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (CompanyCodemsg == null)
                return;

            CompanyCodemsg.Content = string.IsNullOrWhiteSpace(CompanyCodeBox.Text)
                ? "Please Enter Company Code"
                : "";
        }


        public async Task alertt(string url, string content)
        {
            string appDirectory = AppContext.BaseDirectory;
            string WebServerPath = System.IO.Path.Combine(appDirectory, "Resources/Images/");

            await Task.Run(() =>
            {
                //버튼누를시 작동시
                this.Dispatcher.Invoke(new Action(delegate ()
                {
                    alertImg.Source = new BitmapImage(new Uri(WebServerPath + url));
                    alertText.Content = content;
                    alert.Visibility = Visibility.Visible;
                }));
                Thread.Sleep(1000);
            });

            alert.Visibility = Visibility.Collapsed;



        }

        private void CompanyCodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}