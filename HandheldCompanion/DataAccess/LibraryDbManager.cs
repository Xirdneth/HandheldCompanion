using ABI.System;
using craftersmine.SteamGridDBNet;
using GameLib;
using GameLib.Core;
using GameLib.Plugin.RiotGames.Model;
using HandheldCompanion.Database;
using HandheldCompanion.Managers;
using HandheldCompanion.ViewModels;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Exception = System.Exception;
using Game = HandheldCompanion.ViewModels.Game;

namespace HandheldCompanion.DataAccess
{
    public class LibraryDbManager
    {
        private readonly LiteDatabase liteDb;
        private readonly LauncherManager launcherManager;
        private readonly ILiteStorage<string> fileStorage;

        private static SteamGridDb? steamGridDb { get; set; }
        private ILiteCollection<Game>? dbGames { get { return this.liteDb.GetCollection<Game>("Games"); } }
        private ILiteCollection<IGame>? dbBaseGame { get { return this.liteDb.GetCollection<IGame>("baseGame"); } }
        private ILiteCollection<SteamGridDbGame>? dbGamesMetaData { get { return this.liteDb.GetCollection<SteamGridDbGame>("metaData"); } }

        public LibraryDbManager(LiteDatabase liteDb, LauncherManager launcherManager)
        {
            this.liteDb = liteDb;
            this.launcherManager = launcherManager;
            fileStorage = liteDb.GetStorage<string>("myFiles", "myChunks");
            steamGridDb = new SteamGridDb("3211a8c119e2d7e013e69e80d9bc460a");
            Config();
        }

        public void Config()
        {
            BsonMapper.Global.Entity<Game>()
            .Id(x => x.Id)
            .Ignore(x => x.heroArt)
            .Ignore(x => x.gridArt)
            .Ignore(x => x.logoArt)
            .Ignore(x => x.iconArt)
            .Field(x => x.metaData, "metaData")
            .Field(x => x.baseGame, "baseGame")
            .DbRef(x => x.metaData, "metaData")
            .DbRef(x => x.baseGame, "baseGame");
            BsonMapper.Global.EnumAsInteger = true;
        }

        public async Task<IEnumerable<Game>> GetAllGames(string launcherName)
        {
            try
            {
                var ImportedGames = dbGames.Include(i => i.baseGame).Include(i => i.metaData).FindAll().ToList();
               
                if (ImportedGames.Any())
                {
                    ImportedGames.ForEach(async x =>{ 
                        x.logoArt = await LoadGameLogo(x.baseGame.Id, "logo");
                        x.gridArt = await LoadGameLogo(x.baseGame.Id, "grid");
                        x.iconArt = await LoadGameLogo(x.baseGame.Id, "icon");
                        x.heroArt = await LoadGameLogo(x.baseGame.Id, "hero");
                    });
                    return ImportedGames;
                }

                return Enumerable.Empty<Game>();
            }
            catch (System.Exception ex)
            { 
                LogManager.LogError(ex.Message);
                return Enumerable.Empty<Game>();
            }
        }

        public async Task ImportGamesToFromLauncher(string launcherName)
        {
            IEnumerable<Game> games = Enumerable.Empty<Game>();

            await Task.Run(async () =>
            {
                try
                {
                    var InstalledGames = launcherManager.GetLaunchers().Where((ILauncher launcher) => launcher.Name == launcherName).FirstOrDefault();
                    if (InstalledGames != null)
                    {
                        games = InstalledGames.Games.Select(x => new Game()
                        {
                            baseGame = x,
                        });

                        foreach (var game in games)
                        {
                            await DownloadImageData(game);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex.Message);
                }
            });
        }

        public async Task DownloadImageData(Game game)
        {
            try
            {
                if (game != null)
                {
                    dbBaseGame.Insert(game.baseGame);
                    var gameSearchByName = await steamGridDb.SearchForGamesAsync(game.baseGame.Name);
                    SteamGridDbGame? gameSearch = gameSearchByName.Where(w => w.Name == game.baseGame.Name).FirstOrDefault();
                    if (gameSearch != null)
                    {
                        var GameMetaData = gameSearch;

                        game.metaData = GameMetaData;
                        dbGamesMetaData.Insert(game.metaData);

                        SteamGridDbHero[]? Heros = await steamGridDb.GetHeroesByGameIdAsync(GameMetaData.Id);
                        SteamGridDbGrid[]? Grids = await steamGridDb.GetGridsByGameIdAsync(GameMetaData.Id);
                        SteamGridDbLogo[]? Logos = await steamGridDb.GetLogosByGameIdAsync(GameMetaData.Id);
                        SteamGridDbIcon[]? Icons = await steamGridDb.GetIconsByGameIdAsync(GameMetaData.Id);

                        Stream ArtStream;
                        if (Heros.Any())
                        {
                            var imageSource = new BitmapImage();
                            
                            var Hero = Heros.FirstOrDefault();
                            ArtStream = await Hero.GetImageAsStreamAsync(true);
                            ArtStream.Seek(0, SeekOrigin.Begin);

                            var result = fileStorage.Upload($"$/heroart/{game.baseGame.Id}.{Hero.Format}", $"{game.baseGame.Name}.{Hero.Format}", ArtStream);
                        }

                        if (Grids.Any())
                        {
                            var imageSource = new BitmapImage();

                            var Grid = Grids.FirstOrDefault();
                            ArtStream = await Grid.GetImageAsStreamAsync(true);
                            ArtStream.Seek(0, SeekOrigin.Begin);

                            var result = fileStorage.Upload($"$/gridart/{game.baseGame.Id}.{Grid.Format}", $"{game.baseGame.Name}.{Grid.Format}", ArtStream);
                        }

                        if (Logos.Any())
                        {
                            var imageSource = new BitmapImage();

                            var Logo = Logos.FirstOrDefault();
                            ArtStream = await Logo.GetImageAsStreamAsync(true);
                            ArtStream.Seek(0, SeekOrigin.Begin);

                            var result = fileStorage.Upload($"$/logoart/{game.baseGame.Id}.{Logo.Format}", $"{game.baseGame.Name}.{Logo.Format}", ArtStream);
                        }

                        if (Icons.Any())
                        {
                            var imageSource = new BitmapImage();

                            var Icon = Icons.FirstOrDefault();
                            ArtStream = await Icon.GetImageAsStreamAsync(true);
                            ArtStream.Seek(0, SeekOrigin.Begin);

                            var result = fileStorage.Upload($"$/iconart/{game.baseGame.Id}.{Icon.Format}", $"{game.baseGame.Name}.{Icon.Format}", ArtStream);
                        }

                        dbGames.Insert(game);
                    }
                }

            }
            catch (Exception ex)
            {
                LogManager.LogError(ex.Message);
            }
        }

        private async Task<ImageSource> LoadGameLogo(string gameId, string artType)
        {
            try
            {
                if (!string.IsNullOrEmpty(gameId))
                {
                    var gameLogo = fileStorage.FindAll().Where(w => w.Id.Contains($"$/{artType}art/{gameId}")).ToList().FirstOrDefault();
                    if (gameLogo != null)
                    {
                        MemoryStream ms = new MemoryStream();
                        gameLogo.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        var imageSource = new BitmapImage();
                        imageSource.BeginInit();
                        imageSource.StreamSource = ms;
                        imageSource.EndInit();
                        imageSource.Freeze();

                        return imageSource;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex.Message);
                return null;
            }

        }
    }
}
