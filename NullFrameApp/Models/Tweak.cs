using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NullFrame.Models
{
    public enum TweakType
    {
        Toggle,
        Apply,
        Preset
    }

    public class TweakPreset
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Recommended { get; set; }
        public Func<bool>? Apply { get; set; }
        public Func<bool>? Check { get; set; }
    }

    public class Tweak : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _statusText = "";
        private bool _statusSuccess;
        private bool _isBusy;

        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public TweakType Type { get; set; } = TweakType.Toggle;
        public bool IsFree { get; set; }
        public bool HasWarning { get; set; }
        public string WarningText { get; set; } = "";

        public Func<bool>? Enable { get; set; }
        public Func<bool>? Disable { get; set; }
        public Func<bool>? Check { get; set; }
        public Func<bool>? ApplyAction { get; set; }
        public TweakPreset[]? Presets { get; set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool StatusSuccess
        {
            get => _statusSuccess;
            set { _statusSuccess = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string DisplayName => IsFree ? $"{Name}  (FREE)" : Name;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
