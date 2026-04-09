using System.Windows;

namespace DoodlesClient
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage : Window
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void JoinBtn_Click(object sender, RoutedEventArgs e)
        {



            ClientWindow clientWindow = new(UsernameTxt.Text, CodeTxt.Text);
            clientWindow.Closed += ClientWindow_Closed;
            clientWindow.Show();
            Hide();
        }

        private void ClientWindow_Closed(object? sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CreateBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
