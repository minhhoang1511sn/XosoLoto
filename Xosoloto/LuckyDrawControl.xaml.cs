using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xosoloto.Services;

namespace Xosoloto
{
    public partial class LuckyDrawWindow : Window
    {
        /// <summary>
        /// Màn hình Trình chiếu (Show Window) dành cho khán giả/máy chiếu khi chơi "Lộc Xuân
        /// Đầu Năm", tách biệt hoàn toàn với cửa sổ điều khiển này (màn hình Chỉnh sửa) - giống
        /// mô hình Presenter View / Slide Show của PowerPoint. Mọi thao tác nhập số trúng
        /// thưởng (kể cả từ PrizeDisplayWindow) được đẩy sang đây ngay lập tức.
        /// </summary>
        private LocXuanShowWindow _showWindow;
        // Kích thước chuẩn
        private const double BASE_WIDTH = 1600;
        private const double BASE_HEIGHT = 900;
        private const double KK_X_FRAC = 0.5768;
        private const double KK_Y_FRAC = 0.8815;

        // Bề rộng khung "9 GIẢI KHUYẾN KHÍCH" trên ảnh nền (đo theo canvas gốc 1600x900).
        // Dùng để giới hạn TextWrapping khi chuỗi số khuyến khích dài, đồng thời để
        // TextAlignment="Center" canh giữa từng dòng bên trong khung.
        private const double KK_FRAME_WIDTH = 900;
        // Bảng màu xoay vòng cho các nút bấm (số vòng động, không còn giới hạn 5).
        private static readonly string[] ButtonColors =
        {
            "#4CAF50", "#2196F3", "#FF9800", "#E91E63", "#9C27B0",
            "#00BCD4", "#795548", "#607D8B", "#8BC34A", "#3F51B5",
            "#FFC107", "#009688"
        };

        // THÊM CÁC PROPERTIES
        public string ImagePath { get; set; }
        public string LogoPath { get; set; }
        public string[] PrizePaths { get; set; }

        /// <summary>
        /// True nếu người dùng bấm "🔁 Đổi loại game" ngay trong màn hình quay giải này để
        /// thoát ra và quay lại màn hình chọn loại game, thay vì phải đóng hẳn ứng dụng.
        /// InitLocXuan.BtnDone_Click kiểm tra cờ này sau khi ShowDialog() trả về.
        /// </summary>
        public bool ChangeGameRequested { get; private set; } = false;

        // Ô số hiển thị mỗi giải (sinh động theo PrizePaths.Length).
        private List<TextBlock> prizeNumberBlocks = new();
        private List<string> prizeNumbers = new();
        private List<string> prizeNames = new();

        public LuckyDrawWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        public LuckyDrawWindow(string imagePath, string title, string logoPath, string[] prizePaths)
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            this.Closed += (s, e) => CloseShowWindow();

            this.ImagePath = imagePath;
            this.Title = title;
            this.LogoPath = logoPath;
            this.PrizePaths = prizePaths ?? Array.Empty<string>();

            BuildDynamicPrizeSlots();
            LoadData();
            EnsureShowWindow();
        }

        /// <summary>
        /// Mở (hoặc đưa lên foreground) màn hình Trình chiếu. Nếu máy có 2 màn hình trở lên,
        /// màn hình này tự động full-screen ở màn hình phụ (máy chiếu/TV), tách biệt với cửa
        /// sổ điều khiển hiện tại.
        /// </summary>
        private void EnsureShowWindow()
        {
            if (_showWindow == null)
            {
                _showWindow = new LocXuanShowWindow();
                // Nếu người dùng tự đóng màn hình Trình chiếu (bấm ESC hoặc nút "✕ Đóng"),
                // phải xoá tham chiếu ở đây để lần bấm "Trình chiếu" tiếp theo tạo cửa sổ mới
                // thay vì lỗi do dùng lại một Window đã Close().
                _showWindow.Closed += (s, e) => _showWindow = null;
                _showWindow.Show();
            }
            MonitorHelper.PlaceOnShowMonitor(_showWindow);
            _showWindow.SetMonitorStatus(MonitorHelper.DescribeMode());
            _showWindow.Initialize(this.Title, this.LogoPath, this.ImagePath, PrizePaths?.Length ?? 0);
            for (int i = 0; i < prizeNumbers.Count; i++)
                _showWindow.UpdatePrizeNumber(i, prizeNumbers[i]);
            // Đưa focus bàn phím sang màn hình Trình chiếu để phím ESC đóng được ngay,
            // kể cả khi đang Topmost đè lên màn hình Chỉnh sửa (trường hợp chỉ có 1 màn hình).
            _showWindow.Activate();
        }

