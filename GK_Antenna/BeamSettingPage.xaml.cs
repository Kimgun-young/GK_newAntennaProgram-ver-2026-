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
using System.Windows.Shapes;
using System.Xml.Linq;
using GK_Antenna.Models;
using Newtonsoft.Json;

namespace GK_Antenna
{
    /// <summary>
    /// BeamSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BeamSettingPage : Page
    {

        double sysMin;
        double sysMaxDVB;
        double txFreqMax;
        double txFreqMin;
        double rxFreqMin;
        double rxFreqMax;
        double rxOscMax;
        double rxOscMin;
        bool showRxPolAngle;
        bool showTxPolAngle;
        double phiMax;
        double phiMin;
        double thetaMax;
        double thetaMin;
        double rxpolMax;
        double rxpolMin;
        double txpolMax;
        double txpolMin;

        bool isGetAutoParam = false;
        bool isGetManualParam = false;
        public BeamSettingPage()
        {
            InitializeComponent();


            workMode.Items.Add("COTM");
            workMode.Items.Add("Open AMIP");

            trackMode.Items.Add("DVB");   // 나중에 trackmode >> Carre Detect Mode로 수정필요
            trackMode.Items.Add("Detection");
            trackMode.Items.Add("Beacon");
            trackMode.Items.Add("DVB-Detection");

            GNSSMode.Items.Add("Auto"); // GNSSMode >> GPSMode 수정필요
            GNSSMode.Items.Add("Manual");

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
            try
            {
                this.NavigationService.Navigate(new Uri("BeamSettingPage.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("페이지 이동 오류: " + ex.Message);
            }
        }

        private void StatusText_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.NavigationService.Navigate(new Uri("StatusPage.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("페이지 이동 오류: " + ex.Message);
            }
        }


        // Roll > RoF 변경 필요 
        private void Roll_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (rollRed != null)
            {
                try
                {
                    if (Roll.Text == "")
                    {
                        rollRed.Content = "No Value";
                    }
                    else if (Double.Parse(Roll.Text) >= 0 && Double.Parse(Roll.Text) <= 1)
                    {

                        rollRed.Content = "";

                    }
                    else
                    {

                        rollRed.Content = "0 ~ 1";

                    }
                }
                catch (Exception ex)
                {
                    rollRed.Content = "0 ~ 1";
                }

            }


        }


        // GNSSMode >> GPSMode
        private void GNSSMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GNSSMode.SelectedValue.ToString() == "Auto")
            {
                manaulLong.Visibility = Visibility.Hidden;
                ManualLat.Visibility = Visibility.Hidden;
                ManualAlt.Visibility = Visibility.Hidden;
                longLabel.Visibility = Visibility.Hidden;
                latLabel.Visibility = Visibility.Hidden;
                altLabel.Visibility = Visibility.Hidden;


            }
            else if (GNSSMode.SelectedValue.ToString() == "Manual")
            {
                manaulLong.Visibility = Visibility.Visible;
                ManualLat.Visibility = Visibility.Visible;
                ManualAlt.Visibility = Visibility.Visible;
                longLabel.Visibility = Visibility.Visible;
                latLabel.Visibility = Visibility.Visible;
                altLabel.Visibility = Visibility.Visible;

            }



        }

