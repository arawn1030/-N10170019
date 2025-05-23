using Microsoft.Maui.Controls;

namespace NumberSearchApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // 使用 NavigationPage 包裹整個應用，支持頁面間的導航
            MainPage = new NavigationPage(new LoginPage());
        }
    }
}
