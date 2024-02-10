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
using Microsoft.Extensions.DependencyModel;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Security.Isolation;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;

namespace HandheldCompanion.ViewModels;

public partial class GameViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Game> games = default!;

    [ObservableProperty]
    private Game? _selectedGame;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _noGameFound = false;

    [ObservableProperty]
    private string _isRunningLogo = "";

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
        var AllGames = Task.Run(async () => await libraryDb.GetAllGames()).Result;

        if (AllGames.Any())
        {
            
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                Games = new(AllGames);
            });
            SelectedGame = Games.FirstOrDefault();
        }
        else
        {
            NoGameFound = true;
        }
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
    public async Task DebugDatabaseClearAsync()
    {
        IsLoading = true;
        await libraryDb.ClearDatabase();
        LoadLocalGames();
    }

    [RelayCommand]
    public async Task DebugFileStorageClearAsync()
    {
        IsLoading = true;
        await libraryDb.ClearFilefileStorage();
        LoadLocalGames();
    }

    [RelayCommand]
    public async Task DebugReDownloadMetadataAsync()
    {
        //await Application.Current.Dispatcher.BeginInvoke(async () =>
        //{

        //});

        IsLoading = true;
        NoGameFound = false;
        var result = await Task.Run(() => libraryDb.ReDownloadMetadata());
        if (result)
        {
            LoadLocalGames();
        } 
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
                IsLoading = true;
                NoGameFound = false;
                await Task.Run(() => libraryDb.ImportGamesToFromLauncher(LauncherName));
                LoadLocalGames();
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
    public string baseGameId { get; set; }
    public ImageSource? heroArt { get; set; }
    public ImageSource? gridArt { get; set; }
    public ImageSource? logoArt { get; set; }
    public ImageSource? iconArt { get; set; }
    [BsonRef("metaData")]
    public SteamGridDbGame? metaData { get; set; }
    [BsonRef("baseGame")]
    public required IGame baseGame { get; set; }
}