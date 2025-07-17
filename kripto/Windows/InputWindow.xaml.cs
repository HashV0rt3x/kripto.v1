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

namespace kripto.Windows
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public string NameInput { get; private set; }
        public int AgeInput { get; private set; }
        public string CityInput { get; private set; }
        public InputWindow()
        {
            InitializeComponent();
        }
        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                string.IsNullOrWhiteSpace(AgeTextBox.Text) ||
                string.IsNullOrWhiteSpace(CityTextBox.Text) ||
                !int.TryParse(AgeTextBox.Text, out int age))
            {
                MessageBox.Show("Barcha maydonlarni to'g'ri to'ldiring!");
                return;
            }

            NameInput = NameTextBox.Text.Trim();
            AgeInput = age;
            CityInput = CityTextBox.Text.Trim();

            DialogResult = true;
            Close();
        }
    }
}
