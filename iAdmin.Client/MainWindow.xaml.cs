using System.Text.Json;
using System.IO;
using System.Windows;
using iAdmin.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace iAdmin.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private readonly string _windowStatePath;

    private class SavedWindowState
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public MainWindow()
    {
        InitializeComponent();
        
        // Get ViewModel from DI container
        var app = Application.Current as App;
        _viewModel = app?.Services?.GetService<LoginViewModel>() 
            ?? throw new InvalidOperationException("LoginViewModel not found in DI container");
        
        DataContext = _viewModel;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "iAdmin");
        Directory.CreateDirectory(appDataPath);
        _windowStatePath = Path.Combine(appDataPath, "window-state.json");
        
        // Handle PasswordBox binding manually
        PasswordBox.PasswordChanged += (s, e) =>
        {
            // Store password temporarily for login (should use SecureString in production)
            _viewModel.Password = PasswordBox.Password;
        };

        Loaded += async (_, _) =>
        {
            RestoreWindowPosition();
            await _viewModel.InitializeAsync();
        };

        Closing += (_, _) => SaveWindowPosition();
    }

    private void SaveWindowPosition()
    {
        try
        {
            var state = new SavedWindowState
            {
                Left = Left,
                Top = Top,
                Width = Width,
                Height = Height
            };

            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(_windowStatePath, json);
        }
        catch
        {
            // Best effort only.
        }
    }

    private void RestoreWindowPosition()
    {
        try
        {
            if (!File.Exists(_windowStatePath))
            {
                return;
            }

            var json = File.ReadAllText(_windowStatePath);
            var state = JsonSerializer.Deserialize<SavedWindowState>(json);
            if (state == null)
            {
                return;
            }

            Left = state.Left;
            Top = state.Top;
            Width = state.Width;
            Height = state.Height;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        catch
        {
            // Best effort only.
        }
    }
}