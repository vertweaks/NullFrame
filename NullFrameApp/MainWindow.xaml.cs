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

    // ── Category definition ──────────────────────────────────────────────────

    public record Category(string Name, string Key, string Number);

    // ── Main Window ──────────────────────────────────────────────────────────

    public partial class MainWindow : Window
    {
        private static readonly Category[] Categories =
        {
            new("CPU & SYSTEM",      "cpu",     "01"),
            new("GPU & GAMING",      "gpu",     "02"),
            new("NETWORK",           "network", "03"),
            new("USB & INPUT",       "usb",     "04"),
            new("DEVICES",           "devices", "05"),
            new("STORAGE & SSD",     "storage", "06"),
            new("MEMORY & RAM",      "memory",  "07"),
            new("PRIVACY & DEBLOAT", "privacy", "08"),
        };

        private readonly Dictionary<string, List<Tweak>> _tweakMap;
        private readonly Dictionary<string, Button> _navButtons = new();
        private string _activeKey = "";

        public MainWindow()
        {
            // Register converters before InitializeComponent
            Application.Current.Resources["BoolVis"] = new BoolToVisibilityConverter();
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
            var tweaks = _tweakMap.GetValueOrDefault(key) ?? new List<Tweak>();

            // Update header
            HeaderEyebrow.Text = $"NULLFRAME  //  MODULE {cat.Number}";
            HeaderTitle.Text = cat.Name;
            HeaderCount.Text = $"{tweaks.Count} TWEAKS AVAILABLE";

            // Set tweak list
            TweakList.ItemsSource = tweaks;

            // Load initial states for toggle tweaks
            LoadTweakStates(tweaks);
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
