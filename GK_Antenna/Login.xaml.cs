using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace GK_Antenna
{
    /// <summary>
    /// Login.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
        }


        private string filePath = "ip.txt"; // 저장할 파일 경로
        private string defaultIp = "192.168.0.2"; // 기본 IP 주소

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string inputCode = CompanyCodeBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(inputCode))
            {
                CompanyCodemsg.Content = "Please Enter Company Code";
                return;
            }

            CompanyVerifier verifier = new CompanyVerifier();
            if (!verifier.Verify(inputCode))
            {
                CompanyCodemsg.Content = "Invalid Company Code";
                return;
            }

            bool isInputValid =
                string.IsNullOrEmpty(ipmsg.Content?.ToString()) &&
                string.IsNullOrEmpty(portmsg.Content?.ToString()) &&
                string.IsNullOrEmpty(CompanyCodemsg.Content?.ToString());

            if (!isInputValid)
                return;

            NavigationService?.Navigate(new StatusPage());
        }

        private void CancelButton_Click(Object sender, RoutedEventArgs e)
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


                //System.Windows.MessageBox.Show($"입력값: [{cleanedInput}]\n계산된 해시: {inputHash}\n파일의 해시: {config.companyCodeHash}");

                return inputHash.Equals(config.companyCodeHash?.Trim(), StringComparison.OrdinalIgnoreCase);
            }



            public class CompanyConfig
            {
                public string companyName { get; set; }
                public string companyCodeHash { get; set; }
            }


            private string ComputeSHA256(string input)
            {
                // UTF8 인코딩 시 BOM(Byte Order Mark) 문제가 생기지 않도록 생성
                using (var sha256 = SHA256.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = sha256.ComputeHash(bytes);

                    StringBuilder builder = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        builder.Append(b.ToString("x2")); // 소문자 16진수로 변환
                    }
                    return builder.ToString();
                }
            }
        }

        private void IPTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //ipmsg 로딩까지 기다리기
            if (ipmsg != null)
            {
                if (AntennaIpBox.Text == "")
                {
                    ipmsg.Content = "Please Enter The Ipv4 Address";
                }
                else
                {
                    //텍스트가 있을시
                    Regex regex = new Regex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
                    bool isMatch = regex.IsMatch(AntennaIpBox.Text);

                    if (!isMatch)
                    {
                        ipmsg.Content = "Please Check The Ip Address";
                    }
                    else
                    {
                        ipmsg.Content = "";
                    }

                }
            }

        }

        private void portbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //portmsg 로딩까지 기다리기
            if (portmsg != null)
            {
                if (AntennaPortBox.Text == "")
                {
                    portmsg.Content = "Please Enter Port Number";
                }
                else
                {
                    //텍스트가 있을시
                    Regex regex = new Regex("^(?:[0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$");
                    bool isMatch = regex.IsMatch(AntennaPortBox.Text);

                    if (!isMatch)
                    {
                        //매치실패시
                        portmsg.Content = "Please Check The Port Number(0~65535)";
                    }
                    else
                    {
                        //매치시
                        portmsg.Content = "";
                    }

                }
            }
        }

        private void CompanyCodeBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (CompanyCodemsg == null)
                return;

            if (string.IsNullOrWhiteSpace(CompanyCodeBox.Text))
            {
                CompanyCodemsg.Content = "Please Enter Company Code";
            }
            else
            {
                // 입력 중에는 아무 메시지도 표시하지 않음
                CompanyCodemsg.Content = "";
            }
        }

    }
}
