using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GK_Antenna.Models;
using Newtonsoft.Json;

namespace GK_Antenna
{
    /// <summary>
    /// IpSettingPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class IpSettingPage : Page
    {
        public IpSettingPage()
        {
            InitializeComponent();
            GetWorkMode();
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
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new BeamSettingPage();
        }

        private void IP_SettingText_Click(Object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new IpSettingPage();
        }

        private void StatusText_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new StatusPage();

        }

        private void MapText_Click(System.Object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new MapPage();
        }

        private void CompanyInfoText_Click(System.Object sender, MouseButtonEventArgs e)
        {
            MainWindow main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            main.fr.Content = new CompanyInfoPage();
        }


        private void GetWorkMode()
        {

            string WorkApiUrl = "http://localhost:9999/api/executeCommand?commandCode=QueryWorkParam&param={}";

            string response = "";
            //localhost:9999 고정

            try
            {
                // request setting
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(WorkApiUrl);
                request.Method = "GET";
                request.Timeout = 10 * 1000;

                // GET Request & Response
                using (HttpWebResponse res = (HttpWebResponse)request.GetResponse())
                {
                    HttpStatusCode status = res.StatusCode;
                    Stream response_stream = res.GetResponseStream();
                    using (StreamReader read_stream = new StreamReader(response_stream))
                    {
                        response = read_stream.ReadToEnd();
                    }
                }


                //  Console.WriteLine(response);
                Root classRes = JsonConvert.DeserializeObject<Root>(response);
                //Console.WriteLine(classRes.code);
                if (classRes.code == 0)
                {
                    //연결성공
                    Console.WriteLine("Workmode연결성공");
                    string rawdata = classRes.data;
                    string realData = rawdata.Replace("\\", "");
                    // Console.Write(realData);
                    Root3 realResponse = JsonConvert.DeserializeObject<Root3>(realData);


                    amipIP.Text = realResponse.openamipHost;
                    amipPort.Text = realResponse.openamipPort.ToString();
                    amipMask.Text = realResponse.openamipMask.ToString();

                    antennaIP.Text = realResponse.serverHost;
                    antennaMask.Text = realResponse.serverMask.ToString();

                    AppSettings.WorkMode = realResponse.workMode.ToString();
                    AppSettings.TrackMode = realResponse.trackMode.ToString();
                    AppSettings.RollF = realResponse.rollFactor.ToString();
                    AppSettings.MGPS = realResponse.manual_gps.ToString();

                    AppSettings.MLong = realResponse.manual_longitude.ToString();
                    AppSettings.MLat = realResponse.manual_latitude.ToString();
                    AppSettings.MHeight = realResponse.manual_height.ToString();



                    AppSettings.OpenHost = realResponse.openamipHost;
                    AppSettings.OpenPort = realResponse.openamipPort.ToString();
                    AppSettings.OpenMask = realResponse.openamipMask;

                    AppSettings.ServerHost = realResponse.serverHost;
                    AppSettings.ServerPort = realResponse.serverPort.ToString();
                    AppSettings.ServerMask = realResponse.serverMask;

                }
                else if (classRes.code == -1)
                {
                    //연결실패


                }

            }
            catch (Exception ex)
            {

                Console.WriteLine("에러 " + ex);
            }


        }

        private async void ResetAPi()
        {

            string resetApiUrl = "http://localhost:9999/api/executeCommand?commandCode=Reboot&param={mode:3}";

            string response = "";
            //localhost:9999 고정

            try
            {
                // request setting
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(resetApiUrl);
                request.Method = "GET";
                request.Timeout = 10 * 1000;

                // GET Request & Response
                using (HttpWebResponse res = (HttpWebResponse)request.GetResponse())
                {
                    HttpStatusCode status = res.StatusCode;
                    Stream response_stream = res.GetResponseStream();
                    using (StreamReader read_stream = new StreamReader(response_stream))
                    {
                        response = read_stream.ReadToEnd();
                    }
                }


                //  Console.WriteLine(response);
                Root classRes = JsonConvert.DeserializeObject<Root>(response);
                //Console.WriteLine(classRes.code);
                if (classRes.code == 0)
                {




                    await alertt(@"\ant-design--check-circle-filled (1).png", "Restart Success");

                    this.NavigationService.Navigate(new Uri("Login.xaml", UriKind.Relative));

                }
                else if (classRes.code == -1)
                {
                    //연결실패
                    // Console.WriteLine("연결실패");


                    await alertt(@"\ant-design--close-circle-filled.png", "Restart Failed");

                }

            }
            catch (Exception ex)
            {

                await alertt(@"\ant-design--close-circle-filled.png", "Restart Failed");
                Console.WriteLine("에러 " + ex);
            }




        }