        private void CloseShowWindow()
        {
            if (_showWindow == null) return;
            _showWindow.Close();
            _showWindow = null;
        }

        /// <summary>
        /// Sinh động (theo số vòng = PrizePaths.Length) các ô TextBlock hiển thị số trúng
        /// thưởng và các nút mở màn hình nhập số cho từng giải. Vị trí ô số được sắp theo
        /// lưới 2 cột (giống bố cục gốc cho 4 giải "Lộc Xuân"); ô cuối cùng luôn là giải
        /// "Khuyến khích" và được canh giữa phía dưới, giữ đúng hành vi cũ khi có 5 giải.
        /// </summary>
        private void BuildDynamicPrizeSlots()
        {
            int count = PrizePaths?.Length ?? 0;

            prizeNumberBlocks = new List<TextBlock>(count);
            prizeNumbers = new List<string>(count);
            prizeNames = new List<string>(count);
            spButtons.Children.Clear();

            for (int i = 0; i < count; i++)
            {
                bool isLastSlot = i == count - 1;
                prizeNames.Add(isLastSlot ? "9 GIẢI KHUYẾN KHÍCH" : $"LỘC XUÂN {i + 1}");
                prizeNumbers.Add("- - - -");

                var block = new TextBlock
                {
                    Text = "- - - -",
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    FontSize = isLastSlot ? 35 : 56,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = isLastSlot ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    Width = isLastSlot ? KK_FRAME_WIDTH : double.NaN // NaN = auto, giữ hành vi cũ cho các ô khác
                };
                prizeNumberBlocks.Add(block);
                MainCanvas.Children.Add(block);
                PositionPrizeBlock(block, i, isLastSlot);

                int capturedIndex = i;
                var btn = new Button
                {
                    Content = (i + 1).ToString(),
                    Width = 50,
                    Height = 50,
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ButtonColors[i % ButtonColors.Length])),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                var cornerStyle = new Style(typeof(Border));
                cornerStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(25)));
                btn.Resources.Add(typeof(Border), cornerStyle);
                btn.Click += (s, e) => OpenPrizeWindow(capturedIndex);
                spButtons.Children.Add(btn);
            }
        }

        private void OpenPrizeWindow(int prizeIndex)
        {
            try
            {
                if (PrizePaths == null || PrizePaths.Length == 0)
                {
                    MessageBox.Show("Chưa có hình ảnh giải thưởng!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (prizeIndex < 0 || prizeIndex >= PrizePaths.Length)
                {
                    MessageBox.Show("Giải thưởng không hợp lệ!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Đẩy màn hình Trình chiếu sang chế độ CHI TIẾT đúng giải này NGAY, để khán giả
                // thấy đúng giải đang được quay thay vì màn hình tổng.
                ShowPrizeOnScreen(prizeIndex);

                // Tạo và mở PrizeDisplayWindow - TRUYỀN THÊM PARENT
                PrizeDisplayWindow prizeWindow = new PrizeDisplayWindow(
                    title: this.Title,
                    logoPath: this.LogoPath,
                    prizeName: prizeNames[prizeIndex],
                    prizeIndex: prizeIndex,
                    prizePaths: this.PrizePaths,
                    backgroundPath: this.ImagePath,
                    imagePath: this.ImagePath,
                    parent: this
                );

                try
                {
                    // Hiển thị PrizeDisplayWindow
                    prizeWindow.ShowDialog();
                }
                finally
                {
                    // Dù đóng bằng Back, Enter hay ESC (hoặc lỗi), luôn đưa màn hình Trình chiếu
                    // quay lại chế độ TỔNG khi không còn giải nào đang mở.
                    ShowOverviewOnScreen();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ giải thưởng: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Đẩy màn hình Trình chiếu (khán giả/máy chiếu) sang chế độ CHI TIẾT đúng giải
        /// <paramref name="prizeIndex"/> đang được mở, khớp bố cục PrizeDisplayWindow.
        /// Được gọi khi mở 1 giải (OpenPrizeWindow) hoặc khi chuyển giải bằng phím trái/phải
        /// ngay trong PrizeDisplayWindow (qua parentWindow.ShowPrizeOnScreen).
        /// </summary>
        public void ShowPrizeOnScreen(int prizeIndex)
        {
            if (_showWindow == null) return;
            if (prizeIndex < 0 || prizeIndex >= prizeNames.Count) return;

            string prizeImg = (PrizePaths != null && prizeIndex < PrizePaths.Length) ? PrizePaths[prizeIndex] : null;
            string number = prizeIndex < prizeNumbers.Count ? prizeNumbers[prizeIndex] : "? ? ? ?";

            _showWindow.ShowPrizeDetail(prizeIndex, prizeNames[prizeIndex], prizeImg, this.LogoPath, this.Title);
            _showWindow.UpdatePrizeNumber(prizeIndex, number);
        }

        /// <summary>Đưa màn hình Trình chiếu quay lại chế độ TỔNG (tất cả các giải).</summary>
        public void ShowOverviewOnScreen() => _showWindow?.ShowOverview();

        private void btnToggleShow_Click(object sender, RoutedEventArgs e)
        {
            if (_showWindow == null) EnsureShowWindow();
            else CloseShowWindow();
        }

        /// <summary>
        /// Thoát khỏi màn hình quay giải Lộc Xuân hiện tại và quay lại màn hình chọn loại
        /// game, thay vì phải đóng hẳn ứng dụng. Đóng luôn màn hình Trình chiếu (nếu đang
        /// mở) trước khi thoát.
        /// </summary>
        private void btnChangeGame_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có muốn thoát khỏi Lộc Xuân Đầu Năm và chọn lại loại game khác không?\n" +
                "Kết quả quay giải hiện tại (nếu có) sẽ không được lưu lại.",
                "Đổi loại game", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            ChangeGameRequested = true;
            this.Close();
        }

        // PUBLIC METHOD - Cập nhật số giải thưởng từ PrizeDisplayWindow
        public void UpdatePrizeNumber(int prizeIndex, string prizeNumber)
        {
            if (prizeIndex < 0 || prizeIndex >= prizeNumberBlocks.Count) return;

            prizeNumbers[prizeIndex] = prizeNumber;
            var block = prizeNumberBlocks[prizeIndex];
            block.Text = prizeNumber;
            // Nội dung đổi độ dài làm DesiredSize đổi theo, tính lại vị trí để ô số vẫn
            // canh giữa đúng khung, không bị lệch.
            PositionPrizeBlock(block, prizeIndex, prizeIndex == prizeNumberBlocks.Count - 1);

            // Đẩy real-time sang màn hình Trình chiếu (khán giả/máy chiếu).
            _showWindow?.UpdatePrizeNumber(prizeIndex, prizeNumber);
        }

        // PUBLIC METHOD - Lấy số giải thưởng hiện tại
        public string GetPrizeNumber(int prizeIndex)
        {
            if (prizeIndex < 0 || prizeIndex >= prizeNumbers.Count)
                return "? ? ? ?";

            return prizeNumbers[prizeIndex];
        }

        // PUBLIC METHOD - Cập nhật tất cả số trúng thưởng
        public void UpdateAllPrizeNumbers(string[] winningNumbers)
        {
            if (winningNumbers == null) return;

            int max = Math.Min(prizeNumberBlocks.Count, winningNumbers.Length);
            for (int i = 0; i < max; i++)
            {
                UpdatePrizeNumber(i, winningNumbers[i]);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                CenterTitle();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // Cửa sổ giờ được vẽ trên 1 canvas ẢO kích thước cố định BASE_WIDTH x BASE_HEIGHT
        // (xem LuckyDrawControl.xaml: <Viewbox><Grid Width="1600" Height="900">...), và
        // Viewbox tự co giãn ĐỀU toàn bộ khối đó theo kích thước cửa sổ thực tế. Vì vậy
        // KHÔNG còn cần lắng nghe SizeChanged / tính lại scaleX,scaleY thủ công cho từng
        // phần tử nữa - mọi vị trí bên dưới đều là toạ độ cố định trên canvas 1600x900,
        // Viewbox lo phần co giãn cho khớp màn hình lớn/nhỏ.
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) { }

        /// <summary>Canh giữa tiêu đề theo chiều rộng canvas cố định (BASE_WIDTH). Gọi lại
        /// mỗi khi nội dung Title thay đổi vì độ rộng chữ phụ thuộc vào nội dung.</summary>
        private void CenterTitle()
        {
            txtTitle.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(txtTitle, (BASE_WIDTH - txtTitle.DesiredSize.Width) / 2);
            Canvas.SetTop(txtTitle, 130);
        }

        // Vị trí các ô số được đo TRỰC TIẾP từ ảnh nền Images/backgroundlocxuan.png (tâm
        // của từng khung trắng bên dưới mỗi banner "LỘC XUÂN i"/"9 GIẢI KHUYẾN KHÍCH"),
        // biểu diễn bằng TỶ LỆ (%) theo chiều rộng/cao canvas GỐC (1600x900) - canvas gốc
        // luôn giữ đúng tỉ lệ này, còn việc co giãn cho khớp màn hình thực tế do Viewbox
        // đảm nhiệm. Cột trái/phải và hàng 1/2 ứng với 4 giải "Lộc Xuân 1-4" (đúng bố cục
        // 2x2 trong ảnh nền); các hàng > 1 (nếu số vòng > 4) được ngoại suy thêm xuống dưới
        // bằng đúng khoảng cách giữa hàng 1 và hàng 2, do ảnh nền chỉ vẽ sẵn 2 hàng khung.
        private const double COL0_X_FRAC = 0.4453;
        private const double COL1_X_FRAC = 0.7688;
        private const double ROW0_Y_FRAC = 0.4583;
        private const double ROW_GAP_FRAC = 0.2174; // = ROW1_Y_FRAC (0.6758) - ROW0_Y_FRAC


        /// <summary>Đặt vị trí cố định (trên canvas gốc 1600x900) cho 1 ô số giải, ngay khi
        /// nó được tạo ra - không cần chờ SizeChanged vì canvas không đổi kích thước, chỉ có
        /// Viewbox bao ngoài co giãn hình ảnh của toàn khối.</summary>
        private void PositionPrizeBlock(TextBlock block, int index, bool isLastSlot)
        {
            double measureWidth = isLastSlot ? KK_FRAME_WIDTH : double.PositiveInfinity;
            block.Measure(new Size(measureWidth, double.PositiveInfinity));

            double x, y;
            if (isLastSlot)
            {
                x = BASE_WIDTH * KK_X_FRAC;
                y = BASE_HEIGHT * KK_Y_FRAC;
            }
            else
            {
                int row = index / 2;
                int col = index % 2;
                x = BASE_WIDTH * (col == 0 ? COL0_X_FRAC : COL1_X_FRAC);
                y = BASE_HEIGHT * (ROW0_Y_FRAC + row * ROW_GAP_FRAC);
            }

            // Với ô "khuyến khích": block.DesiredSize.Width == KK_FRAME_WIDTH (do đã set Width cố
            // định), nên trừ nửa Width vẫn canh đúng tâm ngang khung; TextAlignment=Center lo phần
            // canh giữa TỪNG DÒNG bên trong. Trừ nửa Height (đã tính đủ số dòng sau khi wrap) canh
            // đúng tâm dọc cho cả khối nhiều dòng.
            Canvas.SetLeft(block, x - block.DesiredSize.Width / 2);
            Canvas.SetTop(block, y - block.DesiredSize.Height / 2);
        }

        private void LoadData()
        {
            try
            {
                if (!string.IsNullOrEmpty(ImagePath))
                {
                    imgPrize.Source = new BitmapImage(new Uri(ImagePath, UriKind.Absolute));
                }

                if (!string.IsNullOrEmpty(Title))
                {
                    txtTitle.Text = Title;
                }
                CenterTitle();

                if (!string.IsNullOrEmpty(LogoPath))
                {
                    imgLogo.Source = new BitmapImage(new Uri(LogoPath, UriKind.Absolute));
                }

                // Cập nhật số giải ban đầu
                for (int i = 0; i < prizeNumbers.Count; i++)
                {
                    UpdatePrizeNumber(i, prizeNumbers[i]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
