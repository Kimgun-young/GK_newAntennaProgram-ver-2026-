using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GK_Antenna.Models;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using Newtonsoft.Json;
using WebSocketSharp;

namespace GK_Antenna
{

    public partial class MapPage : Page
    {

        private GMapMarker movingMarker;
        private DispatcherTimer timer;




        public static double currentlat;
        public static double currentlng;

        private bool antenna_ON = false;


        private string filePath = "ip.txt"; // 저장할 파일 경로
        private string defaultIp = "192.168.0.2"; // 기본 IP 주소

        public MapPage()
        {
            InitializeComponent();

            StartWebSocket();

            InitializeMap();
            StartRealTimeUpdates();
        }

        private void InitializeMap()
        {
            gmap.MapProvider = GMapProviders.GoogleMap;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gmap.Position = new PointLatLng(currentlat, currentlng);
            gmap.MinZoom = 1;
            gmap.MaxZoom = 20;
            gmap.Zoom = 17;
            gmap.CanDragMap = false;

            // 1. 경로를 리소스 상대 경로로 단일화
            string uri = "/Resources/Icons/marker.png";

            movingMarker = new GMapMarker(gmap.Position);
            movingMarker.Shape = new Image
            {
                Width = 25,
                Height = 25,
                Source = new BitmapImage(new Uri(uri, UriKind.Relative))
            };

            gmap.Markers.Add(movingMarker);

            Console.WriteLine(currentlat + " " + currentlng);
        }
        private void TopBar_MouseEnter(object sender, MouseEventArgs e)
        {
            DropBar.Visibility = Visibility.Visible;
        }

        private void TopBar_MouseLeave(object sender, MouseEventArgs e)
        {
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


        private void StartRealTimeUpdates()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5); // 5초마다 위치 업데이트
            timer.Tick += Timer_Tick;

            timer.Start();


        }

        private void Timer_Tick(object sender, EventArgs e)
        {

            double newLat = currentlat;
            double newLng = currentlng;

            PointLatLng newPosition = new PointLatLng(newLat, newLng);

            movingMarker.Position = newPosition;

            gmap.Position = newPosition;
            Console.WriteLine("타이머 " + currentlat + " " + currentlng);

        }

        private async void StartWebSocket()
        {

            try
            {
                await WebSocket();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹소켓 작업 중 오류 발생: {ex.Message}");
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

            this.Dispatcher.Invoke(new Action(delegate ()
            {

                string data = e.Data;
                Root2 response = JsonConvert.DeserializeObject<Root2>(data);


                if (response.antennaData.antennaState == 0)
                {

                    antenna_ON = true;
                    timer.Start();

                    if (response.gnssData.state == 1)
                    {
                        currentlat = response.gnssData.gpsLatitude;
                        currentlng = response.gnssData.gpsLongitude;
                        latitude.Content = response.gnssData.gpsLatitude;
                        longitude.Content = response.gnssData.gpsLongitude;
                    }
                    else
                    {
                        currentlat = 37.392402;
                        currentlng = 126.959046;
                        latitude.Content = 37.392402;
                        longitude.Content = 126.959046;
                    }



                }
                else
                {

                    latitude.Content = "0.00";
                    longitude.Content = "0.00";
                    currentlat = 0;
                    currentlng = 0;
                    antenna_ON = false;
                    timer.Stop();
                }


            }));





        }
    }
}