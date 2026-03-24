using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using NullFrame.Models;
using NullFrame.Services;
using NullFrame.Tweaks;

namespace NullFrame
{
    // ── Value Converters ─────────────────────────────────────────────────────

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is true ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class StatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is true
                ? (SolidColorBrush)Application.Current.Resources["Success"]
                : (SolidColorBrush)Application.Current.Resources["NfBright"];
        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class TweakTypeVisConverter : IValueConverter
    {
        public TweakType VisibleType { get; set; }
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is TweakType tt && tt == VisibleType ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is true ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Category definition ──────────────────────────────────────────────────

    public record Category(string Name, string Key, string Number, string Subtitle);

    // ── Main Window ──────────────────────────────────────────────────────────

    public partial class MainWindow : Window
    {
        private static readonly Category[] Categories =
        {
            new("CPU & SYSTEM",      "cpu",     "01", "Optimize CPU scheduling and system performance."),
            new("GPU & GAMING",      "gpu",     "02", "Optimize your GPU and gaming performance."),
            new("NETWORK",           "network", "03", "Optimize network settings and performance."),
            new("USB & INPUT",       "usb",     "04", "Reduce input latency for peripherals."),
            new("DEVICES",           "devices", "05", "Disable unnecessary device drivers."),
            new("STORAGE & SSD",     "storage", "06", "Optimize disk and SSD performance."),
            new("MEMORY & RAM",      "memory",  "07", "Optimize memory and RAM usage."),
            new("PRIVACY & DEBLOAT", "privacy", "08", "Remove telemetry and bloatware."),
            new("BACKUP & RESTORE",  "backup",  "09", "Back up your system settings and restore them."),
        };

        private readonly Dictionary<string, List<Tweak>> _tweakMap;
        private readonly Dictionary<string, Button> _navButtons = new();
        private string _activeKey = "";

        public MainWindow()
        {
            // Register converters before InitializeComponent
            Application.Current.Resources["BoolVis"] = new BoolToVisibilityConverter();
            Application.Current.Resources["InvBoolVis"] = new InverseBoolToVisibilityConverter();
            Application.Current.Resources["StatusBrush"] = new StatusBrushConverter();
            Application.Current.Resources["ToggleVis"] = new TweakTypeVisConverter { VisibleType = TweakType.Toggle };
            Application.Current.Resources["ApplyVis"] = new TweakTypeVisConverter { VisibleType = TweakType.Apply };
            Application.Current.Resources["PresetVis"] = new TweakTypeVisConverter { VisibleType = TweakType.Preset };

            InitializeComponent();

            _tweakMap = new Dictionary<string, List<Tweak>>
            {
                ["cpu"]     = CpuSystemTweaks.GetTweaks(),
                ["gpu"]     = GpuGamingTweaks.GetTweaks(),
                ["network"] = NetworkTweaks.GetTweaks(),
                ["usb"]     = UsbInputTweaks.GetTweaks(),
                ["devices"] = DeviceTweaks.GetTweaks(),
                ["storage"] = StorageTweaks.GetTweaks(),
                ["memory"]  = MemoryTweaks.GetTweaks(),
                ["privacy"] = PrivacyTweaks.GetTweaks(),
            };

            BuildNavButtons();
            Navigate("cpu");
        }

        // ── Sidebar navigation ───────────────────────────────────────────────

        private void BuildNavButtons()
        {
            foreach (var cat in Categories)
            {
                var btn = new Button
                {
                    Content = $"  {cat.Number}  {cat.Name}",
                    Style = (Style)FindResource("NavButton"),
                    Tag = cat.Key
                };
                btn.Click += (s, e) => Navigate(cat.Key);
                NavPanel.Children.Add(btn);
                _navButtons[cat.Key] = btn;
            }
        }

        private void Navigate(string key)
        {
            _activeKey = key;

            // Update nav button styles
            foreach (var (k, btn) in _navButtons)
            {
                btn.Style = (Style)FindResource(k == key ? "NavButtonActive" : "NavButton");
            }

            // Find category info
            var cat = Categories.First(c => c.Key == key);

            // Clear search
            SearchBox.Text = "";

            HeaderTitle.Text = cat.Name;
            HeaderSubtitle.Text = cat.Subtitle;

            if (key == "backup")
            {
                TweakScroller.Visibility = Visibility.Collapsed;
                BackupScroller.Visibility = Visibility.Visible;
                HeaderCount.Text = "SYSTEM RESTORE";
                BuildBackupPage();
            }
            else
            {
                TweakScroller.Visibility = Visibility.Visible;
                BackupScroller.Visibility = Visibility.Collapsed;

                var tweaks = _tweakMap.GetValueOrDefault(key) ?? new List<Tweak>();
                HeaderCount.Text = $"{tweaks.Count} TWEAKS";
                TweakList.ItemsSource = tweaks;
                LoadTweakStates(tweaks);
            }
        }

        // ── Search ────────────────────────────────────────────────────────────

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (_activeKey == "backup") return;
            var query = SearchBox.Text.Trim().ToLower();
            var tweaks = _tweakMap.GetValueOrDefault(_activeKey) ?? new List<Tweak>();

            if (string.IsNullOrEmpty(query))
            {
                TweakList.ItemsSource = tweaks;
                HeaderCount.Text = $"{tweaks.Count} TWEAKS";
            }
            else
            {
                var filtered = tweaks.Where(t =>
                    t.Name.ToLower().Contains(query) ||
                    t.Description.ToLower().Contains(query)).ToList();
                TweakList.ItemsSource = filtered;
                HeaderCount.Text = $"{filtered.Count} / {tweaks.Count} TWEAKS";
            }
        }

        private async void LoadTweakStates(List<Tweak> tweaks)
        {
            foreach (var tweak in tweaks.Where(t => t.Type == TweakType.Toggle && t.Check != null))
            {
                var t = tweak;
                try
                {
                    bool state = await Task.Run(() => t.Check!());
                    t.IsEnabled = state;
                }
                catch { }
            }
        }

        // ── Tweak card event handlers ────────────────────────────────────────

        private async void OnToggleClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton toggle) return;
            if (toggle.DataContext is not Tweak tweak) return;

