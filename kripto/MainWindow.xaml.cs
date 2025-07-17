using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace kripto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholder();
        }

        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholder();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                SendMessage();
            }
        }

        private void UpdatePlaceholder()
        {
            //if (PlaceholderText != null)
            //{
            //    if (string.IsNullOrEmpty(MessageTextBox.Text) && !MessageTextBox.IsFocused)
            //    {
            //        if (!isConnectedToServer)
            //        {
            //            PlaceholderText.Text = "Server connection required...";
            //            PlaceholderText.Visibility = Visibility.Visible;
            //        }
            //        else if (!MessageTextBox.IsEnabled)
            //        {
            //            PlaceholderText.Text = "Connect to start messaging...";
            //            PlaceholderText.Visibility = Visibility.Visible;
            //        }
            //        else if (string.IsNullOrEmpty(selectedUser))
            //        {
            //            PlaceholderText.Text = "Select a user to chat...";
            //            PlaceholderText.Visibility = Visibility.Visible;
            //        }
            //        else
            //        {
            //            PlaceholderText.Text = "Type a secure message...";
            //            PlaceholderText.Visibility = Visibility.Visible;
            //        }
            //    }
            //    else
            //    {
            //        PlaceholderText.Visibility = Visibility.Collapsed;
            //    }
            //}
        }

        private void SendMessage()
        {
            //if (!isConnectedToServer)
            //{
            //    MessageBox.Show("Server bilan ulanish yo'q!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //if (string.IsNullOrEmpty(currentUser))
            //{
            //    MessageBox.Show("Avval login qiling!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //if (string.IsNullOrEmpty(selectedUser))
            //{
            //    MessageBox.Show("Foydalanuvchi tanlang!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //string messageContent = MessageTextBox.Text.Trim();
            //if (string.IsNullOrEmpty(messageContent))
            //{
            //    return;
            //}

            //try
            //{
            //    // Mock message sending
            //    AddMessageToUI(currentUser, messageContent, DateTime.Now, true);

            //    MessageTextBox.Text = "";
            //    UpdatePlaceholder();

            //    StatusTextBlock.Text = $"Xabar yuborildi: {selectedUser}";
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Xabar yuborishda xatolik: {ex.Message}", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
        }
    }
}