using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace GK_Antenna
{
    public partial class LoadPage : Page
    {
        public LoadPage()
        {
            InitializeComponent();
            Loaded += LoadPage_Loaded;
        }

        private async void LoadPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 2초 대기 (로딩 화면 효과)
            await Task.Delay(2000);

            // Login 페이지로 이동
            NavigationService?.Navigate(new Login());
        }
    }
}