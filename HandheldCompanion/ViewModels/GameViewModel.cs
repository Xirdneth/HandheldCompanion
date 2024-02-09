using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using craftersmine.SteamGridDBNet;
using GameLib;
using GameLib.Core;
using GameLib.Plugin.RiotGames.Model;
using HandheldCompanion.Database;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HandheldCompanion.ViewModels;

public partial class GameViewModel : ViewModelBase
{
    const string CrossImagePath = "/Resources/GameLib/cross-color.png";
    const string CheckImagePath = "/Resources/GameLib/check-color.png";

    private readonly LauncherManager _launcherManager;
    private readonly LiteDbContext db;
    [ObservableProperty]
    private ObservableCollection<Game> _games = default!;

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

    private ILiteCollection<Game>? dbGames;
    private ILiteCollection<IGame>? dbBaseGame;
    private ILiteCollection<SteamGridDbGame>? dbGamesMetaData;


    private static SteamGridDb? steamGridDb { get; set; }

    public GameViewModel(LauncherManager launcherManager, LiteDbContext db)
    {
        _launcherManager = launcherManager;
        this.db = db;
        steamGridDb = new SteamGridDb("3211a8c119e2d7e013e69e80d9bc460a");

        var mapper = BsonMapper.Global;

        mapper.Entity<Game>()
            .Id(x => x.Id)
            .Ignore(x => x.coverArt)
            .Field(x => x.metaData, "metaData")
            .Field(x => x.baseGame, "baseGame");

        //BsonMapper.Global.Entity<Game>()
        //    .Id(oid => oid.Id)
        //    .DbRef(x => x.baseGame, "baseGame")
        //    .DbRef(x => x.metaData, "metaData");

        //BsonMapper.Global.Entity<IGame>()
        //    .Id(oid => oid.Id);
        //BsonMapper.Global.Entity<SteamGridDbGame>()
        //    .Id(oid => oid.Id);

        BsonMapper.Global.ResolveMember = (type, memberInfo, memberMapper) =>
        {
            if (memberMapper.DataType.IsEnum)
            {
                memberMapper.Serialize = (obj, mapper) => new BsonValue((int)obj);
                memberMapper.Deserialize = (value, mapper) => Enum.ToObject(memberMapper.DataType, value);
            }
        };

        //dbGamesMetaData = db.Context.GetCollection<SteamGridDbGame>("metaData");
        //dbBaseGame = db.Context.GetCollection<IGame>("baseGame");
        dbGames = db.Context.GetCollection<Game>("Games");


        if (dbGames.Count() == 0)
        {
            Task.Run(async () => await ImportGames("Steam"));
        }
        else
        {
            Task.Run(async () => await LoadGamesfromDb());
        }
    }

    private async Task ImportGames(string launcherName)
    {
        IsLoading = true;
        IEnumerable<Game> games = Enumerable.Empty<Game>();

        await Task.Run(async () =>
        {
            try
            {
                var InstalledGames = _launcherManager.GetLaunchers().Where((ILauncher launcher) => launcher.Name == launcherName).FirstOrDefault();
                if(InstalledGames != null)
                {
                    games = InstalledGames.Games.Select(x => new Game()
                    {
                        baseGame = x,
                    });

                    if (games.Any())
                    {
                        await DownloadMetaData(games);                    
                    }
                }
            }
            catch { /* ignore */ }
        });
        
        
    }
    private async Task LoadGamesfromDb()
    {
        IsLoading = true;

        await Task.Run(async () =>
        {
            try
            {
                var InstalledGames = dbGames.FindAll().ToList();
                if (InstalledGames.Any())
                {
                    await LoadGameMetaData(InstalledGames);
                }
            }
            catch { /* ignore */ }
        });


    }
    public async Task LoadGameMetaData(IEnumerable<Game> games)
    {
        try
        {
            if (games.Any())
            {
                Games = new();
                var fs = db.Context.GetStorage<string>("myFiles", "myChunks");
                foreach (var game in games)
                {
                    var files1 = fs.FindAll().ToList();
                    //var files = files1[3];
                    //var files = fs.Find($"$/coverart/{game.metaData.Id}.Png");
                    Stream Stream = new MemoryStream();
                    files1.FirstOrDefault().CopyTo(Stream);
                    game.coverArt = BitmapFrame.Create(Stream,
                                                        BitmapCreateOptions.None,
                                                        BitmapCacheOption.OnLoad);
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        Games.Add(game);

                    });
                }
               
                SelectedGame = Games.FirstOrDefault();
            }
            else
            {
                NoGameFound = true;
            }

