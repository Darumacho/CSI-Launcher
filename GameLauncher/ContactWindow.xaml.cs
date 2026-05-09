using System;
using System.Diagnostics;
using System.Windows;

namespace GameLauncher
{
    public partial class ContactWindow : Window
    {
        private const string ContactEmail = "darumacho@csi-world.xyz";

        public ContactWindow()
        {
            InitializeComponent();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string subject = Uri.EscapeDataString(SubjectBox.Text.Trim());
            string body = Uri.EscapeDataString(
                string.IsNullOrWhiteSpace(NameBox.Text) ? MessageTextBox.Text.Trim()
                : $"De : {NameBox.Text.Trim()}\n\n{MessageTextBox.Text.Trim()}");

            Process.Start(new ProcessStartInfo(
                $"mailto:{ContactEmail}?subject={subject}&body={body}")
            { UseShellExecute = true });

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
