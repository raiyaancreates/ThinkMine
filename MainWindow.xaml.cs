using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Media;
using System.IO;
using System.Windows.Forms; // For SendKeys
using System.Runtime.InteropServices; // For user32.dll
using System.Windows.Media.Animation; // For Animations
using System.Collections.Generic;
using Brushes = System.Windows.Media.Brushes; // Fix ambiguity
using ThinkMine.Services;

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

        // Tutorial State
        private int _tutorialStep = 0;
        private bool _tutorialProcessing = false;
        private readonly (string Element, string Title, string Instruction)[] _tutorialSteps = new[]
        {
            ("AlignBtn", "Text Alignment", "Click to cycle through Left, Center, Right, and Justify alignment. Right-click to go backwards."),
            ("FontBtn", "Font Selection", "Click to cycle through available fonts. Right-click to go backwards."),
            ("FontSizeBtn", "Font Size", "Click to decrease font size. Right-click to increase."),
            ("BoldBtn", "Bold Text", "Click to toggle bold formatting."),
            ("ItalicBtn", "Italic Text", "Click to toggle italic formatting."),
            ("TimerDisplay", "Focus Timer", "Click to start/pause the timer. Right-click to edit the duration."),
            ("MiniModeBtn", "Mini Mode (Docker)", "Click to enter Mini Mode - a compact overlay that stays on top of other windows."),
            ("LibraryBtn", "Library", "Access your saved files, create new documents, and manage your work."),
        };

        private readonly string[] CALM_BG_COLORS = new[]
        {
            "#F8FBFF", "#EEF6FF", "#EAF4F6", "#F8F8FA", "#E5E6EB", 
            "#D2D4DC", "#C0C2CE", "#D0E1F9", "#EEE3E7", "#EAD5DC", 
            "#FFF6E9", "#FFF5EE", "#FDF5E6", "#FAEBD7", "#E3F0FF", 
            "#D2E7FF", "#A8E6CF", "#DCEDC1", "#FFD3B6"
        };

        private readonly string[] MINIMAL_FONTS = new[]
        {
            "Inter", "Space Grotesk", "Quicksand", "Playfair Display", 
            "Merriweather", "Cormorant Garamond", "Space Mono", 
            "Fira Code", "Courier Prime", "Oswald", "Syne"
        };

        // P/Invoke for Smart Copy
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public MainWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            ApplySettings();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            MainEditor.TextChanged += (s, e) => 
            {
                _isDirty = true;
                UpdateDockerPreview();
            };

            // Onboarding
            string currentVersion = "1.1.1";
            if (_settings.IsFirstRun || _settings.LastVersion != currentVersion)
            {
                _settings.IsFullScreen = true; // Force Fullscreen
                _settings.IsFirstRun = false;
                _settings.LastVersion = currentVersion;
                _settings.Save();
                ShowWelcomeAnimation();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isExiting = true;
            _settings.Save();
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
                // Force update just in case
                this.WindowState = WindowState.Maximized;
            }

            // Background
            _currentBgIndex = _settings.BackgroundIndex;
            if (_currentBgIndex >= 0 && _currentBgIndex < CALM_BG_COLORS.Length)
                ApplyBackground(CALM_BG_COLORS[_currentBgIndex]);
            else
                ApplyBackground("#FFFFFF");

            // Font
            _currentFontIndex = Array.IndexOf(MINIMAL_FONTS, _settings.CurrentFontFamily);
            if (_currentFontIndex == -1) _currentFontIndex = 0;
            
            try 
            { 
                var font = new System.Windows.Media.FontFamily(_settings.CurrentFontFamily);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Optional: Update settings or layout if needed
        }

        private void UpdateDateTime()
        {
            DateTimeDisplay.Text = DateTime.Now.ToString("dddd, MMM dd | h:mm tt");
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

        // --- Onboarding ---

        private void ShowWelcomeAnimation()
        {
            WelcomeOverlay.Visibility = Visibility.Visible;
            string message = "Welcome to ThinkMine :P";
            WelcomeText.Text = "";
            
            int index = 0;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += (s, e) =>
            {
                if (index < message.Length)
                {
                    WelcomeText.Text += message[index];
                    index++;
                }
                else
                {
                    timer.Stop();
                }
            };
            timer.Start();
        }

        private void WelcomeOverlay_Click(object sender, MouseButtonEventArgs e)
        {
            WelcomeOverlay.Visibility = Visibility.Collapsed;
            if (!_settings.HasSeenTutorial)
            {
                ShowTutorial();
            }
        }

        // --- Tutorial ---

        private void ShowTutorial()
        {
            TutorialOverlay.Visibility = Visibility.Visible;
            _tutorialStep = 0;
            ShowTutorialStep();
        }

        private void ShowTutorialStep()
        {
            if (_tutorialStep >= _tutorialSteps.Length)
            {
                // End
                TutorialOverlay.Visibility = Visibility.Collapsed;
                _settings.HasSeenTutorial = true;
                _settings.Save();
                return;
            }

            var step = _tutorialSteps[_tutorialStep];
            TutorialTitle.Text = step.Title;
            TutorialInstruction.Text = step.Instruction;
            
            HighlightElement(step.Element);
        }

        private void HighlightElement(string elementName)
        {
            var element = FindName(elementName) as FrameworkElement;
            if (element != null)
            {
                System.Windows.Point p = element.TranslatePoint(new System.Windows.Point(0, 0), MainGrid);
                TutorialSpotlight.Width = element.ActualWidth + 10;
                TutorialSpotlight.Height = element.ActualHeight + 10;
                TutorialSpotlight.Margin = new Thickness(p.X - 5, p.Y - 5, 0, 0);
                TutorialSpotlight.Visibility = Visibility.Visible;
            }
            else
            {
                TutorialSpotlight.Visibility = Visibility.Collapsed;
            }
        }
        
        // Helper property to find MainGrid (assuming root grid has no name, we use MainBackground's parent)
        private Grid MainGrid => (Grid)MainBackground.Parent;

        private void TutorialNext_Click(object sender, RoutedEventArgs e)
        {
            if (_tutorialProcessing) return;
            _tutorialProcessing = true;
            
            _tutorialStep++;
            ShowTutorialStep();
            
            // Debounce to prevent double-clicks skipping steps
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, args) =>
            {
                _tutorialProcessing = false;
                timer.Stop();
            };
            timer.Start();
        }

        private void TutorialSkip_Click(object sender, RoutedEventArgs e)
        {
            TutorialOverlay.Visibility = Visibility.Collapsed;
            _settings.HasSeenTutorial = true;
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
            
            if (_isTimerRunning)
            {
                _isTimerPaused = true;
                _isTimerRunning = false;
                _timer.Stop();
                TimerDisplay.Opacity = 0.5;
            }
            else
            {
                _isTimerRunning = true;
                _isTimerPaused = false;
                _timer.Start();
                TimerDisplay.Opacity = 1.0;
            }
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
        
        private void FontBtn_RightClick(object sender, MouseButtonEventArgs e)
        {
            CycleFont(true);
            e.Handled = true;
        }

        private void FontSizeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            // Left Click = Decrease (Reverse = true)
            CycleFontSize(true);
        }
        
        private void FontSizeBtn_RightClick(object sender, MouseButtonEventArgs e)
        {
            // Right Click = Increase (Reverse = false)
            CycleFontSize(false);
            e.Handled = true;
        }
        
        private void AlignBtn_Click(object sender, MouseButtonEventArgs e)
        {
            CycleAlignment(false);
        }
        
        private void AlignBtn_RightClick(object sender, MouseButtonEventArgs e)
        {
            CycleAlignment(true);
            e.Handled = true;
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

        // --- Library Logic ---

        private void LibraryBtn_Click(object sender, MouseButtonEventArgs e)
        {
            LibraryOverlay.Visibility = Visibility.Visible;
            PopulateRecentFiles();
        }

        private void LibraryClose_Click(object sender, MouseButtonEventArgs e)
        {
            LibraryOverlay.Visibility = Visibility.Collapsed;
            
            // Reset Timer to 5:00 on New File
            _remainingSeconds = 300;
            UpdateTimerDisplay();

            LibraryOverlay.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Visible;
        }

        private void LibraryOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            if (dlg.ShowDialog() == true)
            {
                LoadDocument(dlg.FileName);
                LibraryOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void LibrarySave_Click(object sender, RoutedEventArgs e)
        {
            // Default action
            SaveDocument();
            LibraryOverlay.Visibility = Visibility.Collapsed;
        }

        private void LibrarySaveAction_Click(object sender, RoutedEventArgs e)
        {
            SaveDocument();
            LibraryOverlay.Visibility = Visibility.Collapsed;
        }

        private void LibrarySaveAsAction_Click(object sender, RoutedEventArgs e)
        {
            SaveAsDocument();
            LibraryOverlay.Visibility = Visibility.Collapsed;
        }

        private void PopulateRecentFiles()
        {
            RecentFilesList.Children.Clear();
            
            if (_settings.RecentFiles != null && _settings.RecentFiles.Count > 0)
            {
                foreach (var file in _settings.RecentFiles)
                {
                    if (File.Exists(file))
                    {
                        AddRecentFileItem(file);
                    }
                }
            }
            else
            {
                var empty = new TextBlock { Text = "No recent files", Foreground = Brushes.Gray, FontStyle = FontStyles.Italic, Margin = new Thickness(0, 10, 0, 10) };
                RecentFilesList.Children.Add(empty);
            }
        }

        private void AddToRecentFiles(string path)
        {
            if (_settings.RecentFiles == null) _settings.RecentFiles = new List<string>();
            
            // Remove if exists (to move to top)
            _settings.RecentFiles.Remove(path);
            
            // Add to top
            _settings.RecentFiles.Insert(0, path);
            
            // Limit to 10
            if (_settings.RecentFiles.Count > 10)
                _settings.RecentFiles.RemoveAt(_settings.RecentFiles.Count - 1);
                
            _settings.Save();
        }

        private void AddRecentFileItem(string path)
        {
            var item = new Border { Padding = new Thickness(10), Background = Brushes.Transparent, Cursor = System.Windows.Input.Cursors.Hand, Margin = new Thickness(0, 0, 0, 5) };
            item.MouseLeftButtonDown += (s, e) => 
            {
                LoadDocument(path);
                LibraryOverlay.Visibility = Visibility.Collapsed;
            };
            
            var text = new TextBlock { Text = GetDisplayName(path), FontSize = 14, Foreground = Brushes.Black }; // Ensure visible text
            item.Child = text;
            
            RecentFilesList.Children.Add(item);
        }

        private string GetDisplayName(string path)
        {
            string name = Path.GetFileName(path);
            if (name.EndsWith(".tm")) return name.Substring(0, name.Length - 3);
            if (name.EndsWith(".txt")) return name.Substring(0, name.Length - 4);
            return name;
        }

        // --- Docker Logic ---

        private bool _isDockerDrawerOpen = true;

        private void MiniModeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleMiniMode();
        }

        private void ToggleMiniMode()
        {
            DockerOverlay.Visibility = Visibility.Visible;
            Topmost = true;
            
            // Make Window Transparent for Docker Mode
            Background = Brushes.Transparent;

            // Hide main controls
            TopControls.Visibility = Visibility.Collapsed;
            BottomInfoPanel.Visibility = Visibility.Collapsed;
            MainEditor.Visibility = Visibility.Collapsed;
            MainBackground.Visibility = Visibility.Collapsed;
            
            // Reset Drawer
            _isDockerDrawerOpen = true;
            DockerTransform.Y = 0;
            DockerTabIcon.Text = "^";
            
            UpdateDockerPreview();
        }

        private void FullScreenBtn_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleFullMode();
        }

        private void ToggleFullMode()
        {
            DockerOverlay.Visibility = Visibility.Collapsed;
            Topmost = false;
            
            // Restore Background Color
            if (_currentBgIndex >= 0 && _currentBgIndex < CALM_BG_COLORS.Length)
                ApplyBackground(CALM_BG_COLORS[_currentBgIndex]);
            else
                ApplyBackground("#FFFFFF");

            // Show main controls
            TopControls.Visibility = Visibility.Visible;
            BottomInfoPanel.Visibility = Visibility.Visible;
            MainEditor.Visibility = Visibility.Visible;
            MainBackground.Visibility = Visibility.Visible;
        }

        private void DockerTab_Click(object sender, MouseButtonEventArgs e)
        {
            double targetY = 0;
            
            if (_isDockerDrawerOpen)
            {
                // Close (Slide Up)
                targetY = -DockerContent.ActualHeight;
                DockerTabIcon.Text = "v"; 
                _isDockerDrawerOpen = false;
            }
            else
            {
                // Open (Slide Down)
                targetY = 0;
                DockerTabIcon.Text = "^"; 
                _isDockerDrawerOpen = true;
            }

            var animation = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            DockerTransform.BeginAnimation(TranslateTransform.YProperty, animation);
        }

        private void UpdateDockerPreview()
        {
            string text = new TextRange(MainEditor.Document.ContentStart, MainEditor.Document.ContentEnd).Text;
            if (string.IsNullOrWhiteSpace(text)) text = "Start writing...";
            DockerPreviewText.Text = text.Trim();
            DockerPreviewText.TextAlignment = TextAlignment.Center; // Fix: Center text
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DockerOverlay != null)
                DockerOverlay.Opacity = e.NewValue;
        }

        private void CopyBtn_Click(object sender, MouseButtonEventArgs e)
        {
            // Smart Copy
            WindowState = WindowState.Minimized;
            
            // Wait for minimize
            Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(200);
                SendKeys.SendWait("^c"); // Ctrl+C
                await Task.Delay(200);
                
                WindowState = WindowState.Normal;
                Activate();
                
                // Show feedback (Green Text)
                var originalBrush = DockerPreviewText.Foreground;
                string originalText = DockerPreviewText.Text;
                
                DockerPreviewText.Foreground = Brushes.Green;
                DockerPreviewText.Text = "Copied!";
                
                await Task.Delay(1000);
                
                DockerPreviewText.Foreground = originalBrush;
                DockerPreviewText.Text = originalText;
            });
        }

        // --- Shortcuts & Window ---

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
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
                else if (e.Key == Key.S)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        SaveAsDocument();
                    else
                        SaveDocument();
                    e.Handled = true;
                }
            }

            if (e.Key == Key.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                if (!_isEditingTimer) 
                {
                    // Minimize instead of Exit
                    WindowState = WindowState.Minimized;
                }
            }
        }

        private void ToggleFullScreen()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
            }
            else
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
        }

        private void Window_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Check if we clicked a control that handles its own right-click
            if (e.OriginalSource is FrameworkElement fe && 
               (fe.Name.Contains("Btn") || fe.Name == "TimerDisplay" || fe.Name == "TimerEdit"))
            {
                return;
            }

            bool reverse = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            CycleBackground(reverse);
            e.Handled = true;
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
                if (MainBackground != null) MainBackground.Background = brush; // Fix: Update the visible border
                MainEditor.Background = Brushes.Transparent;
                UpdateTextColorForContrast(brush.Color);
            }
            catch { }
        }

        private void UpdateTextColorForContrast(System.Windows.Media.Color bgColor)
        {
            double luminance = (0.2126 * bgColor.R + 0.7152 * bgColor.G + 0.0722 * bgColor.B) / 255.0;
            var newColor = luminance < 0.5 ? Colors.White : Colors.Black;
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
                FontBtn.ToolTip = fontName;
                FontBtn.Text = fontName;
            }
            catch { }
        }
        
        private void CycleAlignment(bool reverse)
        {
            var align = MainEditor.Document.TextAlignment;
            if (reverse)
            {
                if (align == TextAlignment.Left) align = TextAlignment.Justify;
                else if (align == TextAlignment.Justify) align = TextAlignment.Right;
                else if (align == TextAlignment.Right) align = TextAlignment.Center;
                else align = TextAlignment.Left;
            }
            else
            {
                if (align == TextAlignment.Left) align = TextAlignment.Center;
                else if (align == TextAlignment.Center) align = TextAlignment.Right;
                else if (align == TextAlignment.Right) align = TextAlignment.Justify;
                else align = TextAlignment.Left;
            }
            MainEditor.Document.TextAlignment = align;
            
            string label = "L";
            if (align == TextAlignment.Center) label = "C";
            if (align == TextAlignment.Right) label = "R";
            if (align == TextAlignment.Justify) label = "J";
            AlignBtn.Text = label;
        }

        private void CycleFontSize(bool reverse)
        {
            double[] sizes = { 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };
            double current = MainEditor.FontSize;
            int index = Array.IndexOf(sizes, current);
            
            if (index == -1) index = 8; // Default to 36

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
                AddToRecentFiles(_currentFilePath); // Add to recent
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving: {ex.Message}");
            }
        }

        private void SaveAsDocument()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "ThinkMine Files (*.tm)|*.tm|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            dlg.FileName = $"Thoughts-{DateTime.Now:yyyy-MM-dd_HH-mm}";
            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                SaveDocument();
            }
        }
        
        private void LoadDocument(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string text = File.ReadAllText(path);
                    MainEditor.Document.Blocks.Clear();
                    MainEditor.Document.Blocks.Add(new Paragraph(new Run(text)));
                    _currentFilePath = path;
                    _isDirty = false;
                    
                    PlaceholderText.Visibility = Visibility.Collapsed;
                    AddToRecentFiles(path); // Add to recent
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading: {ex.Message}");
            }
        }
        private async void CheckForUpdatesAsync()
        {
            var update = await _updateService.CheckForUpdate();
            if (update != null)
            {
                _updateUrl = update.Url;
                UpdateBtn.Visibility = Visibility.Visible;
            }
        }

        private void UpdateBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(_updateUrl))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _updateUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }
    }
}
