using System.Diagnostics;
using System.Reflection;
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
using CompressedImageAPP.ViewModels;

namespace CompressedImageAPP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainWindowViewModel viewmodel = new();
            this.DataContext = viewmodel;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();  // 实现窗口拖拽
            }
        }

        private void OutputFormat_ComboBox_MouseEnter(object sender, MouseEventArgs e)
        {
            OutputFormat_ComboBox.IsDropDownOpen = true;
        }

        private void OutputFormat_ComboBox_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //var comboBox = (ComboBox)sender;
            if ((bool)e.NewValue)
            {
                // 鼠标进入
                OutputFormat_ComboBox.IsDropDownOpen = false;
            }
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow
            {
                Owner = Application.Current.MainWindow
            };
            aboutWindow.ShowDialog();

            //// 创建弹窗窗口
            //var aboutWindow = new Window
            //{
            //    Title = "关于本软件",
            //    Width = 400,
            //    Height = 500,
            //    WindowStartupLocation = WindowStartupLocation.CenterOwner,
            //    Owner = Application.Current.MainWindow,
            //    ResizeMode = ResizeMode.NoResize,
            //    WindowStyle = WindowStyle.None,
            //};

            //// 创建主布局
            //var mainStackPanel = new StackPanel
            //{
            //    Margin = new Thickness(20),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};

            //// 添加项目标题
            //var titleTextBlock = new TextBlock
            //{
            //    Text = "图片压缩转换工具",
            //    FontSize = 20,
            //    FontWeight = FontWeights.Bold,
            //    Margin = new Thickness(0, 0, 0, 10),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};

            //// 添加版本信息
            //var versionTextBlock = new TextBlock
            //{
            //    Text = $"版本: {Assembly.GetExecutingAssembly().GetName().Version}",
            //    Margin = new Thickness(0, 0, 0, 20),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};

            //// 添加项目描述
            //var descriptionTextBlock = new TextBlock
            //{
            //    Text = "这是一个完全免费的高效的图片压缩和格式转换工具，支持多种图片格式互转，支持调整压缩率和图片分辨率。\n项目完全开源。",
            //    TextWrapping = TextWrapping.Wrap,
            //    Margin = new Thickness(0, 0, 0, 20),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};

            //// 添加二维码图片（假设你有一个二维码图片资源）
            //var qrCodeImage1 = new Image
            //{
            //    Source = new BitmapImage(new Uri("pack://application:,,,/Resource/1.png")),
            //    Width = 150,
            //    Height = 150,
            //    Margin = new Thickness(0, 0, 0, 10),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};
            //var qrCodeImage2 = new Image
            //{
            //    Source = new BitmapImage(new Uri("pack://application:,,,/Resource/2.png")),
            //    Width = 150,
            //    Height = 150,
            //    Margin = new Thickness(0, 0, 0, 5),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};

            //var qrCodesPanel = new StackPanel
            //{
            //    Orientation = Orientation.Horizontal,
            //    HorizontalAlignment = HorizontalAlignment.Center,

            //    Margin = new Thickness(0, 30, 0, 5)
            //};

            //// 第一个二维码和文字
            //var qr1Stack = new StackPanel
            //{
            //    Margin = new Thickness(0, 0, 20, 0),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};
            //qr1Stack.Children.Add(new TextBlock
            //{
            //    Text = "支付宝",
            //    HorizontalAlignment = HorizontalAlignment.Center,
            //    Margin = new Thickness(0, 0, 0, 5)
            //});
            //qr1Stack.Children.Add(qrCodeImage1);

            //// 第二个二维码和文字
            //var qr2Stack = new StackPanel
            //{
            //    Margin = new Thickness(20, 0, 0, 0),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};
            //qr2Stack.Children.Add(new TextBlock
            //{
            //    Text = "微信",
            //    HorizontalAlignment = HorizontalAlignment.Center,
            //    Margin = new Thickness(0, 0, 0, 5)
            //});
            //qr2Stack.Children.Add(qrCodeImage2);

            //qrCodesPanel.Children.Add(qr1Stack);
            //qrCodesPanel.Children.Add(qr2Stack);

            //// 添加赞助提示文字
            //var supportTextBlock = new TextBlock
            //{
            //    Text = "觉得还不错，请作者喝杯茶",
            //    FontStyle = FontStyles.Italic,
            //    Margin = new Thickness(0, 0, 0, 10),
            //    HorizontalAlignment = HorizontalAlignment.Center
            //};

            //// 添加关闭按钮
            //var closeButton = new Button
            //{
            //    Content = "关闭",
            //    Width = 80,
            //    HorizontalAlignment = HorizontalAlignment.Center,
            //    Margin = new Thickness(0, 10, 0, 0)
            //};
            //closeButton.Click += (s, args) => aboutWindow.Close();

            //// 将所有控件添加到主布局
            //mainStackPanel.Children.Add(titleTextBlock);
            //mainStackPanel.Children.Add(versionTextBlock);
            //mainStackPanel.Children.Add(descriptionTextBlock);
            //mainStackPanel.Children.Add(qrCodesPanel);
            //mainStackPanel.Children.Add(supportTextBlock);
            //mainStackPanel.Children.Add(closeButton);

            //// 设置窗口内容并显示
            //aboutWindow.Content = mainStackPanel;
            //aboutWindow.ShowDialog();
        }

    }
}