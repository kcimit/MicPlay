using GlobalLowLevelHooks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace MicPlay
{
 

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel vm;
        private System.Windows.Forms.NotifyIcon m_notifyIcon;
        public MainWindow()
        {
            InitializeComponent();
            vm = new MainViewModel(this);
            if (!vm.CanContinue)
            {
                System.Windows.MessageBox.Show("Error initializing module", "Can not continue", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }    
            this.DataContext = vm;
            m_notifyIcon = new System.Windows.Forms.NotifyIcon();
            m_notifyIcon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
            m_notifyIcon.BalloonTipTitle = "Privgram";
            m_notifyIcon.Text = "Privgram";
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/MicPlay;component/app.ico")).Stream;
            m_notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            m_notifyIcon.Click += new EventHandler(m_notifyIcon_Click);

        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        private const int WM_HOTKEY = 0x0312;
        private const int WH_MOUSE_LL = 14;

        private const int HOTKEY_ID = 9000;

        //Modifiers:
        private const uint MOD_NONE = 0x0000; //(none)
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        //CAPS LOCK:
        private const uint VK_CAPITAL = 0x14;
        //

        private const uint KEY_V = 0x56;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP  = 0x020C;
        private const int WM_NCXBUTTONDOWN = 0x00AB;
        private const int WM_NCXBUTTONUP = 0x00AC;


        private IntPtr _windowHandle;
        private HwndSource _source;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            MouseHook mouseHook = new MouseHook();
            mouseHook. += new MouseHook.MouseHookCallback(mouseHook_MouseMove);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, KEY_V);

        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            
            switch (msg)
            {
                case WM_LBUTTONDOWN:
                    break;
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == KEY_V)
                            {
                                vm.PlayRandom();
                            }
                            handled = true;
                            break;
                    }
                    break;
                case WM_XBUTTONDOWN:
                case WM_NCXBUTTONDOWN:
                    vm.Status2 = msg.ToString();
                    break;
            }
            return IntPtr.Zero;
        }


        void OnClose(object sender, CancelEventArgs args)
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            m_notifyIcon.Dispose();
            m_notifyIcon = null;
        }


        private WindowState m_storedWindowState = WindowState.Normal;
        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                if (m_notifyIcon != null)
                    m_notifyIcon.ShowBalloonTip(2000);
            }
            else
                m_storedWindowState = WindowState;
        }
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }
        public void HideToTray()
        {
            Hide();
            if (m_notifyIcon != null)
                m_notifyIcon.ShowBalloonTip(2000);
            ShowTrayIcon(true);
        }
        void m_notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = m_storedWindowState;
            ShowTrayIcon(false);
        }
        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }
    }
}
