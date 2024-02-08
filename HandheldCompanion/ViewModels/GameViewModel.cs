using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using craftersmine.SteamGridDBNet;
using GameLib;
using GameLib.Core;
using GameLib.Plugin.RiotGames.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using static System.Net.Mime.MediaTypeNames;

namespace HandheldCompanion.ViewModels;

public partial class GameViewModel : ViewModelBase
{
    const string CrossImagePath = "/Resources/GameLib/cross-color.png";
    const string CheckImagePath = "/Resources/GameLib/check-color.png";

    private readonly LauncherManager _launcherManager;

    [ObservableProperty]
    private ObservableCollection<GameExtended> _games = default!;

    [ObservableProperty]
    private GameExtended? _selectedGame;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _noGameFound = false;

    [ObservableProperty]
    private string _isRunningLogo = CrossImagePath;

    [ObservableProperty]
    private string? _launcherName;

    [ObservableProperty]
    private string? aPIKey = "3211a8c119e2d7e013e69e80d9bc460a";

    private static SteamGridDb _steamGridDb { get; set; }

    public GameViewModel(LauncherManager launcherManager)
    {
        _launcherManager = launcherManager;
        _steamGridDb = new SteamGridDb(aPIKey);
        LoadData();
    }

    private async void LoadData()
    {
        IsLoading = true;
        IEnumerable<IGame> games = Enumerable.Empty<IGame>();

        await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                games = _launcherManager.GetAllGames().OfType<IGame>().OrderBy(g => g.Name);
            }
            catch { /* ignore */ }
        });
        LoadGameData(games).GetAwaiter();
        //Games = new(games);
       
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(SelectedGame):
                switch (_selectedGame)
                {
                    case null:
                        LauncherName = string.Empty;
                        IsRunningLogo = CrossImagePath;
                        break;
                    default:
                        LauncherName = _launcherManager.GetLaunchers().First(l => l.Id == _selectedGame.LauncherId).Name;
                        IsRunningLogo = _selectedGame.IsRunning ? CheckImagePath : CrossImagePath;
                        break;
                }
                break;
        }
    }

    [RelayCommand]
    public void RefreshGameIsRunning()
    {
        OnPropertyChanged(nameof(SelectedGame));
    }

    [RelayCommand]
    public static void RunGame(GameExtended? game)
    {
        if (game is null)
        {
            return;
        }

        if (game.IsRunning)
        {
            //FocusUtil.FocusProcess(Path.GetFileNameWithoutExtension(game.Executable));
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = game.Executables.FirstOrDefault(),
                WorkingDirectory = game.WorkingDir
            });
            
        }
        catch { /* ignore */ }
    }
    [RelayCommand]
    public static void RunLaunchString(IGame? game)
    {
        if (game is null)
        {
            return;
        }

        if (game.IsRunning)
        {
            //FocusUtil.FocusProcess(Path.GetFileNameWithoutExtension(game.Executable));
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = game.LaunchString
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
    public async System.Threading.Tasks.Task LoadGameData(IEnumerable<IGame> games)
    {
        try
        {
            List<GameExtended> tempList = new List<GameExtended>();
            
            foreach (var game in games)
            {
                var Test = new GameExtended()
                {
                    Name = game.Name,
                    Executable = game.Executable,
                    ExecutableIcon = game.ExecutableIcon,
                    Executables = game.Executables,
                    Id = game.Id,
                    InstallDate = game.InstallDate,
                    InstallDir = game.InstallDir,
                    IsRunning = game.IsRunning,
                    LauncherId = game.LauncherId,
                    LaunchString = game.LaunchString,
                    WorkingDir = game.WorkingDir
                };
                SteamGridDbGame[]? gameSearch = await _steamGridDb.SearchForGamesAsync(game.Name);
                var GameId = gameSearch[0].Id;
                Test.MetaData = gameSearch[0];
                //SteamGridDbGame? gameById = await _steamGridDb.GetGameByIdAsync(game.Id);
                SteamGridDbGrid[]? grids = await _steamGridDb.GetGridsByGameIdAsync(GameId);
                SteamGridDbLogo[]? Logo = await _steamGridDb.GetLogosByGameIdAsync(GameId);
                SteamGridDbGame? game2 = await _steamGridDb.GetGameByIdAsync(GameId);
                if (Logo.Any())
                {
                    Stream LogoStream = await Logo[0].GetImageAsStreamAsync(false);
                    //Stream IconStream = await Icon[0].GetImageAsStreamAsync(false);

                    Test.Logo = BitmapFrame.Create(LogoStream,
                                                          BitmapCreateOptions.None,
                                                          BitmapCacheOption.OnLoad);
                }
                else
                {
                    SteamGridDbIcon[]? Icon = await _steamGridDb.GetIconsByGameIdAsync(GameId);
                    if(Icon.Any())
                    {
                        Stream IconStream = await Icon[0].GetImageAsStreamAsync(false);
                        //Stream IconStream = await Icon[0].GetImageAsStreamAsync(false);

                        Test.Logo = BitmapFrame.Create(IconStream,
                                                              BitmapCreateOptions.None,
                                                              BitmapCacheOption.OnLoad);
                    }
                }
                tempList.Add(Test);
                
            }
            Games = new(tempList);
            IsLoading = false;
            if (!Games.Any())
            {
                NoGameFound = true;
            }
            else
            {
                SelectedGame = Games.FirstOrDefault();
            }
        }
        catch (Exception)
        {
            IsLoading = false;
            NoGameFound = true;
        }

    }
}

public class GameExtended : IGame
{
    public ImageSource? Logo { get; set; }
    public SteamGridDbGame? MetaData { get; set; }
    public Icon? ExecutableIcon { get; set; }

    public string Id { get; set; }

    public Guid LauncherId { get; set; }

    public string Name { get; set; }

    public string InstallDir { get; set; }

    public string Executable { get; set; }

    public IEnumerable<string> Executables { get; set; }

    public string WorkingDir { get; set; }

    public string LaunchString { get; set; }

    public DateTime InstallDate { get; set; }

    public bool IsRunning { get; set; }
}