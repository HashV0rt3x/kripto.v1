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
using System.Text.RegularExpressions;

namespace kripto.Windows
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public string IpAddressText { get; private set; }
        public string Password { get; private set; } // int emas, string bo'lishi kerak
        
        public InputWindow()
        {
            InitializeComponent();
        }
        
        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(IpAddressTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Barcha maydonlarni to'g'ri to'ldiring!", "Xatolik", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // IP Address validation
            if (!IsValidIpAddress(IpAddressTextBox.Text.Trim()))
            {
                MessageBox.Show("IP address formati noto'g'ri!\nMisol: 192.168.1.1", "Xatolik", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IpAddressTextBox.Focus();
                return;
            }
            
            // Password validation (minimum 4 ta belgi)
            if (PasswordBox.Password.Length < 4)
            {
                MessageBox.Show("Parol kamida 4 ta belgidan iborat bo'lishi kerak!", "Xatolik", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }
            
            // Assign values
            IpAddressText = IpAddressTextBox.Text.Trim();
            Password = PasswordBox.Password; // ToString() kerak emas
            
            DialogResult = true;
            Close();
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        // IP Address validation method
        private bool IsValidIpAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;
                
            string pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            return Regex.IsMatch(ip, pattern);
        }
        
        // Enter key support
        private void InputWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Submit_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }

        private void IpAddressTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                TextBox textBox = sender as TextBox;

                // Faqat raqamlar va nuqta ruxsat etiladi
                if (!char.IsDigit(e.Text[0]) && e.Text[0] != '.')
                {
                    e.Handled = true;
                    return;
                }

                // Agar nuqta kiritilayotgan bo'lsa
                if (e.Text[0] == '.')
                {
                    // Ketma-ket ikkita nuqta bo'lmasin
                    if (textBox.Text.Contains("..") || textBox.Text.EndsWith("."))
                    {
                        e.Handled = true;
                        return;
                    }

                    // 3 tadan ortiq nuqta bo'lmasin
                    if (textBox.Text.Count(c => c == '.') >= 3)
                    {
                        e.Handled = true;
                        return;
                    }
                }

                // Raqam kiritilayotgan bo'lsa
                if (char.IsDigit(e.Text[0]))
                {
                    // Caret pozitsiyasini olish
                    int caretIndex = textBox.SelectionStart;
                    string newText = textBox.Text.Insert(caretIndex, e.Text);

                    // Har bir qismni tekshirish
                    string[] parts = newText.Split('.');

                    foreach (string part in parts)
                    {
                        if (!string.IsNullOrEmpty(part))
                        {
                            // Har bir qism 255 dan katta bo'lmasin
                            if (int.TryParse(part, out int value))
                            {
                                if (value > 255)
                                {
                                    e.Handled = true;
                                    return;
                                }
                            }

                            // Har bir qism 3 ta raqamdan ortiq bo'lmasin
                            if (part.Length > 3)
                            {
                                e.Handled = true;
                                return;
                            }

                            // Leading zero bo'lmasin (01, 001 kabi)
                            if (part.Length > 1 && part[0] == '0')
                            {
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IpAddressTextBox_PreviewTextInput xatolik: {ex.Message}");
            }
        }

        private void IpAddressTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                TextBox textBox = sender as TextBox;

                // Backspace, Delete, Tab, Enter, Arrow keys ruxsat etiladi
                if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab ||
                    e.Key == Key.Enter || e.Key == Key.Left || e.Key == Key.Right ||
                    e.Key == Key.Home || e.Key == Key.End)
                {
                    return;
                }

                // Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X ruxsat etiladi
                if (Keyboard.Modifiers == ModifierKeys.Control &&
                    (e.Key == Key.A || e.Key == Key.C || e.Key == Key.V || e.Key == Key.X))
                {
                    return;
                }

                // Boshqa barcha keys uchun PreviewTextInput ishlaydi
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IpAddressTextBox_PreviewKeyDown xatolik: {ex.Message}");
            }
        }

        private void IpAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TextBox textBox = sender as TextBox;

                // Real-time validation
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Gray);
                    return;
                }

                // IP format tekshiruvi
                if (IsValidIpAddressFormat(textBox.Text))
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IpAddressTextBox_TextChanged xatolik: {ex.Message}");
            }
        }

        // IP format tekshiruvchi helper method
        private bool IsValidIpAddressFormat(string ip)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ip))
                    return false;

                // Nuqta bilan boshlanmasin yoki tugamasin
                if (ip.StartsWith(".") || ip.EndsWith("."))
                    return false;

                // Ketma-ket nuqtalar bo'lmasin
                if (ip.Contains(".."))
                    return false;

                string[] parts = ip.Split('.');

                // Exactly 4 ta qism bo'lishi kerak
                if (parts.Length != 4)
                    return false;

                foreach (string part in parts)
                {
                    // Har bir qism bo'sh bo'lmasin
                    if (string.IsNullOrEmpty(part))
                        return false;

                    // Faqat raqamlar
                    if (!int.TryParse(part, out int value))
                        return false;

                    // 0-255 oralig'ida
                    if (value < 0 || value > 255)
                        return false;

                    // Leading zero bo'lmasin (01, 001 kabi)
                    if (part.Length > 1 && part[0] == '0')
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}