        private void ManualLong_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (longRed != null)
            {
                try
                {
                    if (manaulLong.Text == "")
                    {
                        longRed.Content = "No Value (°)";
                    }
                    else if (Double.Parse(manaulLong.Text) >= -180 && Double.Parse(manaulLong.Text) <= 180)
                    {

                        longRed.Content = "";

                    }
                    else
                    {

                        longRed.Content = "-180°~ 180°";

                    }
                }
                catch (Exception ex)
                {
                    longRed.Content = "-180°~ 180°";
                }

            }


        }

        private void ManualLat_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (latRed != null)
            {
                try
                {
                    if (ManualLat.Text == "")
                    {
                        latRed.Content = "No Value (°)";
                    }
                    else if (Double.Parse(ManualLat.Text) >= -180 && Double.Parse(ManualLat.Text) <= 180)
                    {

                        latRed.Content = "";

                    }
                    else
                    {

                        latRed.Content = "-180°~ 180°";

                    }
                }
                catch (Exception ex)
                {
                    latRed.Content = "-180°~ 180°";
                }

            }


        }

        private void ManualAlt_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (altRed != null)
            {
                try
                {
                    if (ManualAlt.Text == "")
                    {
                        altRed.Content = "No Value (m)";
                    }
                    else if (Double.Parse(ManualAlt.Text) >= -1000 && Double.Parse(ManualAlt.Text) <= 10000)
                    {

                        altRed.Content = "";

                    }
                    else
                    {

                        altRed.Content = "-1000m~ 10000m";

                    }
                }
                catch (Exception ex)
                {
                    altRed.Content = "-1000m~ 10000m";
                }

            }


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
                    if (realResponse.workMode == 0)
                    {
                        workMode.SelectedValue = "COTM";
                    }
                    else if (realResponse.workMode == 1)
                    {
                        workMode.SelectedValue = "OpenAMIP";
                    }

                    if (realResponse.trackMode == 0)
                    {
                        trackMode.SelectedValue = "DVB";
                    }
                    else if (realResponse.trackMode == 1)
                    {
                        trackMode.SelectedValue = "Detection";
                    }
                    else if (realResponse.trackMode == 2)
                    {
                        trackMode.SelectedValue = "Beacon";
                    }
                    else if (realResponse.trackMode == 3)
                    {
                        trackMode.SelectedValue = "DVB-Detection";
                    }

                    Roll.Text = realResponse.rollFactor.ToString();

                    if (realResponse.manual_gps == 0)
                    {
                        GNSSMode.SelectedValue = "Auto";
                    }
                    else if (realResponse.manual_gps == 1)
                    {
                        GNSSMode.SelectedValue = "Manual";
                    }

                    manaulLong.Text = realResponse.manual_longitude.ToString();
                    ManualLat.Text = realResponse.manual_latitude.ToString();
                    ManualAlt.Text = realResponse.manual_height.ToString();



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

        private void GetDeviceQuery()
        {

            string disconnectApiUrl = "http://localhost:9999/api/executeCommand?commandCode=QueryDeviceInfo&param={mode:0}";

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
                    Console.WriteLine("deviceQuery연결성공");
                    string rawdata = classRes.data;
                    string realData = rawdata.Replace("\\", "");

                    Root3 Response = JsonConvert.DeserializeObject<Root3>(realData);

                    sysMin = Response.deviceParamRange.symMin;
                    sysMaxDVB = Response.deviceParamRange.symMaxDVB;
                    txFreqMax = Response.deviceParamRange.txFreqMax;
                    txFreqMin = Response.deviceParamRange.txFreqMin;
                    rxFreqMin = Response.deviceParamRange.rxFreqMin;
                    rxFreqMax = Response.deviceParamRange.rxFreqMax;
                    phiMax = Response.deviceParamRange.phiMax;
                    phiMin = Response.deviceParamRange.phiMin;
                    thetaMax = Response.deviceParamRange.pitchMax;
                    thetaMin = Response.deviceParamRange.pitchMin;
                    rxpolMax = Response.deviceParamRange.rxAngleMax;
                    rxpolMin = Response.deviceParamRange.rxAngleMin;
                    txpolMax = Response.deviceParamRange.txAngleMax;
                    txpolMin = Response.deviceParamRange.txAngleMin;
                    rxOscMax = Response.deviceParamRange.rxOscMax;
                    rxOscMin = Response.deviceParamRange.rxOscMin;



                    //tx폴타입설정
                    //txpolType.ItemsSource = Response.deviceParamRange.txDirectionPolarityType;
                    txpolType.Items.Add("HORIZONTAL");
                    txpolType.Items.Add("VERTICAL");
                    txpolType.Items.Add("LEFT");
                    txpolType.Items.Add("RIGHT");
                    //  rxpolType.ItemsSource = Response.deviceParamRange.rxDirectionPolarityType;
                    rxpolType.Items.Add("HORIZONTAL");
                    rxpolType.Items.Add("VERTICAL");
                    rxpolType.Items.Add("LEFT");
                    rxpolType.Items.Add("RIGHT");



                    Autorxlocb.Visibility = Visibility.Visible;
                    rxlo.Visibility = Visibility.Collapsed;
                    RxLORed.Visibility = Visibility.Collapsed;
                    rxtextb.Visibility = Visibility.Visible;








                    txlo.ItemsSource = Response.deviceParamRange.txOscList;
                    //rxlo.ItemsSource = Response.deviceParamRange.rxOscList;
                    Autorxlocb.ItemsSource = Response.deviceParamRange.rxOscList;



                    showRxPolAngle = Response.deviceParamRange.showRxPolAngle;
                    showTxPolAngle = Response.deviceParamRange.showTxPolAngle;

                    trackmode.ItemsSource = new List<string> { "DVB", "DETECTION", "BEACON" };
                    rxlolist.ItemsSource = Response.deviceParamRange.rxOscList;

                    // rxpolcb.ItemsSource = Response.deviceParamRange.rxDirectionPolarityType;
                    rxpolcb.Items.Add("HORIZONTAL");
                    rxpolcb.Items.Add("VERTICAL");
                    rxpolcb.Items.Add("LEFT");
                    rxpolcb.Items.Add("RIGHT");

                    manualtxlo.ItemsSource = Response.deviceParamRange.txOscList;
                    //  txpolcb.ItemsSource = Response.deviceParamRange.txDirectionPolarityType;
                    txpolcb.Items.Add("HORIZONTAL");
                    txpolcb.Items.Add("VERTICAL");
                    txpolcb.Items.Add("LEFT");
                    txpolcb.Items.Add("RIGHT");

                    Console.WriteLine("MODEL: " + Response.model);
                    Console.WriteLine("FIRMWARE TYPE: " + Response.firmwareType);
                    Console.WriteLine("TX OSC LIST: " + string.Join(",", Response.deviceParamRange.txOscList));
                    Console.WriteLine("RX OSC LIST: " + string.Join(",", Response.deviceParamRange.rxOscList));



                }
                else if (classRes.code == -1)
                {
                    //연결실패(disconnected)
                    // Console.WriteLine("연결실패");



                }

            }
            catch (Exception ex)
            {

                Console.WriteLine("에러 " + ex);
            }

        }




        private void autolongitude_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (longRed != null)
            {
                if (longitude.Text == "")
                {
                    longRed.Content = "No Value (°)";
                }
                else
                {
                    Regex regex = new Regex("^-?(180(\\.0+)?|1[0-7][0-9](\\.\\d+)?|[1-9]?[0-9](\\.\\d+)?)([eE][+-]?\\d+)?$");
                    bool isMatch = regex.IsMatch(longitude.Text);

                    if (!isMatch)
                    {
                        longRed.Content = "-180°~ 180°";

                    }
                    else
                    {
                        longRed.Content = "";
                    }


                }




            }


        }

        private void Autosym_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (symRed != null)
            {
                try
                {
                    if (sym.Text == "")
                    {
                        symRed.Content = "No Value";
                    }
                    else if (Double.Parse(sym.Text) >= sysMin && Double.Parse(sym.Text) <= sysMaxDVB)
                    {

                        symRed.Content = "";

                    }
                    else
                    {

                        symRed.Content = sysMin + "ksps~" + sysMaxDVB / 1000.0 + "Msps";

                    }
                }
                catch (Exception ex)
                {
                    symRed.Content = sysMin + "ksps~" + sysMaxDVB / 1000.0 + "Msps";
                }

            }
        }

            private void autorxlo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rxtextb != null)
            {
                if (Autorxlocb.SelectedItem != null)
                {
                    rxtextb.Text = Autorxlocb.SelectedItem.ToString();
                    Autorxlocb.SelectedIndex = -1;

                }
            }
        }

        private void autotxlo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtextb != null)
            {
                if (txlo.SelectedItem != null)
                {
                    txtextb.Text = txlo.SelectedItem.ToString();
                    txlo.SelectedIndex = -1;

                }
            }
        }


    

    private void ManualMode_Checked(object sender, RoutedEventArgs e)
        {
            // 사용자가 선택한 값 저장
            if (rxpolType.SelectedItem != null)
                _manualRxPolarity = rxpolType.SelectedItem.ToString();
            if (txpolType.SelectedItem != null)
                _manualTxPolarity = txpolType.SelectedItem.ToString();

            // Rx/Tx LO 값도 저장
            if (double.TryParse(rxlo.Text, out double rxValue))
                _manualRxLO = rxValue;
            if (double.TryParse(txlo.Text, out double txValue))
                _manualTxLO = txValue;

            // Tx 상태 표시
            txOn.Visibility = _isTxOn ? Visibility.Visible : Visibility.Hidden;
            txOff.Visibility = _isTxOn ? Visibility.Hidden : Visibility.Visible;

            // Manual UI 표시
            if (manualModeBox != null)
                manualModeBox.Visibility = Visibility.Visible;

            isGetAutoParam = false;
            isGetManualParam = true;
        }

        private void RxFreq_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (RxFreqRed != null)
            {
                try
                {
                    if (rxfreq.Text == "")
                    {
                        RxFreqRed.Content = "No Value (GHz)";
                    }
                    else if (Double.Parse(rxfreq.Text) >= rxFreqMin && Double.Parse(rxfreq.Text) <= rxFreqMax)
                    {

                        RxFreqRed.Content = "";

                    }
                    else
                    {

                        RxFreqRed.Content = rxFreqMin + "GHz~" + rxFreqMax + "GHz";

                    }
                }
                catch (Exception ex)
                {
                    RxFreqRed.Content = rxFreqMin + "GHz~" + rxFreqMax + "GHz";
                }

            }


        }


    }



}