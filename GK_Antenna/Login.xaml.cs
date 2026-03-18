using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

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
            string port = AntennaPortBox.Text?.Trim() ?? "";

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

            if (!int.TryParse(port, out int portNumber) || portNumber < 0 || portNumber > 65535)
            {
                portmsg.Content = "Please Check The Port Number(0~65535)";
                isValid = false;
            }
            else
            {
                portmsg.Content = "";
            }

            if (!isValid)
                return;

            OkButton.IsEnabled = false;

            try
            {
                StartWebServer();
                await Task.Delay(1000);

                ApiService api = new ApiService();
                Root result = await api.Connect(ip, port);

                bool isSuccess = result.code == 0 ||
                                 (result.msg != null && result.msg.Contains("repeat"));

                if (isSuccess)
                {
                    MessageBox.Show(result.msg.Contains("repeat")
                        ? "Already Connected"
                        : "Connection Success");

                    NavigationService?.Navigate(new StatusPage());
                }
                else
                {
                    MessageBox.Show($"Connection Failed: {result.msg}");
                    OkButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection Error: {ex.Message}");
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

        private void portbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (portmsg != null)
            {
                if (AntennaPortBox.Text == "")
                {
                    portmsg.Content = "Please Enter Port Number";
                }
                else
                {
                    Regex regex = new Regex("^(?:[0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$");
                    bool isMatch = regex.IsMatch(AntennaPortBox.Text);
                    portmsg.Content = isMatch ? "" : "Please Check The Port Number(0~65535)";
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
    }
}