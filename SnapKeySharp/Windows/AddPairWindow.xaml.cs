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

namespace SnapKeySharp.Windows
{
    /// <summary>
    /// Логика взаимодействия для AddPairWindow.xaml
    /// </summary>



    public partial class AddPairWindow : Window
    {
        public AddPairWindow()
        {
            InitializeComponent();

        }

        bool isBorder1Focused = false;
        bool isBorder2Focused = false;


        public int Key1 { get; private set; } = 0;
        public int Key2 { get; private set; } = 0;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isBorder1Focused)
            {
                Key1Text.Text = e.Key.ToString();
                Key1Text.Foreground = Brushes.White;
                Key1 = KeyInterop.VirtualKeyFromKey(e.Key);
                e.Handled = true;
            }
            else if (isBorder2Focused)
            {
                Key2Text.Text = e.Key.ToString();
                Key2Text.Foreground = Brushes.White;
                Key2 = KeyInterop.VirtualKeyFromKey(e.Key);
                e.Handled = true;
            }
        }


        private void Border1GotFocus(object sender, RoutedEventArgs e)
        {
            isBorder1Focused = true;
            Key1Border.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        }

        private void Border1LostFocus(object sender, RoutedEventArgs e)
        {
            isBorder1Focused = false;
            Key1Border.Background = new SolidColorBrush(Color.FromRgb(37, 37, 37));
        }


        private void Border2GotFocus(object sender, RoutedEventArgs e)
        {
            isBorder2Focused = true;
            Key2Border.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        }

        private void Border2LostFocus(object sender, RoutedEventArgs e)
        {
            isBorder2Focused = false;
            Key2Border.Background = new SolidColorBrush(Color.FromRgb(37, 37, 37));
        }


        private void CancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            if (Key1 == 0 || Key2 == 0) return; // не все клавиши выбраны
            this.DialogResult = true;
            this.Close();
        }

        private void Border1MouseEnter(object sender, MouseEventArgs e)
        {
            Border1GotFocus(sender, e);
        }
        private void Border1MouseLeave(object sender, MouseEventArgs e)
        {
            Border1LostFocus(sender, e);
        }

        private void Border2MouseEnter(object sender, MouseEventArgs e)
        {
            Border2GotFocus(sender, e);
        }

        private void Border2MouseLeave(object sender, MouseEventArgs e)
        {
            Border2LostFocus(sender, e);
        }


    }
}
