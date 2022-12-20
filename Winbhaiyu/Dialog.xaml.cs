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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Winbhaiyu
{
    /// <summary>
    /// Interaction logic for Dialog.xaml
    /// </summary>
    public partial class Dialog : Window
    {
        private TimeSpan duration { get; set; } = TimeSpan.FromSeconds(1);
        private IEasingFunction ease { get; set; } = new QuarticEase { EasingMode = EasingMode.EaseInOut };

        public string ans { get; set; }
        public Dialog(string message, string buttontext1, string buttontext2, bool isError, int fontsize)
        {
            InitializeComponent();
            ans = "THIS IS NOT NULL!! THANKS VISUAL STUDIO";
            Message.FontSize = fontsize;
            Message.Text = message;
            yes.Content = buttontext1;
            no.Content = buttontext2;
            if(isError) 
            {
                warn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFeb4034"));
                warn.Text = "Error!";
                warning.Opacity = 0;
                error.Opacity = 100;
                Message.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFeb4034"));
            }
        }

        public void FadeOut(DependencyObject element)
        {

            DoubleAnimation fadeAnimation = new DoubleAnimation()
            {
                From = 1,
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };

            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }

        public void FadeIn(DependencyObject element)
        {

            DoubleAnimation fadeAnimation = new DoubleAnimation()
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = ease
            };

            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }


        public void ShowDialog(Window owner)
        {
            this.Owner = owner;
            this.ShowDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FadeIn(mai);

        }

        private async void yes_Click(object sender, RoutedEventArgs e)
        {
            FadeOut(mai);
            await Task.Delay(1000);
            ans = (string)yes.Content;
            this.DialogResult = true;
        }

        private async void no_Click(object sender, RoutedEventArgs e)
        {
            FadeOut(mai);
            await Task.Delay(1000);
            ans = (string)no.Content;
            this.DialogResult = true;
        }
    }
}
