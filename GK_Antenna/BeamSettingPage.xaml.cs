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

namespace GK_Antenna
{
    /// <summary>
    /// BeamSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BeamSettingPage : Page
    {
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










    }

}