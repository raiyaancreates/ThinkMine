using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Media;
using System.IO;

namespace ThinkMine
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private DispatcherTimer _timer;
        private int _remainingSeconds;
        private bool _isTimerRunning;
        private bool _isTimerPaused;
        private bool _isEditingTimer;
        
        // V2 State
        private bool _isDirty;
        private string _currentFilePath;
        private int _currentBgIndex;
        private int _currentFontIndex;
        private bool _isExiting;

        private readonly string[] CALM_BG_COLORS = new[]
        {
            "#F8FBFF", "#EEF6FF", "#EAF4F6", "#F8F8FA", "#E5E6EB", 
            "#D2D4DC", "#C0C2CE", "#D0E1F9", "#EEE3E7", "#EAD5DC", 
            "#FFF6E9", "#FFF5EE", "#FDF5E6", "#FAEBD7", "#E3F0FF", 
            "#D2E7FF", "#A8E6CF", "#DCEDC1", "#FFD3B6"
        };

        private readonly string[] MINIMAL_FONTS = new[]
        {
            "Inter",            // Clean Sans
            "Space Grotesk",    // Quirky Geometric Sans
            "Quicksand",        // Rounded Sans
            "Playfair Display", // Elegant Serif
            "Merriweather",     // Readable Serif
            "Cormorant Garamond", // Classic Serif
            "Space Mono",       // Geometric Mono
            "Fira Code",        // Tech Mono
            "Courier Prime",    // Typewriter
            "Oswald",           // Tall Display
            "Syne"              // Art-house
        };
        public MainWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            ApplySettings();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Fix: 1 second interval
            _timer.Tick += Timer_Tick;

            MainEditor.TextChanged += (s, e) => 
            {
                _isDirty = true;
            };
        }

        private void ApplySettings()
        {
            // Window
            if (_settings.WindowWidth > 0) Width = _settings.WindowWidth;
            if (_settings.WindowHeight > 0) Height = _settings.WindowHeight;
            Top = _settings.WindowTop;
            Left = _settings.WindowLeft;
            
            if (_settings.IsFullScreen)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }

            // Background
            _currentBgIndex = _settings.BackgroundIndex;
            if (_currentBgIndex >= 0 && _currentBgIndex < CALM_BG_COLORS.Length)
            {
                ApplyBackground(CALM_BG_COLORS[_currentBgIndex]);
            }
            else
            {
                ApplyBackground("#FFFFFF");
            }

            // Font
            _currentFontIndex = Array.IndexOf(MINIMAL_FONTS, _settings.CurrentFontFamily);
            if (_currentFontIndex == -1) _currentFontIndex = MINIMAL_FONTS.Length - 1;
            
            try 
            { 
                var font = new System.Windows.Media.FontFamily(_settings.CurrentFontFamily);
                MainEditor.FontFamily = font;
                FontBtn.ToolTip = _settings.CurrentFontFamily;
            } 
            catch { }

            MainEditor.FontSize = _settings.FontSize;
            FontSizeBtn.Text = _settings.FontSize.ToString();

            // Bold
            ApplyStyleState();

            // Color
            try 
            { 
                MainEditor.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(_settings.LastTextColor); 
            } 
            catch { }

            // Timer
            _remainingSeconds = _settings.LastTimerSeconds;
            UpdateTimerDisplay();
            
            MainEditor.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainEditor.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isExiting) return;

            if (_isDirty)
            {
                e.Cancel = true;
                UnsavedOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
            _settings.WindowTop = Top;
            _settings.WindowLeft = Left;
            _settings.IsFullScreen = WindowState == WindowState.Maximized;
            _settings.CurrentFontFamily = MainEditor.FontFamily.Source;
            _settings.FontSize = MainEditor.FontSize;
            _settings.IsBold = MainEditor.FontWeight == FontWeights.Bold;
            _settings.IsItalic = MainEditor.FontStyle == FontStyles.Italic;
            if (MainEditor.Foreground is SolidColorBrush brush)
            {
                _settings.LastTextColor = brush.Color.ToString();
            }
            _settings.LastTimerSeconds = _remainingSeconds;
            _settings.BackgroundIndex = _currentBgIndex;
            _settings.Save();
        }

        // --- Timer Logic ---

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateTimerDisplay();
                if (_remainingSeconds == 0)
                {
                    TimerFinished();
                }
            }
        }

        private void TimerFinished()
        {
            _isTimerRunning = false;
            _timer.Stop();
            SystemSounds.Beep.Play();
            
            // Flash effect
            var originalBrush = TimerDisplay.Foreground;
            TimerDisplay.Foreground = System.Windows.Media.Brushes.Red;
            
            var flashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            flashTimer.Tick += (s, args) =>
            {
                TimerDisplay.Foreground = originalBrush;
                flashTimer.Stop();
            };
            flashTimer.Start();
        }

        private void UpdateTimerDisplay()
        {
            int m = _remainingSeconds / 60;
            int s = _remainingSeconds % 60;
            TimerDisplay.Text = $"{m:00}:{s:00}";
        }

        private void Timer_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isEditingTimer) return;

            // Always allow editing on left-click
            StartEditingTimer();
        }

        private void Timer_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isEditingTimer)
            {
                StartEditingTimer();
                e.Handled = true;
            }
        }

        private void StartEditingTimer()
        {
            _isEditingTimer = true;
            TimerDisplay.Visibility = Visibility.Collapsed;
            TimerEdit.Visibility = Visibility.Visible;
            TimerEdit.Text = TimerDisplay.Text;
            TimerEdit.SelectAll();
            TimerEdit.Focus();
        }

        private void CommitTimerEdit()
        {
            if (!_isEditingTimer) return;

            string text = TimerEdit.Text;
            if (TryParseTimer(text, out int seconds))
            {
                _remainingSeconds = seconds;
            }

            _isEditingTimer = false;
            TimerEdit.Visibility = Visibility.Collapsed;
            TimerDisplay.Visibility = Visibility.Visible;
            UpdateTimerDisplay();

            if (_remainingSeconds > 0)
            {
                _isTimerRunning = true;
                _isTimerPaused = false;
                _timer.Start();
            }
        }

        private bool TryParseTimer(string text, out int totalSeconds)
        {
            totalSeconds = 0;
            var parts = text.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int m) && int.TryParse(parts[1], out int s))
                {
                    totalSeconds = m * 60 + s;
                    return true;
                }
            }
            else if (parts.Length == 1)
            {
                 if (int.TryParse(parts[0], out int m))
                 {
                     totalSeconds = m * 60;
                     return true;
                 }
            }
            return false;
        }

        private void TimerEdit_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitTimerEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Cancel edit
                _isEditingTimer = false;
                TimerEdit.Visibility = Visibility.Collapsed;
                TimerDisplay.Visibility = Visibility.Visible;
                e.Handled = true;
            }
        }

        private void TimerEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitTimerEdit();
        }

        // --- Controls ---

        private void FontBtn_Click(object sender, MouseButtonEventArgs e)
        {
            CycleFont(false);
        }

        private void FontSizeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            bool reverse = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            CycleFontSize(reverse);
        }

        private void BoldBtn_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleBold();
        }

        private void ItalicBtn_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleItalic();
        }

        private void MinBtn_Click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaxBtn_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleFullScreen();
        }

        private void CloseBtn_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }



        // --- Shortcuts ---

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Zoom
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add)
                {
                    MainEditor.FontSize += 2;
                    e.Handled = true;
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    if (MainEditor.FontSize > 4) MainEditor.FontSize -= 2;
                    e.Handled = true;
                }
                else if (e.Key == Key.F)
                {
                    CycleFont(false);
                    e.Handled = true;
                }
                else if (e.Key == Key.T)
                {
                    StartEditingTimer();
                    e.Handled = true;
                }
                else if (e.Key == Key.S)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        SaveAsDocument();
                    }
                    else
                    {
                        SaveDocument();
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.E)
                {
                    // Export RTF logic could go here
                    e.Handled = true;
                }
            }



            // Fullscreen
            if (e.Key == Key.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }

            // Exit
            if (e.Key == Key.Escape)
            {
                if (!_isEditingTimer)
                {
                    Close();
                }
            }
        }

        private void ToggleFullScreen()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None; // Keep borderless
            }
            else
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
        }

        // --- V2 Interactions ---

        private void Window_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool reverse = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            CycleBackground(reverse);
            e.Handled = true; // Suppress context menu
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                bool reverse = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                CycleFont(reverse);
            }
        }

        // --- Overlay Handlers ---

        private void OverlaySave_Click(object sender, RoutedEventArgs e)
        {
            SaveDocument();
            _isExiting = true;
            Close();
        }

        private void OverlayDontSave_Click(object sender, RoutedEventArgs e)
        {
            _isExiting = true;
            Close();
        }

        private void OverlayCancel_Click(object sender, RoutedEventArgs e)
        {
            UnsavedOverlay.Visibility = Visibility.Collapsed;
        }

        // --- Helpers ---

        private void ApplyBackground(string hexColor)
        {
            try
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor);
                Background = brush;
                MainEditor.Background = System.Windows.Media.Brushes.Transparent;
                UpdateTextColorForContrast(brush.Color);
            }
            catch { }
        }

        private void UpdateTextColorForContrast(System.Windows.Media.Color bgColor)
        {
            double luminance = (0.2126 * bgColor.R + 0.7152 * bgColor.G + 0.0722 * bgColor.B) / 255.0;
            var newColor = luminance < 0.5 ? System.Windows.Media.Colors.White : System.Windows.Media.Colors.Black;
            var brush = new SolidColorBrush(newColor);
            MainEditor.Foreground = brush;
        }

        private void CycleBackground(bool reverse)
        {
            if (reverse)
            {
                _currentBgIndex--;
                if (_currentBgIndex < 0) _currentBgIndex = CALM_BG_COLORS.Length - 1;
            }
            else
            {
                _currentBgIndex++;
                if (_currentBgIndex >= CALM_BG_COLORS.Length) _currentBgIndex = 0;
            }
            ApplyBackground(CALM_BG_COLORS[_currentBgIndex]);
        }

        private void CycleFont(bool reverse)
        {
            if (reverse)
            {
                _currentFontIndex--;
                if (_currentFontIndex < 0) _currentFontIndex = MINIMAL_FONTS.Length - 1;
            }
            else
            {
                _currentFontIndex++;
                if (_currentFontIndex >= MINIMAL_FONTS.Length) _currentFontIndex = 0;
            }

            string fontName = MINIMAL_FONTS[_currentFontIndex];
            try
            {
                MainEditor.FontFamily = new System.Windows.Media.FontFamily(fontName);
                FontBtn.ToolTip = fontName; // Update ToolTip
            }
            catch { }
        }

        private void CycleFontSize(bool reverse)
        {
            double[] sizes = { 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };
            double current = MainEditor.FontSize;
            int index = Array.IndexOf(sizes, current);
            
            if (index == -1) index = 3; // Default to 18 if unknown

            if (reverse)
            {
                index--;
                if (index < 0) index = sizes.Length - 1;
            }
            else
            {
                index++;
                if (index >= sizes.Length) index = 0;
            }

            double newSize = sizes[index];
            MainEditor.FontSize = newSize;
            FontSizeBtn.Text = newSize.ToString();
        }

        private void ToggleBold()
        {
            _settings.IsBold = !_settings.IsBold;
            ApplyStyleState();
        }

        private void ToggleItalic()
        {
            _settings.IsItalic = !_settings.IsItalic;
            ApplyStyleState();
        }

        private void ApplyStyleState()
        {
            MainEditor.FontWeight = _settings.IsBold ? FontWeights.Bold : FontWeights.Normal;
            MainEditor.FontStyle = _settings.IsItalic ? FontStyles.Italic : FontStyles.Normal;
            
            // Visual feedback
            BoldBtn.Opacity = _settings.IsBold ? 1.0 : 0.4;
            ItalicBtn.Opacity = _settings.IsItalic ? 1.0 : 0.4;
        }

        private void SaveDocument()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsDocument();
                return;
            }

            try
            {
                File.WriteAllText(_currentFilePath, new TextRange(MainEditor.Document.ContentStart, MainEditor.Document.ContentEnd).Text);
                _isDirty = false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving: {ex.Message}");
            }
        }

        private void SaveAsDocument()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            dlg.FileName = $"Thoughts-{DateTime.Now:yyyy-MM-dd_HH-mm}";
            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                SaveDocument();
            }
        }
    }
}
