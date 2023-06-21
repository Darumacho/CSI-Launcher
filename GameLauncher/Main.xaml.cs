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
            this.SoundClick();
            csi1.Show();
            this.Close();
        }
        private void ButtonClicked_2(object sender, RoutedEventArgs e)
        {
            CSIIWindow csi2 = new CSIIWindow();
            this.SoundClick();
            csi2.Show();
            this.Close();
        }

        private void ButtonClicked_R(object sender, RoutedEventArgs e)
        {
            CSRWindow csir = new CSRWindow();
            this.SoundClick();
            csir.Show();
            this.Close();
        }

        private void SoundClick()
        {
            //SoundPlayer start = new SoundPlayer("C:/Users/Darumacho/Documents/Visual Studio 2019/MultiLauncher/GameLauncher/sound/Startup.wav");
            string Audio_FilePath = AppDomain.CurrentDomain.BaseDirectory + "sound\\Startup.wav";
            SoundPlayer start = new SoundPlayer(Audio_FilePath);
            start.Load();
            start.Play();
        }
    }
}