            IsLoading = false;
        }
        catch (Exception)
        {
            IsLoading = false;
            NoGameFound = true;
        }

    }
    public async Task DownloadMetaData(IEnumerable<Game> games)
    {
        try
        {
            if (games.Any())
            {
                Games = new();
                var fs = db.Context.GetStorage<string>("myFiles", "myChunks");
                foreach (var game in games)
                {
                    SteamGridDbGame[]? gameSearch = await steamGridDb.SearchForGamesAsync(game.baseGame.Name);

                    if (gameSearch.Any())
                    {
                        var GameMetaData = gameSearch.Where(w => w.Name == game.baseGame.Name).FirstOrDefault();

                        game.metaData = GameMetaData;
                        SteamGridDbGame? gameById = await steamGridDb.GetGameByIdAsync(GameMetaData.Id);
                        SteamGridDbLogo[]? Logo = await steamGridDb.GetLogosByGameIdAsync(GameMetaData.Id);

                        if (Logo.Any())
                        {
                            var logo = Logo.FirstOrDefault();
                            using Stream LogoStream = await logo.GetImageAsStreamAsync(false);

                            game.coverArt = BitmapFrame.Create(LogoStream,
                                                                  BitmapCreateOptions.None,
                                                                  BitmapCacheOption.OnLoad);
                            fs.Upload($"$/coverart/{game.baseGame.Id}.{logo.Format}", $"{game.baseGame.Name}.{logo.Format}", LogoStream);
                        }
                        else
                        {
                            SteamGridDbIcon[]? Icon = await steamGridDb.GetIconsByGameIdAsync(GameMetaData.Id);
                            if (Icon.Any())
                            {
                                var icon = Icon.FirstOrDefault();
                                using Stream IconStream = await icon.GetImageAsStreamAsync(false);

                                game.coverArt = BitmapFrame.Create(IconStream,
                                                                      BitmapCreateOptions.None,
                                                                      BitmapCacheOption.OnLoad);
    
                                fs.Upload($"$/coverart/{game.baseGame.Id}.{icon.Format}", $"{game.baseGame.Name}.{icon.Format}", IconStream);
                            }
                        }


                    }
                    //var doc = BsonMapper.Global.ToDocument(game.baseGame);
                    dbGames.Insert(game);
                    //dbBaseGame.Insert(game.baseGame);
                    //dbGamesMetaData.Insert(game.metaData);
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        Games.Add(game);

                    });
                }
                SelectedGame = Games.FirstOrDefault();
            }
            else
            {
                NoGameFound = true;
            }

            IsLoading = false;
        }
        catch (Exception)
        {
            IsLoading = false;
            NoGameFound = true;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName.Equals("SelectedGame"))
        {
            if(SelectedGame != null)
            {
                LauncherName = _launcherManager.GetLaunchers().First(l => l.Id == SelectedGame.baseGame.LauncherId).Name;
                IsRunningLogo = SelectedGame.baseGame.IsRunning ? CheckImagePath : CrossImagePath;
            }
            else
            {
                LauncherName = string.Empty;
                IsRunningLogo = CrossImagePath;
            }
        }
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
        Task.Run(async () => await LoadGamesfromDb());
    }

    
}

public class Game
{
    [BsonId]
    public ObjectId Id { get; set; }
    public ImageSource? coverArt { get; set; }
    //[BsonRef("metaData")]
    public SteamGridDbGame? metaData { get; set; }
    //[BsonRef("baseGame")]
    public required IGame baseGame { get; set; }
}