        private async void DisconnectApi()
        {
            string disconnectApiUrl = "http://localhost:9999/api/deviceDisconnect";

            string response = "";
            //localhost:9999 고정

            try
            {
                // request setting
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(disconnectApiUrl);
                request.Method = "GET";
                request.Timeout = 10 * 1000;

                // GET Request & Response
                using (HttpWebResponse res = (HttpWebResponse)request.GetResponse())
                {
                    HttpStatusCode status = res.StatusCode;
                    Stream response_stream = res.GetResponseStream();
                    using (StreamReader read_stream = new StreamReader(response_stream))
                    {
                        response = read_stream.ReadToEnd();
                    }
                }


                //  Console.WriteLine(response);
                Root classRes = JsonConvert.DeserializeObject<Root>(response);
                //Console.WriteLine(classRes.code);
                if (classRes.code == 0)
                {
                    //연결성공

                    await alertt(@"\ant-design--check-circle-filled (1).png", "Disconnect Success");

                    this.NavigationService.Navigate(new Uri("Login.xaml", UriKind.Relative));

                }
                else if (classRes.code == -1)
                {
                    //연결실패
                    // Console.WriteLine("연결실패");

                    await alertt(@"\ant-design--close-circle-filled.png", "Disconnect Failed");

                }

            }
            catch (Exception ex)
            {
                await alertt(@"\ant-design--close-circle-filled.png", "Disconnect Failed");
                Console.WriteLine("에러 " + ex);
            }



        }

