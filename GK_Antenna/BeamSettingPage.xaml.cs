using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using LiveChartsCore.Defaults;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;


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

        private double _manualRxLO;
        private double _manualTxLO;
        private string _manualTxPolarity;
        private string _manualRxPolarity;

        private double _txPhiValue;
        private double _rxPhiValue;
        private double _txThetaValue;
        private double _rxThetaValue;
        private bool _isTxOn = false;
        private bool _isRxOn = true;

        private readonly ObservableCollection<ObservablePolarPoint> _txPolarValues = new ObservableCollection<ObservablePolarPoint>();
        private ObservableCollection<ObservablePolarPoint> _RxPolarValues;

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

            GetDeviceQuery();

            GetBeamQuery();
            isGetAutoParam = true;

            StartWebSocket();

            _RxPolarValues = new ObservableCollection<ObservablePolarPoint>();

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

        private void IP_SettingText_Click(Object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.NavigationService.Navigate(new Uri("IpSettingPage.xaml", UriKind.Relative));
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

        private void MapText_Click(System.Object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.NavigationService.Navigate(new Uri("MapPage.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("페이지 이동 오류: " + ex.Message);
            }
        }

        private void CompanyInfoText_Click(System.Object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.NavigationService.Navigate(new Uri("CompanyInfoPage.xaml", UriKind.Relative));
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

                    workMode.SelectedIndex = realResponse.workMode;

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

        private void GetBeamQuery()
        {

            string disconnectApiUrl = "http://localhost:9999/api/executeCommand?commandCode=QueryBeamParam&param={}";

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
                    Console.WriteLine("beamQuery연결성공");
                    string rawdata = classRes.data;
                    string realData = rawdata.Replace("\\", "");

                    Root4 BeamResponse = JsonConvert.DeserializeObject<Root4>(realData);

                    _txPhiValue = BeamResponse.tx_phi;
                    _txThetaValue = BeamResponse.tx_theta;
                    _rxPhiValue = BeamResponse.rx_phi;
                    _rxThetaValue = BeamResponse.rx_theta;

                    //오토모드
                    longitude.Text = BeamResponse.sat_longitude.ToString();


                    /* sym.Text = BeamResponse.sym.ToString();
                     txfreq.Text = BeamResponse.tx_freq.ToString();
                     rxfreq.Text = BeamResponse.rx_freq.ToString();
                     txpolType.Text = BeamResponse.tx_polarity_type;
                     rxpolType.Text = BeamResponse.rx_polarity_type;
                     txlo.Text = BeamResponse.tx_osc.ToString();
                     rxlo.Text = BeamResponse.rx_osc.ToString();


                     //매뉴얼모드
                     manualrxFreq.Text = BeamResponse.rx_freq.ToString();
                     manualsym.Text = BeamResponse.sym.ToString();
                     trackmode.Text = BeamResponse.workMode;
                     //rxlolist.Text = BeamResponse.rx_osc.ToString();
                     mrxloblock.Text = BeamResponse.rx_osc.ToString();
                     rxphi.Text = BeamResponse.rx_phi.ToString();
                     rxtheta.Text = BeamResponse.rx_theta.ToString();
                     rxpoltb.Text = BeamResponse.rx_pol.ToString();
                     rxpolcb.Text = BeamResponse.rx_polarity_type;

                     manualtxfreq.Text = BeamResponse.tx_freq.ToString();
                     manualtxlo.Text = BeamResponse.tx_osc.ToString();
                     txphi.Text = BeamResponse.tx_phi.ToString();
                     txtheta.Text = BeamResponse.tx_theta.ToString();
                     txpoltb.Text = BeamResponse.tx_pol.ToString();
                     txpolcb.Text = BeamResponse.tx_polarity_type;


 */



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
            if (autolongred != null)
            {
                if (longitude.Text == "")
                {
                    autolongred.Content = "No Value (°)";
                }
                else
                {
                    Regex regex = new Regex("^-?(180(\\.0+)?|1[0-7][0-9](\\.\\d+)?|[1-9]?[0-9](\\.\\d+)?)([eE][+-]?\\d+)?$");
                    bool isMatch = regex.IsMatch(longitude.Text);

                    if (!isMatch)
                    {
                        autolongred.Content = "-180°~ 180°";

                    }
                    else
                    {
                        autolongred.Content = "";
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

        private void AutoMode_Checked(object sender, RoutedEventArgs e)
        {
            // Manual UI 숨기기
            if (manualModeBox != null)
                manualModeBox.Visibility = Visibility.Collapsed;


            GetBeamQuery(); // 서버에서 Auto Mode 데이터 가져오기
            isGetAutoParam = true;
            isGetManualParam = false;

            // Manual에서 마지막으로 입력한 값 적용
            rxlo.Text = _manualRxLO.ToString();
            txlo.Text = _manualTxLO.ToString();
            if (!string.IsNullOrEmpty(_manualRxPolarity))
                rxpolType.SelectedItem = _manualRxPolarity;
            if (!string.IsNullOrEmpty(_manualTxPolarity))
                txpolType.SelectedItem = _manualTxPolarity;

        }

        private void MRxFreq_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (mRxFreqRed != null)
            {
                try
                {
                    if (manualrxFreq.Text == "")
                    {
                        mRxFreqRed.Content = "No Value (GHz)";
                    }
                    else if (Double.Parse(manualrxFreq.Text) >= rxFreqMin && Double.Parse(manualrxFreq.Text) <= rxFreqMax)
                    {

                        mRxFreqRed.Content = "";

                    }
                    else
                    {

                        mRxFreqRed.Content = rxFreqMin + "GHz~" + rxFreqMax + "GHz";

                    }
                }
                catch (Exception ex)
                {
                    mRxFreqRed.Content = rxFreqMin + "GHz~" + rxFreqMax + "GHz";
                }

            }


        }

        private void mrxlo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mrxloblock != null)
            {
                if (rxlolist.SelectedItem != null)
                {
                    mrxloblock.Text = rxlolist.SelectedItem.ToString();
                    rxlolist.SelectedIndex = -1;

                }
            }
        }

        private void MSym_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (mSymRed != null)
            {
                try
                {
                    if (manualsym.Text == "")
                    {
                        mSymRed.Content = "No Value";
                    }
                    else if (Double.Parse(manualsym.Text) >= sysMin && Double.Parse(manualsym.Text) <= sysMaxDVB)
                    {

                        mSymRed.Content = "";

                    }
                    else
                    {

                        mSymRed.Content = sysMin + "ksps~" + sysMaxDVB / 1000.0 + "Msps";

                    }
                }
                catch (Exception ex)
                {
                    mSymRed.Content = sysMin + "ksps~" + sysMaxDVB / 1000.0 + "Msps";
                }

            }


        }

        private void MTxFreq_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (mTxFreqRed != null)
            {
                try
                {
                    if (manualtxfreq.Text == "")
                    {
                        mTxFreqRed.Content = "No Value (GHz)";
                    }
                    else if (Double.Parse(manualtxfreq.Text) >= txFreqMin && Double.Parse(manualtxfreq.Text) <= txFreqMax)
                    {

                        mTxFreqRed.Content = "";

                    }
                    else
                    {

                        mTxFreqRed.Content = txFreqMin + "GHz~" + txFreqMax + "GHz";

                    }
                }
                catch (Exception ex)
                {
                    mTxFreqRed.Content = txFreqMin + "GHz~" + txFreqMax + "GHz";
                }

            }
        }

        private void RxLo_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (RxLORed != null)
            {
                try
                {
                    if (rxlo.Text == "")
                    {
                        RxLORed.Content = "No Value (GHz)";
                    }
                    else if (Double.Parse(rxlo.Text) >= rxOscMin && Double.Parse(rxlo.Text) <= rxOscMax)
                    {

                        RxLORed.Content = "";

                    }
                    else
                    {

                        RxLORed.Content = rxOscMin + "GHz~" + rxOscMax + "GHz";

                    }
                }
                catch (Exception ex)
                {
                    RxLORed.Content = rxOscMin + "GHz~" + rxOscMax + "GHz";
                }

            }


        }

        private void Rxphi_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (RxPhiRed != null)
            {
                try
                {
                    if (rxphi.Text == "")
                    {
                        RxPhiRed.Content = "No Value(°)";
                    }
                    else if (Double.Parse(rxphi.Text) >= phiMin && Double.Parse(rxphi.Text) <= phiMax)
                    {

                        RxPhiRed.Content = "";

                    }
                    else
                    {

                        RxPhiRed.Content = phiMin + "°~" + phiMax + "°";

                    }
                }
                catch (Exception ex)
                {
                    RxPhiRed.Content = phiMin + "°~" + phiMax + "°";
                }

            }


        }

        private void RxPol_TextChanged(object sender, TextChangedEventArgs e)
        {
            //check finish

            if (RxPolRed != null)
            {
                try
                {
                    if (rxpoltb.Text == "")
                    {
                        RxPolRed.Content = "No Value(°)";
                    }
                    else if (Double.Parse(rxpoltb.Text) >= rxpolMin && Double.Parse(rxpoltb.Text) <= rxpolMax)
                    {

                        RxPolRed.Content = "";

                    }
                    else
                    {

                        RxPolRed.Content = rxpolMin + "°~" + rxpolMax + "°";

                    }
                }
                catch (Exception ex)
                {
                    RxPolRed.Content = rxpolMin + "°~" + rxpolMax + "°";
                }

            }


        }

        private void RxSwitchOff(object sender, MouseEventArgs e)
        {
            if (autoRadio.IsChecked == true)
            {
                AutoTrackFalse(2, false);
            }
            else
            {
                AutoTrackFalse(2, false);
            }


        }

        private void RxSwitchOn(object sender, MouseEventArgs e)
        {
            if (autoRadio.IsChecked == true)
            {
                AutoTrackTrue(2, true);
            }
            else
            {
                AutoTrackFalse(2, true);
            }





        }

        private void RxTheta_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (RxThetaRed != null)
            {
                try
                {
                    if (rxtheta.Text == "")
                    {
                        RxThetaRed.Content = "No Value(°)";
                    }
                    else if (Double.Parse(rxtheta.Text) >= thetaMin && Double.Parse(rxtheta.Text) <= thetaMax)
                    {

                        RxThetaRed.Content = "";

                    }
                    else
                    {

                        RxThetaRed.Content = thetaMin + "°~" + thetaMax + "°";

                    }
                }
                catch (Exception ex)
                {
                    RxThetaRed.Content = thetaMin + "°~" + thetaMax + "°";
                }

            }


        }

        private void TxFreq_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (txFreqRed != null)
            {
                try
                {
                    if (txfreq.Text == "")
                    {
                        txFreqRed.Content = "No Value (GHz)";
                    }
                    else if (Double.Parse(txfreq.Text) >= txFreqMin && Double.Parse(txfreq.Text) <= txFreqMax)
                    {

                        txFreqRed.Content = "";

                    }
                    else
                    {

                        txFreqRed.Content = txFreqMin + "GHz~" + txFreqMax + "GHz";

                    }
                }
                catch (Exception ex)
                {
                    txFreqRed.Content = txFreqMin + "GHz~" + txFreqMax + "GHz";
                }

            }


        }

        private void Txphi_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (TxPhiRed != null)
            {
                try
                {
                    if (txphi.Text == "")
                    {
                        TxPhiRed.Content = "No Value(°)";
                    }
                    else if (Double.Parse(txphi.Text) >= phiMin && Double.Parse(txphi.Text) <= phiMax)
                    {

                        TxPhiRed.Content = "";

                    }
                    else
                    {

                        TxPhiRed.Content = phiMin + "°~" + phiMax + "°";

                    }
                }
                catch (Exception ex)
                {
                    TxPhiRed.Content = phiMin + "°~" + phiMax + "°";
                }

            }


        }

        private void TxPol_TextChanged(object sender, TextChangedEventArgs e)
        {
            //check finish

            if (TxPolRed != null)
            {
                try
                {
                    if (txpoltb.Text == "")
                    {
                        TxPolRed.Content = "No Value(°)";
                    }
                    else if (Double.Parse(txpoltb.Text) >= txpolMin && Double.Parse(txpoltb.Text) <= txpolMax)
                    {

                        TxPolRed.Content = "";

                    }
                    else
                    {

                        TxPolRed.Content = txpolMin + "°~" + txpolMax + "°";

                    }
                }
                catch (Exception ex)
                {
                    TxPolRed.Content = txpolMin + "°~" + txpolMax + "°";
                }

            }


        }

        private void TxSwitchOff(object sender, MouseEventArgs e)
        {
            if (autoRadio.IsChecked == true)
            {
                TxOFFApi();
            }
            else
            {
                AutoTrackFalse(1, false);
            }
        }

        private void TxSwitchOn(object sender, MouseEventArgs e)
        {
            //tx:1 
            if (autoRadio.IsChecked == true)
            {
                AutoTrackTrue(1, true);
            }
            else
            {
                //매뉴얼모드 체크
                AutoTrackFalse(1, true);
            }

        }

        private void TxTheta_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (TxThetaRed != null)
            {
                try
                {
                    if (txtheta.Text == "")
                    {
                        TxThetaRed.Content = "No Value(°)";
                    }
                    else if (Double.Parse(txtheta.Text) >= thetaMin && Double.Parse(txtheta.Text) <= thetaMax)
                    {

                        TxThetaRed.Content = "";

                    }
                    else
                    {

                        TxThetaRed.Content = thetaMin + "°~" + thetaMax + "°";

                    }
                }
                catch (Exception ex)
                {
                    TxThetaRed.Content = thetaMin + "°~" + thetaMax + "°";
                }

            }


        }

        public void AutoTrackFalse(int tr, bool isOn)
        {
            //tx스위치 키기 tx:1
            string AutoTrackApiUrl = "http://localhost:9999/api/executeCommand?commandCode=SwitchAutoTrack&param={isOn:false}";

            string response = "";
            //localhost:9999 고정

            try
            {
                // request setting
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(AutoTrackApiUrl);
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
                    //autoTrack연결성공
                    if (tr == 1 && isOn == true)
                    {
                        TxOnApi();
                    }
                    else if (tr == 1 && isOn == false)
                    {
                        TxOFFApi();
                    }
                    else if (tr == 2 && isOn == true)
                    {
                        RxOnAPi();
                    }
                    else if (tr == 2 && isOn == false)
                    {
                        RxOFFApi();
                    }


                }
                else if (classRes.code == -1)
                {
                    //연결실패
                    // Console.WriteLine("연결실패");



                }

            }
            catch (Exception ex)
            {

                Console.WriteLine("에러 " + ex);
            }

        }

        public void AutoTrackTrue(int tr, bool isOn)
        {
            //tx스위치 키기 tx:1
            string AutoTrackApiUrl = "http://localhost:9999/api/executeCommand?commandCode=SwitchAutoTrack&param={isOn:true}";

            string response = "";
            //localhost:9999 고정

            try
            {
                // request setting
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(AutoTrackApiUrl);
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
                    //autoTrack연결성공
                    if (tr == 1 && isOn == true)
                    {
                        TxOnApi();
                    }
                    else if (tr == 2 && isOn == true)
                    {
                        RxOnAPi();
                    }


                }
                else if (classRes.code == -1)
                {
                    //연결실패
                    // Console.WriteLine("연결실패");



                }

            }
            catch (Exception ex)
            {

                Console.WriteLine("에러 " + ex);
            }

        }

        public void TxOFFApi()
        {


            //tx스위치 끄기 tx:1
            string disconnectApiUrl = "http://localhost:9999/api/executeCommand?commandCode=SwitchRF&param={ tr:1 , isOn: false }";

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
                    _txPolarValues.Clear();
                    _isTxOn = false;
                    //연결성공

                    MessageBox.Show("TxOFF Success");

                }
                else if (classRes.code == -1)
                {


                    MessageBox.Show("TxOFF Failed");

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("TxOFF Failed");
                Console.WriteLine("에러 " + ex);
            }




        }


        public void TxOnApi()
        {

            //tx스위치 키기 tx:1
            string TxOnApiUrl = "http://localhost:9999/api/executeCommand?commandCode=SwitchRF&param={ tr:1 , isOn: true }";

            string response = "";
            //localhost:9999 고정

            try
            {
                // request setting
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(TxOnApiUrl);
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
                    _txPolarValues.Clear();
                    _isTxOn = true;
                    //연결성공

                    MessageBox.Show("TxON Success");

                }
                else if (classRes.code == -1)
                {
                    //연결실패
                    // Console.WriteLine("연결실패");

                    MessageBox.Show("TxON Failed");

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("TxON Failed");
                Console.WriteLine("에러 " + ex);
            }




        }

        public void RxOnAPi()
        {
            //rx스위치 키기 rx:2
            string disconnectApiUrl = "http://localhost:9999/api/executeCommand?commandCode=SwitchRF&param={ tr:2 , isOn: true }";

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
                    _RxPolarValues.Clear();
                    _isRxOn = true;
                    //연결성공

                    MessageBox.Show("RxON Success");

                }
                else if (classRes.code == -1)
                {
                    //연결실패

                    MessageBox.Show("RxON Failed");

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("RxON Failed");
                Console.WriteLine("에러 " + ex);
            }


        }

        public void RxOFFApi()
        {

            //rx스위치 끄기 rx:2
            string disconnectApiUrl = "http://localhost:9999/api/executeCommand?commandCode=SwitchRF&param={ tr:2 , isOn: false }";

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
                    _RxPolarValues.Clear();
                    _isRxOn = false;
                    //연결성공

                    MessageBox.Show("RxOFF Success");

                }
                else if (classRes.code == -1)
                {
                    //연결실패
                    // Console.WriteLine("연결실패");

                    MessageBox.Show("RxOFF Failed");

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("RxOFF Failed");
                Console.WriteLine("에러 " + ex);
            }



        }


        private WebSocketSharp.WebSocket ws;
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

        private void webMessage(object sender, MessageEventArgs e)
        {
            Root2 response = null;
            string data = e.Data;

            try
            {
                response = JsonConvert.DeserializeObject<Root2>(data);
                this.Dispatcher.Invoke(new Action(delegate ()
                {


                    string data = e.Data;
                    Root2 response = JsonConvert.DeserializeObject<Root2>(data);


                    //tx 스위치
                    if (response.antennaData.antennaTxRfState == "ON")
                    {

                        txOn.Visibility = Visibility.Visible;
                        txOff.Visibility = Visibility.Hidden;

                    }
                    else if (response.antennaData.antennaTxRfState == "OFF")
                    {

                        txOn.Visibility = Visibility.Hidden;
                        txOff.Visibility = Visibility.Visible;

                    }
                    //rx 스위치
                    if (response.antennaData.antennaRxRfState == "ON")
                    {

                        rxOn.Visibility = Visibility.Visible;
                        rxOff.Visibility = Visibility.Hidden;

                    }
                    else if (response.antennaData.antennaRxRfState == "OFF")
                    {
                        rxOn.Visibility = Visibility.Hidden;
                        rxOff.Visibility = Visibility.Visible;

                    }

                    if (isGetAutoParam)
                    {

                        //sym rate가 3000이여야 하는데 300으로 뜬다????
                        sym.Text = response.multiModeReceiverData.mmrSym.ToString();
                        txfreq.Text = response.txArrayPanelData.directionRFFreq.ToString();
                        rxfreq.Text = response.rxArrayPanelData.directionRFFreq.ToString();
                        txpolType.Text = response.txArrayPanelData.directionPolarityType.ToString();
                        rxpolType.Text = response.rxArrayPanelData.directionPolarityType.ToString();
                        txlo.Text = response.frequencyConverterData.fcTxOsc.ToString();
                        rxlo.Text = response.frequencyConverterData.fcRxOsc.ToString();
                        rxtextb.Text = response.frequencyConverterData.fcRxOsc.ToString();
                        txtextb.Text = response.frequencyConverterData.fcTxOsc.ToString();


                        isGetAutoParam = false;

                    }


                    //manual 파라미터
                    if (isGetManualParam)
                    {
                        //매뉴얼모드
                        manualrxFreq.Text = response.rxArrayPanelData.directionRFFreq.ToString();
                        manualsym.Text = response.multiModeReceiverData.mmrSym.ToString();
                        if (response.multiModeReceiverData.mmrWorkMode == null)
                        {
                            trackmode.Text = "DVB";
                        }
                        else
                        {
                            trackmode.Text = response.multiModeReceiverData.mmrWorkMode.ToString();
                        }
                        //rxlolist.Text = BeamResponse.rx_osc.ToString();
                        mrxloblock.Text = response.frequencyConverterData.fcRxOsc.ToString();
                        rxphi.Text = response.rxArrayPanelData.directionPhi.ToString();
                        rxtheta.Text = response.rxArrayPanelData.directionTheta.ToString();
                        rxpoltb.Text = response.rxArrayPanelData.directionPolarityAngle.ToString();
                        rxpolcb.Text = response.rxArrayPanelData.directionPolarityType.ToString();

                        manualtxfreq.Text = response.txArrayPanelData.directionRFFreq.ToString();
                        manualtxlo.Text = response.frequencyConverterData.fcTxOsc.ToString();
                        txphi.Text = response.txArrayPanelData.directionPhi.ToString();
                        txtheta.Text = response.txArrayPanelData.directionTheta.ToString();
                        txpoltb.Text = response.txArrayPanelData.directionPolarityAngle.ToString();
                        txpolcb.Text = response.txArrayPanelData.directionPolarityType.ToString();


                        isGetManualParam = false;



                    }





                    if (response?.txArrayPanelData != null)
                    {
                        _txPhiValue = response.txArrayPanelData.directionPhi;
                        _txThetaValue = response.txArrayPanelData.directionTheta;
                    }

                    if (response?.rxArrayPanelData != null)
                    {
                        _rxPhiValue = response.rxArrayPanelData.directionPhi;
                        _rxThetaValue = response.rxArrayPanelData.directionTheta;
                    }








                }));


            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹소켓 작업3 중 오류 발생: {ex.Message}");
                Debug.WriteLine($"JSON 파싱 오류: {ex.Message}");
                return;
            }


        }

        private async void StartWebSocket()
        {

            try
            {
                await WebSocket(); // 비동기 웹소켓 호출
                                   // MessageBox.Show("웹소켓 작업 완료!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹소켓 작업 중 오류 발생: {ex.Message}");
            }


        }

        public void setAutoPara(object sender, RoutedEventArgs e)
        {
            if (rxOscMin == 0 || rxOscMax == 0)
            {
                if (longRed.Content.ToString() == "" && symRed.Content.ToString() == "" && txFreqRed.Content.ToString() == "" && RxFreqRed.Content.ToString() == "")
                {



                    string satLongitude = longitude.Text;
                    string rxFreq = rxfreq.Text;
                    string txFreq = txfreq.Text;
                    string rxOsc = rxtextb.Text;
                    string rxPolType = rxpolType.Text;
                    string symm = sym.Text;
                    string txOsc = txtextb.Text;
                    string txPolType = txpolType.Text;
                    //   string satName = "";


                    string SetAutoApiUrl = "http://localhost:9999/api/executeCommand?commandCode=SetAutoTrackParam&param={sat_longitude:" + satLongitude + ", rx_freq:" + rxFreq + ", tx_freq:" + txFreq + ", rx_osc:" + rxOsc + ", rx_polarity_type:\"" + rxPolType + "\", sym:" + symm + ", tx_osc:" + txOsc + ", tx_polarity_type:\"" + txPolType + "\"}";
                    Console.WriteLine(SetAutoApiUrl);

                    string response = "";
                    //localhost:9999 고정

                    try
                    {
                        // request setting
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SetAutoApiUrl);
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

                            MessageBox.Show("AutoSet Success");

                        }
                        else if (classRes.code == -1)
                        {
                            //연결실패
                            Console.WriteLine(classRes.msg);

                            MessageBox.Show("AutoSet Failed");

                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("AutoSet Failed");
                        Console.WriteLine("에러 " + ex);
                    }
                }
                else
                {
                    //x 
                }




            }
            else
            {

                if (longRed.Content.ToString() == "" && symRed.Content.ToString() == "" && txFreqRed.Content.ToString() == "" && RxFreqRed.Content.ToString() == "" && RxLORed.Content.ToString() == "")
                {



                    string satLongitude = longitude.Text;
                    string rxFreq = rxfreq.Text;
                    string txFreq = txfreq.Text;
                    string rxOsc = rxlo.Text;
                    string rxPolType = rxpolType.Text;
                    string symm = sym.Text;
                    string txOsc = txtextb.Text;
                    string txPolType = txpolType.Text;
                    //   string satName = "";


                    string SetAutoApiUrl = "http://localhost:9999/api/executeCommand?commandCode=SetAutoTrackParam&param={sat_longitude:" + satLongitude + ", rx_freq:" + rxFreq + ", tx_freq:" + txFreq + ", rx_osc:" + rxOsc + ", rx_polarity_type:\"" + rxPolType + "\", sym:" + symm + ", tx_osc:" + txOsc + ", tx_polarity_type:\"" + txPolType + "\"}";
                    Console.WriteLine(SetAutoApiUrl);

                    string response = "";
                    //localhost:9999 고정

                    try
                    {
                        // request setting
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SetAutoApiUrl);
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

                           MessageBox.Show("AutoSet Success");

                        }
                        else if (classRes.code == -1)
                        {
                            //연결실패
                            Console.WriteLine(classRes.msg);

                           MessageBox.Show("AutoSet Failed");

                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("AutoSet Failed");
                        Console.WriteLine("에러 " + ex);
                    }
                }
                else
                {
                    //x 
                }

            }

            

        }

        private void setBtn_Click(object sender, RoutedEventArgs e)
        {
           

            string workM;
            string trackM;
            string rollF = Roll.Text;
            string mGPS;

            

            if (workMode.SelectedValue.ToString() == "COTM")
                workM = "0";
            else
                workM = "1";

            if (trackMode.SelectedValue.ToString() == "DVB")
                trackM = "0";
            else if (trackMode.SelectedValue.ToString() == "Detection")
                trackM = "1";
            else if (trackMode.SelectedValue.ToString() == "Beacon")
                trackM = "2";
            else
                trackM = "3";

            // GNSS
            if (GNSSMode.SelectedValue.ToString() == "Auto")
                mGPS = "0";
            else
                mGPS = "1";

            AppSettings.WorkMode = workM;
            AppSettings.TrackMode = trackM;
            AppSettings.RollF = rollF;
            AppSettings.MGPS = mGPS;

            if (mGPS == "1")
            {
                AppSettings.MLong = manaulLong.Text;
                AppSettings.MLat = ManualLat.Text;
                AppSettings.MHeight = ManualAlt.Text;
            }

            string openHost = AppSettings.OpenHost;
            string openPort = AppSettings.OpenPort;
            string openMask = AppSettings.OpenMask;

            string serverHost = AppSettings.ServerHost;
            string serverPort = AppSettings.ServerPort;
            string serverMask = AppSettings.ServerMask;

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
            else // Manual
            {
                url = "http://localhost:9999/api/executeCommand?commandCode=SetWorkParam&param={"
                    + "workMode:" + workM
                    + ",trackMode:" + trackM
                    + ",rollFactor:" + rollF
                    + ",manual_longitude:" + AppSettings.MLong
                    + ",manual_latitude:" + AppSettings.MLat
                    + ",manual_height:" + AppSettings.MHeight
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

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 10000;

                using (HttpWebResponse res = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(res.GetResponseStream()))
                {
                    string response = reader.ReadToEnd();

                    Root classRes = JsonConvert.DeserializeObject<Root>(response);

                    if (classRes.code == 0)
                        MessageBox.Show("Beam/GPS Setting Success");
                    else
                        MessageBox.Show("Beam/GPS Setting Failed");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Beam/GPS Setting Failed");
                Console.WriteLine("에러: " + ex);
            }
        }

        private void ＷorkMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (workMode.SelectedItem != null)
            {
                string selected = workMode.SelectedItem.ToString();
                UpdateWorkModeUI(selected);
            }
        }

        void UpdateWorkModeUI(string mode)
        {
            bool isCOTM = mode == "COTM";

            longitude.IsEnabled = isCOTM;
            sym.IsEnabled = isCOTM;
            txfreq.IsEnabled = isCOTM;
            rxfreq.IsEnabled = isCOTM;
            txpolType.IsEnabled = isCOTM;
            rxpolType.IsEnabled = isCOTM;
            txlo.IsEnabled = isCOTM;
            Autorxlocb.IsEnabled = isCOTM;
        }
    }
}




