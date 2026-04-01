using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DoodlesClient
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void ThinBtn_Click(object sender, RoutedEventArgs e)
        {
            drawingHost.UserThickness = 3;
        }

        private void ThickBtn_Click(object sender, RoutedEventArgs e)
        {
            drawingHost.UserThickness = 10;
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            drawingHost.Undo();
        }
    }
}
