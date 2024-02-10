using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using craftersmine.SteamGridDBNet;
using GameLib;
using GameLib.Core;
using GameLib.Plugin.RiotGames.Model;
using HandheldCompanion.Controls;
using HandheldCompanion.DataAccess;
using HandheldCompanion.Database;
using HandheldCompanion.Managers;
using iNKORE.UI.WPF.Modern.Controls;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;

namespace HandheldCompanion.ViewModels;

public partial class GameViewModel : ViewModelBase
{
    const string CrossImagePath = "/Resources/GameLib/cross-color.png";
    const string CheckImagePath = "/Resources/GameLib/check-color.png";

    [ObservableProperty]
    private ObservableCollection<Game> games = default!;

    [ObservableProperty]
    private Game? _selectedGame;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _noGameFound = false;

    [ObservableProperty]
    private string _isRunningLogo = CrossImagePath;

    [ObservableProperty]
    private string? _launcherName;

    private LibraryDbManager? libraryDb;

    public GameViewModel(LauncherManager launcherManager, LiteDbContext db)
    {
        libraryDb = new LibraryDbManager(db.Context, launcherManager);
        LoadLocalGames();
    }
    private async void LoadLocalGames()
    {
        IsLoading = true;
        var AllGames = Task.Run(async () => await libraryDb.GetAllGames("Steam")).Result;

        if (AllGames.Any())
        {
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                Games = new(AllGames);
            });
        }
        else
        {
            await Task.Run(async () => await libraryDb.ImportGamesToFromLauncher("Steam"));
            LoadLocalGames();
        }
        SelectedGame = Games.FirstOrDefault();
        IsLoading = false;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
    }

    [RelayCommand]
    public void RefreshGameIsRunning()
    {
        OnPropertyChanged(nameof(SelectedGame));
    }

    [RelayCommand]
    public static void RunGame(Game? game)
    {
        if (game is null)
        {
            return;
        }

        if (game.baseGame.IsRunning)
        {
            //FocusUtil.FocusProcess(Path.GetFileNameWithoutExtension(game.Executable));
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = game.baseGame.Executables.FirstOrDefault(),
                WorkingDirectory = game.baseGame.WorkingDir
            });

        }
        catch { /* ignore */ }
    }
    [RelayCommand]
    public static void RunLaunchString(Game? game)
    {
        if (game is null)
        {
            return;
        }

        if (game.baseGame.IsRunning)
        {
            //FocusUtil.FocusProcess(Path.GetFileNameWithoutExtension(game.Executable));
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = game.baseGame.LaunchString
            });
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    public static void OpenPath(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                Process.Start("explorer.exe", path);
            }
        }
    }

    [RelayCommand]
    public static void CopyToClipboard(object? obj)
    {
        var copyText = string.Empty;

        switch (obj)
        {
            case string text:
                copyText = text;
                break;
            case IEnumerable<string> list:
                copyText = string.Join("\n", list);
                break;
        }

        if (string.IsNullOrEmpty(copyText))
        {
            return;
        }
        Clipboard.SetText(copyText);
    }

    [RelayCommand]
    public void RefreshGames()
    {
        NoGameFound = false;
        LoadLocalGames();
    }

    [RelayCommand]
    public async Task ShowDialogAsync()
    {
        ContentDialog dialog = new ContentDialog();
        dialog.Title = "Import Titles?";
        dialog.PrimaryButtonText = "Import";
        //dialog.SecondaryButtonText = "Don't Save";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;
        var t = new ContentDialogContent();
        t.DataContext = this;
        dialog.Content = t;

        await Application.Current.Dispatcher.BeginInvoke(async () =>
        {
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                LogManager.LogInformation($"User Imported from {LauncherName}");
            }
            else
            {
                LogManager.LogInformation("User cancelled the dialog");
            }
        });
    }
}

public class Game
{
    [BsonId]
    public ObjectId Id { get; set; }
    public ImageSource? heroArt { get; set; }
    public ImageSource? gridArt { get; set; }
    public ImageSource? logoArt { get; set; }
    public ImageSource? iconArt { get; set; }
    [BsonRef("metaData")]
    public SteamGridDbGame? metaData { get; set; }
    [BsonRef("baseGame")]
    public required IGame baseGame { get; set; }
}