            var fn = tweak.IsEnabled ? tweak.Enable : tweak.Disable;
            if (fn == null) return;

            tweak.IsBusy = true;
            try
            {
                bool ok = await Task.Run(() => fn());
                tweak.StatusText = ok ? "APPLIED \u2713" : "FAILED \u2717";
                tweak.StatusSuccess = ok;
            }
            catch
            {
                tweak.StatusText = "FAILED \u2717";
                tweak.StatusSuccess = false;
            }
            tweak.IsBusy = false;

            // Clear status after 3 seconds
            _ = ClearStatusAfterDelay(tweak);
        }

        private async void OnApplyClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not Tweak tweak) return;
            if (tweak.ApplyAction == null) return;

            tweak.IsBusy = true;
            try
            {
                bool ok = await Task.Run(() => tweak.ApplyAction());
                tweak.StatusText = ok ? "APPLIED \u2713" : "FAILED \u2717";
                tweak.StatusSuccess = ok;
            }
            catch
            {
                tweak.StatusText = "FAILED \u2717";
                tweak.StatusSuccess = false;
            }
            tweak.IsBusy = false;

            _ = ClearStatusAfterDelay(tweak);
        }

        private void OnConfigureClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not Tweak tweak) return;
            if (tweak.Presets == null) return;

            var dialog = new PresetDialog(tweak) { Owner = this };
            dialog.ShowDialog();

            // Refresh status after preset applied
            _ = ClearStatusAfterDelay(tweak);
        }

        private static async Task ClearStatusAfterDelay(Tweak tweak)
        {
            await Task.Delay(3200);
            tweak.StatusText = "";
        }

        // ── Backup & Restore page ─────────────────────────────────────────────

        private StackPanel? _restorePointList;

        private void BuildBackupPage()
        {
            BackupPanel.Children.Clear();

            // ── Section 1: Create Restore Point ───────────────────────────────
            var createCard = MakeCard();
            var createStack = new StackPanel { Margin = new Thickness(16, 14, 16, 14) };

            createStack.Children.Add(MakeLabel("CREATE RESTORE POINT", 13, "Text", FontWeights.Bold));
            createStack.Children.Add(MakeLabel("Create a new Windows System Restore point before applying tweaks.", 11, "TextDim"));

            var inputRow = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameBox = new TextBox
            {
                Text = "NullFrame Backup",
                FontSize = 12,
                Padding = new Thickness(12, 8, 12, 8),
                Background = Res<Brush>("Surface"),
                Foreground = Res<Brush>("Text"),
                BorderBrush = Res<Brush>("BorderMid"),
                BorderThickness = new Thickness(1),
                CaretBrush = Res<Brush>("Text"),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameBox, 0);
            inputRow.Children.Add(nameBox);

            var createBtn = new Button
            {
                Content = "CREATE  \u25B6",
                Style = (Style)FindResource("ApplyButton"),
                MinWidth = 120,
                Height = 36,
                Margin = new Thickness(10, 0, 0, 0)
            };
            var createStatus = MakeLabel("", 10, "Success", FontWeights.Bold);
            createStatus.Margin = new Thickness(0, 8, 0, 0);

            createBtn.Click += async (s, e) =>
            {
                createBtn.IsEnabled = false;
                createBtn.Content = "CREATING...";
                var desc = nameBox.Text.Trim();
                if (string.IsNullOrEmpty(desc)) desc = "NullFrame Backup";

                bool ok = await Task.Run(() => BackupService.CreateRestorePoint(desc));
                createStatus.Text = ok ? "RESTORE POINT CREATED \u2713" : "FAILED TO CREATE \u2717";
                createStatus.Foreground = ok ? Res<Brush>("Success") : Res<Brush>("NfBright");

                createBtn.Content = "CREATE  \u25B6";
                createBtn.IsEnabled = true;

                // Refresh the list
                if (ok) await RefreshRestorePoints();

                await Task.Delay(3200);
                createStatus.Text = "";
            };
            Grid.SetColumn(createBtn, 1);
            inputRow.Children.Add(createBtn);

            createStack.Children.Add(inputRow);
            createStack.Children.Add(createStatus);
            createCard.Child = createStack;
            BackupPanel.Children.Add(createCard);

            // ── Section 2: Open System Restore Wizard ─────────────────────────
            var wizardCard = MakeCard();
            var wizardStack = new StackPanel { Margin = new Thickness(16, 14, 16, 14) };

            wizardStack.Children.Add(MakeLabel("SYSTEM RESTORE WIZARD", 13, "Text", FontWeights.Bold));
            wizardStack.Children.Add(MakeLabel("Open the Windows System Restore interface to roll back to a previous restore point.", 11, "TextDim"));

            var wizardBtn = new Button
            {
                Content = "OPEN SYSTEM RESTORE",
                Style = (Style)FindResource("ConfigButton"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 12, 0, 0),
                MinWidth = 200
            };
            wizardBtn.Click += (s, e) => Task.Run(() => BackupService.OpenSystemRestore());
            wizardStack.Children.Add(wizardBtn);

            wizardCard.Child = wizardStack;
            BackupPanel.Children.Add(wizardCard);

            // ── Section 3: Existing Restore Points ────────────────────────────
            var listCard = MakeCard();
            var listStack = new StackPanel { Margin = new Thickness(16, 14, 16, 14) };

            var listHeader = new Grid();
            listHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            listHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var listTitle = MakeLabel("EXISTING RESTORE POINTS", 13, "Text", FontWeights.Bold);
            Grid.SetColumn(listTitle, 0);
            listHeader.Children.Add(listTitle);

            var refreshBtn = new Button
            {
                Content = "REFRESH",
                Style = (Style)FindResource("ConfigButton"),
                MinWidth = 90
            };
            refreshBtn.Click += async (s, e) =>
            {
                refreshBtn.IsEnabled = false;
                refreshBtn.Content = "LOADING...";
                await RefreshRestorePoints();
                refreshBtn.Content = "REFRESH";
                refreshBtn.IsEnabled = true;
            };
            Grid.SetColumn(refreshBtn, 1);
            listHeader.Children.Add(refreshBtn);

            listStack.Children.Add(listHeader);

            _restorePointList = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            listStack.Children.Add(_restorePointList);

            listCard.Child = listStack;
            BackupPanel.Children.Add(listCard);

            // Load restore points
            _ = RefreshRestorePoints();
        }

        private async Task RefreshRestorePoints()
        {
            if (_restorePointList == null) return;

            _restorePointList.Children.Clear();
            _restorePointList.Children.Add(MakeLabel("Loading restore points...", 11, "TextDim"));

            var points = await Task.Run(() => BackupService.GetRestorePoints());

            _restorePointList.Children.Clear();

            if (points.Count == 0)
            {
                _restorePointList.Children.Add(MakeLabel("No restore points found.", 11, "TextDim"));
                return;
            }

            foreach (var pt in points)
            {
                var row = new Border
                {
                    Background = Res<Brush>("Surface"),
                    BorderBrush = Res<Brush>("BorderBr"),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 4, 0, 4),
                    Padding = new Thickness(14)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Sequence badge
                var badge = new Border
                {
                    Background = Res<Brush>("NfDeep"),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = $"#{pt.SequenceNumber}",
                    FontSize = 11, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    FontFamily = Res<FontFamily>("HeadingFont")
                };
                Grid.SetColumn(badge, 0);
                grid.Children.Add(badge);

                // Info
                var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(MakeLabel(pt.Description.ToUpper(), 12, "Text", FontWeights.Bold));

                var detailPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
                detailPanel.Children.Add(MakeLabel(pt.CreationTime, 10, "TextDim"));
                detailPanel.Children.Add(MakeLabel("  \u2022  ", 10, "TextDim"));
                detailPanel.Children.Add(MakeLabel(pt.RestorePointType, 10, "TextDim"));
                info.Children.Add(detailPanel);

                Grid.SetColumn(info, 1);
                grid.Children.Add(info);

                // Restore button
                var seqNum = pt.SequenceNumber;
                var restoreBtn = new Button
                {
                    Content = "RESTORE",
                    Style = (Style)FindResource("ConfigButton"),
                    MinWidth = 90,
                    VerticalAlignment = VerticalAlignment.Center
                };
                restoreBtn.Click += async (s, e) =>
                {
                    var result = MessageBox.Show(
                        $"Restore to point #{seqNum}?\n\nThis will restart your computer and roll back system changes.",
                        "CONFIRM RESTORE",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        restoreBtn.IsEnabled = false;
                        restoreBtn.Content = "RESTORING...";
                        await Task.Run(() => BackupService.RestoreToPoint(seqNum));
                    }
                };
                Grid.SetColumn(restoreBtn, 2);
                grid.Children.Add(restoreBtn);

                row.Child = grid;
                _restorePointList.Children.Add(row);
            }
        }

        // ── UI helpers for backup page ────────────────────────────────────────

        private static Border MakeCard()
        {
            return new Border
            {
                Background = (Brush)Application.Current.Resources["Card"],
                BorderBrush = (Brush)Application.Current.Resources["BorderBr"],
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        private static TextBlock MakeLabel(string text, double size, string brushKey, FontWeight? weight = null)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                Foreground = (Brush)Application.Current.Resources[brushKey],
                FontWeight = weight ?? FontWeights.Normal,
                FontFamily = (FontFamily)Application.Current.Resources["HeadingFont"],
                TextWrapping = TextWrapping.Wrap
            };
        }

        private static T Res<T>(string key) => (T)Application.Current.Resources[key];
    }

    // ── Preset Dialog ────────────────────────────────────────────────────────

    public class PresetDialog : Window
    {
        private readonly Tweak _tweak;
        private int _selectedIndex;

        public PresetDialog(Tweak tweak)
        {
            _tweak = tweak;
            Title = tweak.Name.ToUpper();
            Width = 520;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0x08, 0x08, 0x0A));
            WindowStyle = WindowStyle.ToolWindow;

            BuildUI();
        }

        private void BuildUI()
        {
            var root = new StackPanel();

            // Red accent line
            root.Children.Add(new Border { Height = 3, Background = Res<Brush>("NfRed") });

            // Header
            var hdr = new Border { Background = Res<Brush>("Panel"), Padding = new Thickness(24, 18, 24, 18) };
            var hdrStack = new StackPanel();
            hdrStack.Children.Add(new TextBlock
            {
                Text = "CONFIGURE PRESET",
                FontSize = 9, FontWeight = FontWeights.Bold,
                Foreground = Res<Brush>("NfBright"),
                FontFamily = Res<FontFamily>("HeadingFont")
            });
            hdrStack.Children.Add(new TextBlock
            {
                Text = _tweak.Name.ToUpper(),
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = Res<Brush>("Text"),
                FontFamily = Res<FontFamily>("HeadingFont"),
                Margin = new Thickness(0, 6, 0, 4)
            });
            hdrStack.Children.Add(new TextBlock
            {
                Text = _tweak.Description,
                FontSize = 11, Foreground = Res<Brush>("TextDim"),
                TextWrapping = TextWrapping.Wrap
            });
            hdr.Child = hdrStack;
            root.Children.Add(hdr);

            // Divider
            root.Children.Add(new Border { Height = 1, Background = Res<Brush>("BorderMid") });

            // Label
            root.Children.Add(new TextBlock
            {
                Text = "PRE-SET VALUES",
                FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = Res<Brush>("TextDim"),
                Margin = new Thickness(24, 14, 0, 8)
            });

            // Find active/recommended preset
            _selectedIndex = 0;
            if (_tweak.Presets != null)
            {
                for (int i = 0; i < _tweak.Presets.Length; i++)
                {
                    if (_tweak.Presets[i].Check != null)
                    {
                        try { if (_tweak.Presets[i].Check!()) { _selectedIndex = i; break; } } catch { }
                    }
                }
                if (_selectedIndex == 0)
                {
                    for (int i = 0; i < _tweak.Presets.Length; i++)
                    {
                        if (_tweak.Presets[i].Recommended) { _selectedIndex = i; break; }
                    }
                }
            }

            // Preset radio buttons
            var presetPanel = new StackPanel { Margin = new Thickness(20, 0, 20, 10) };
            if (_tweak.Presets != null)
            {
                for (int i = 0; i < _tweak.Presets.Length; i++)
                {
                    var preset = _tweak.Presets[i];
                    int idx = i;
                    var row = new Border
                    {
                        Background = Res<Brush>("Card"),
                        BorderBrush = Res<Brush>("BorderBr"),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(0, 4, 0, 4),
                        Padding = new Thickness(14)
                    };
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var rb = new RadioButton
                    {
                        IsChecked = i == _selectedIndex,
                        GroupName = "Presets",
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 12, 0)
                    };
                    rb.Checked += (s, e) => _selectedIndex = idx;
                    Grid.SetColumn(rb, 0);
                    grid.Children.Add(rb);

                    var info = new StackPanel();
                    var nameLabel = preset.Recommended
                        ? $"{preset.Name.ToUpper()}  \u25C6 RECOMMENDED"
                        : preset.Name.ToUpper();
                    info.Children.Add(new TextBlock
                    {
                        Text = nameLabel,
                        FontSize = 12, FontWeight = FontWeights.Bold,
                        Foreground = preset.Recommended ? Res<Brush>("NfBright") : Res<Brush>("Text"),
                        FontFamily = Res<FontFamily>("HeadingFont")
                    });
                    if (!string.IsNullOrEmpty(preset.Description))
                    {
                        info.Children.Add(new TextBlock
                        {
                            Text = preset.Description,
                            FontSize = 10, Foreground = Res<Brush>("TextDim"),
                            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0)
                        });
                    }
                    Grid.SetColumn(info, 1);
                    grid.Children.Add(info);

                    row.Child = grid;
                    presetPanel.Children.Add(row);
                }
            }
            root.Children.Add(presetPanel);

            // Apply button
            var applyBtn = new Button
            {
                Content = "APPLY  \u25B6",
                Style = (Style)FindResource("ApplyButton"),
                Height = 44,
                Margin = new Thickness(20, 0, 20, 20)
            };
            applyBtn.Click += OnApplyPreset;
            root.Children.Add(applyBtn);

            Content = root;
        }

        private async void OnApplyPreset(object sender, RoutedEventArgs e)
        {
            if (_tweak.Presets == null || _selectedIndex >= _tweak.Presets.Length) return;
            var fn = _tweak.Presets[_selectedIndex].Apply;
            if (fn == null) return;

            try
            {
                bool ok = await Task.Run(() => fn());
                _tweak.StatusText = ok ? "APPLIED \u2713" : "FAILED \u2717";
                _tweak.StatusSuccess = ok;
            }
            catch
            {
                _tweak.StatusText = "FAILED \u2717";
                _tweak.StatusSuccess = false;
            }
            Close();
        }

        private static T Res<T>(string key) => (T)Application.Current.Resources[key];
    }
}