        private void OpenamipIP_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (amipIpRed != null)
            {
                if (amipIP.Text == "")
                {
                    amipIpRed.Content = "Please Enter The Ipv4 Address";
                }
                else
                {
                    //텍스트가 있을시
                    Regex regex = new Regex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
                    bool isMatch = regex.IsMatch(amipIP.Text);

                    if (!isMatch)
                    {
                        amipIpRed.Content = "Please Check The Ip Address";
                    }
                    else
                    {
                        amipIpRed.Content = "";
                    }

                }
            }


        }


        private void OpenamipPort_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (amipPortRed != null)
            {
                if (amipPort.Text == "")
                {
                    amipPortRed.Content = "Please Enter Port Number";
                }
                else
                {
                    //텍스트가 있을시
                    Regex regex = new Regex("^(?:[0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$");
                    bool isMatch = regex.IsMatch(amipPort.Text);

                    if (!isMatch)
                    {
                        //매치실패시
                        amipPortRed.Content = "Please Check The Port Number(0~65535)";
                    }
                    else
                    {
                        //매치시
                        amipPortRed.Content = "";
                    }

                }
            }


        }


        private void OpenamipMask_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (amipMaskRed != null)
            {
                if (amipMask.Text == "")
                {
                    amipMaskRed.Content = "Please Enter Net Mask";
                }
                else
                {
                    //텍스트가 있을시
                    Regex regex = new Regex("^(((255\\.){3}(255|254|252|248|240|224|192|128+))|((255\\.){2}(255|254|252|248|240|224|192|128|0+)\\.0)|((255\\.)(255|254|252|248|240|224|192|128|0+)(\\.0+){2})|((255|254|252|248|240|224|192|128|0+)(\\.0+){3}))$");
                    bool isMatch = regex.IsMatch(amipMask.Text);

                    if (!isMatch)
                    {
                        //매치실패시
                        amipMaskRed.Content = "Please Check Net Mask";
                    }
                    else
                    {
                        //매치시
                        amipMaskRed.Content = "";
                    }

                }
            }


        }

        private void AntennaIP_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (antennaIPRed != null)
            {
                if (antennaIP.Text == "")
                {
                    antennaIPRed.Content = "Please Enter The Ipv4 Address";
                }
                else
                {
                    //텍스트가 있을시
                    Regex regex = new Regex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
                    bool isMatch = regex.IsMatch(antennaIP.Text);

                    if (!isMatch)
                    {
                        antennaIPRed.Content = "Please Check The Ip Address";
                    }
                    else
                    {
                        antennaIPRed.Content = "";
                    }

                }
            }


        }





        private void AntennaMask_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (antennaMaskRed != null)
            {
                if (antennaMask.Text == "")
                {
                    antennaMaskRed.Content = "Please Enter Net Mask";
                }
                else
                {
                    //텍스트가 있을시
                    Regex regex = new Regex("^(((255\\.){3}(255|254|252|248|240|224|192|128+))|((255\\.){2}(255|254|252|248|240|224|192|128|0+)\\.0)|((255\\.)(255|254|252|248|240|224|192|128|0+)(\\.0+){2})|((255|254|252|248|240|224|192|128|0+)(\\.0+){3}))$");
                    bool isMatch = regex.IsMatch(antennaMask.Text);

                    if (!isMatch)
                    {
                        //매치실패시
                        antennaMaskRed.Content = "Please Check Net Mask";
                    }
                    else
                    {
                        //매치시
                        antennaMaskRed.Content = "";
                    }

                }
            }


        }

        private async void OpenAmipSetting_Click(object sender, RoutedEventArgs e)
        {
            string openHost = amipIP.Text;
            string openPort = amipPort.Text;
            string openMask = amipMask.Text;

            string serverHost = antennaIP.Text;
            string serverPort = "5500";
            string serverMask = antennaMask.Text;

            AppSettings.OpenHost = openHost;
            AppSettings.OpenPort = openPort;
            AppSettings.OpenMask = openMask;

            AppSettings.ServerHost = serverHost;
            AppSettings.ServerPort = serverPort;
            AppSettings.ServerMask = serverMask;

            string workM = AppSettings.WorkMode;
            string trackM = AppSettings.TrackMode;
            string rollF = AppSettings.RollF;
            string mGPS = AppSettings.MGPS;

            string url = "";

            if (mGPS == "0") // Auto
            {
                url = "http://localhost:9999/api/executeCommand?commandCode=SetWorkParam&param={"
                    + "workMode:" + workM
                    + ",trackMode:" + trackM
                    + ",rollFactor:" + rollF
                    + ",manual_gps:0"
                    + ",openamipHost:\"" + openHost + "\""
                    + ",openamipPort:" + openPort
                    + ",openamipMask:\"" + openMask + "\""
                    + ",serverHost:\"" + serverHost + "\""
                    + ",serverPort:" + serverPort
                    + ",serverMask:\"" + serverMask + "\""
                    + "}";
            }
            else if (mGPS == "1") // Manual
            {
                string mLong = AppSettings.MLong;
                string mLat = AppSettings.MLat;
                string mHeight = AppSettings.MHeight;

                url = "http://localhost:9999/api/executeCommand?commandCode=SetWorkParam&param={"
                    + "workMode:" + workM
                    + ",trackMode:" + trackM
                    + ",rollFactor:" + rollF
                    + ",manual_longitude:" + mLong
                    + ",manual_latitude:" + mLat
                    + ",manual_height:" + mHeight
                    + ",manual_gps:1"
                    + ",openamipHost:\"" + openHost + "\""
                    + ",openamipPort:" + openPort
                    + ",openamipMask:\"" + openMask + "\""
                    + ",serverHost:\"" + serverHost + "\""
                    + ",serverPort:" + serverPort
                    + ",serverMask:\"" + serverMask + "\""
                    + "}";
            }

            Console.WriteLine(url);

            string response = "";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 10 * 1000;

                using (HttpWebResponse res = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(res.GetResponseStream()))
                {
                    response = reader.ReadToEnd();
                }

                Root classRes = JsonConvert.DeserializeObject<Root>(response);

                if (classRes.code == 0)
                {
                    await alertt(@"\ant-design--check-circle-filled (1).png", "AMIP Setting Success");
                }
                else
                {
                    await alertt(@"\ant-design--close-circle-filled.png", "AMIP Setting Failed");
                }
            }
            catch (Exception ex)
            {
                await alertt(@"\ant-design--close-circle-filled.png", "AMIP Setting Failed");
                Console.WriteLine("에러: " + ex);
            }
        }

        public void RestartBtn_Click(object sender, EventArgs e)
        {

            ResetAPi();
        }

        public void DisconnectBtn_Click(object sender, EventArgs e)
        {
            DisconnectApi();
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

    }
}