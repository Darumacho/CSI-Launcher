using System;
using System.Media;
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

namespace GameLauncher
{
    /// <summary>
    /// Logique d'interaction pour Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public Main()
        {
            InitializeComponent();
        }
        private void ButtonClicked_1(object sender, RoutedEventArgs e)
        {
            CSIWindow csi1 = new CSIWindow();
            //SoundPlayer start = new SoundPlayer("sound/Startup.wav");
            //start.Load();
            //start.Play();
            csi1.Show();
            this.Close();
        }
        private void ButtonClicked_2(object sender, RoutedEventArgs e)
        {
            CSIIWindow csi2 = new CSIIWindow();
            csi2.Show();
            this.Close();
        }

        private void ButtonClicked_R(object sender, RoutedEventArgs e)
        {
            CSRWindow csir = new CSRWindow();
            csir.Show();
            this.Close();
        }
    }
}
