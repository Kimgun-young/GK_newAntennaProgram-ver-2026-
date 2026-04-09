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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GK_Antenna
{
    /// <summary>
    /// ComapnyInfoPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CompanyInfoPage : Page
    {
        public CompanyInfoPage()
        {
            InitializeComponent();
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

    }
}