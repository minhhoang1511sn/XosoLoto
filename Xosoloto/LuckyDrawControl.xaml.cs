using System;
using System.Windows;
using System.Windows.Threading;

namespace Xosoloto
{
    public partial class LuckyDrawWindow : Window
    {
        private DispatcherTimer _numberTimer;
        private Random _random;
        private bool _isRolling = false;

        public LuckyDrawWindow()
        {
            InitializeComponent();
            _random = new Random();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _numberTimer = new DispatcherTimer();
            _numberTimer.Interval = TimeSpan.FromMilliseconds(100);
            _numberTimer.Tick += NumberTimer_Tick;
        }

        private void NumberTimer_Tick(object sender, EventArgs e)
        {
            // Animation logic for rolling numbers
        }

        public void StartRolling()
        {
            _isRolling = true;
            _numberTimer.Start();
        }

        public void StopRolling()
        {
            _isRolling = false;
            _numberTimer.Stop();
        }

        public void SetNumbers(int prize1, int prize2, int prize3, int prize4)
        {
            // Set the final numbers for each prize
        }

        public bool EnableAnimations { get; set; } = true;
    }
}