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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Load Custom Cursors
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                // Arrow Cursor
                using (var stream = assembly.GetManifestResourceStream("ThinkMine.cursor_arrow.cur"))
                {
                    if (stream != null)
                    {
                        this.Cursor = new System.Windows.Input.Cursor(stream);
                    }
                }

                // Hand Cursor (for buttons)
                using (var stream = assembly.GetManifestResourceStream("ThinkMine.cursor_hand.cur"))
                {
                    if (stream != null)
                    {
                        var handCursor = new System.Windows.Input.Cursor(stream);
                        
                        // Apply to all buttons/interactive elements
                        foreach (var btn in FindVisualChildren<Border>(this))
                        {
                            if (btn.Cursor == System.Windows.Input.Cursors.Hand)
                            {
                                btn.Cursor = handCursor;
                            }
                        }
                        // Also apply to TextBlocks with Hand cursor
                        foreach (var tb in FindVisualChildren<TextBlock>(this))
                        {
                            if (tb.Cursor == System.Windows.Input.Cursors.Hand)
                            {
                                tb.Cursor = handCursor;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to default cursors if loading fails
                System.Diagnostics.Debug.WriteLine($"Cursor load error: {ex.Message}");
            }

            // 2. Welcome / Onboarding Logic
            string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            
            // Show Welcome if First Run OR Version Changed
            if (_settings.IsFirstRun || _settings.LastVersion != currentVersion)
            {
                _settings.IsFullScreen = true; // Force Fullscreen on update/install
                _settings.IsFirstRun = false;
                _settings.LastVersion = currentVersion;
                _settings.Save();
                
                // Slight delay to ensure window is rendered
                Dispatcher.InvokeAsync(async () => 
                {
                    await Task.Delay(500);
                    ShowWelcomeAnimation();
                });
            }
        }

        // Helper to find children
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isExiting) return; // Already handling exit

            // If dirty, show UnsavedOverlay
            if (_isDirty)
            {
                e.Cancel = true; // Stop closing
                UnsavedOverlay.Visibility = Visibility.Visible;
                return;
            }
            
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
                MainEditor.FontFamily = new System.Windows.Media.FontFamily(_settings.CurrentFontFamily);
            }
            catch { }
            
            // Restore Font Size
            if (_settings.FontSize > 0)
            {
                MainEditor.FontSize = _settings.FontSize;
                FontSizeBtn.Text = _settings.FontSize.ToString();
            }
            
            // Restore Bold/Italic
            ApplyStyleState();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Optional: Update settings or layout if needed
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
            // Cycle: Left -> Center (Horizontal) -> Center (Vertical+Horizontal) -> Right -> Justify
            
            var align = MainEditor.Document.TextAlignment;
            var vertical = MainEditor.VerticalContentAlignment;
            
            // Current State Logic
            int state = 0; // 0=Left, 1=Center, 2=Middle(Center+Vertical), 3=Right, 4=Justify
            
            if (align == TextAlignment.Left) state = 0;
            else if (align == TextAlignment.Center && vertical == VerticalAlignment.Top) state = 1;
            else if (align == TextAlignment.Center && vertical == VerticalAlignment.Center) state = 2;
            else if (align == TextAlignment.Right) state = 3;
            else if (align == TextAlignment.Justify) state = 4;

            if (reverse)
            {
                state--;
                if (state < 0) state = 4;
            }
            else
            {
                state++;
                if (state > 4) state = 0;
            }

            // Apply New State
            switch (state)
            {
                case 0: // Left
                    MainEditor.Document.TextAlignment = TextAlignment.Left;
                    MainEditor.VerticalContentAlignment = VerticalAlignment.Top;
                    AlignBtn.Text = "L";
                    AlignBtn.ToolTip = "Align Left";
                    break;
                case 1: // Center
                    MainEditor.Document.TextAlignment = TextAlignment.Center;
                    MainEditor.VerticalContentAlignment = VerticalAlignment.Top;
                    AlignBtn.Text = "C";
                    AlignBtn.ToolTip = "Align Center";
                    break;
                case 2: // Middle (Vertical Center)
                    MainEditor.Document.TextAlignment = TextAlignment.Center;
                    MainEditor.VerticalContentAlignment = VerticalAlignment.Center;
                    AlignBtn.Text = "M";
                    AlignBtn.ToolTip = "Align Middle (Vertical)";
                    break;
                case 3: // Right
                    MainEditor.Document.TextAlignment = TextAlignment.Right;
                    MainEditor.VerticalContentAlignment = VerticalAlignment.Top;
                    AlignBtn.Text = "R";
                    AlignBtn.ToolTip = "Align Right";
                    break;
                case 4: // Justify
                    MainEditor.Document.TextAlignment = TextAlignment.Justify;
                    MainEditor.VerticalContentAlignment = VerticalAlignment.Top;
                    AlignBtn.Text = "J";
                    AlignBtn.ToolTip = "Justify";
                    break;
            }
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
