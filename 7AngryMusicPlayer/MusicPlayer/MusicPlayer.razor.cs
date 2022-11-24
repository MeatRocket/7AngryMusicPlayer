using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.IO;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Reflection;
using static System.Net.WebRequestMethods;
using System.Xml.Linq;
using MediaFilesAPI;
using _7AngryMusicPlayer.Component;



namespace AngryMonkey.Cloud.Components
{
    public partial class MusicPlayer
    {
        private IJSObjectReference? module;

        private IJSObjectReference? musicModule;
        private AudioPlayer? AudioPlayer;

        private ElementReference ComponentElement { get; set; }

        //protected override async Task OnAfterRenderAsync(bool firstRender)
        //{
        //	if (!firstRender)
        //		return;

        //	string importPath = $"./_content/{Assembly.GetExecutingAssembly().GetName().Name}/musicplayer.min.js?v={Guid.NewGuid()}";

        //	module = await JS.InvokeAsync<IJSObjectReference>("import", importPath);
        //}

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (module is null)
                return;
            await module.DisposeAsync();
        }

        IQueryable<MediaFiles.JSFile>? files { get; set; }
        MediaFiles.JSFile[] JsFile { get; set; }
        MediaFiles.JSFile? CurrentSong { get; set; } = null;

        readonly bool UseFileWatcher = !OperatingSystem.IsBrowser();
        string[] Music { get; set; } = Array.Empty<string>();

        string CurrentSongFilePath { get; set; } = "https://0.0.0.0/music.mp3";
        //string CurrentSongFilePath { get; set; } = $"file:///{CurrentMusicPath}";

        private const string MusicFilesExtensions = "*.mp3";

        protected List<string> MusicDirectories { get; set; } = new List<string>() { Environment.GetFolderPath(Environment.SpecialFolder.MyMusic).ToLower() };
        protected string? FilledMusicDirectory { get; set; }

        //private static string CurrentMusicDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Coverbox Music");
        private static string CurrentMusicDirectory => Path.Combine(Path.GetTempPath(), "Coverbox Music");

        private static string CurrentMusicPath => Path.Combine(CurrentMusicDirectory, "music.mp3");

        protected void AddMusicDirectory()
        {
            try
            {

                if (!OperatingSystem.IsBrowser())
                {
                    if (string.IsNullOrEmpty(FilledMusicDirectory) || !Directory.Exists(FilledMusicDirectory))
                        return;

                    string directory = FilledMusicDirectory.ToLower().Trim();
                    FilledMusicDirectory = null;

                    if (MusicDirectories.Contains(directory))
                        return;

                    MusicDirectories.Add(directory);
                    RefreshMusicFileFromDirectories();

                    FileSystemWatcher fsw = new(directory);
                    fsw.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
                    fsw.Changed += OnChanged;
                    fsw.Created += OnChanged;
                    fsw.Deleted += OnChanged;
                    fsw.Renamed += OnChanged;
                    fsw.Filter = MusicFilesExtensions;
                    fsw.IncludeSubdirectories = true;
                    fsw.EnableRaisingEvents = true;
                }
                else
                {
                    OpenDirectory();
                }
            }
            catch { }
        }


        public async Task OpenDirectory()
        {

            try
            {
                await using var dir = await MediaFiles.ShowDirectoryPicker();
                FilledMusicDirectory = dir.Name;
                files = (await MediaFiles.GetFilesAsync(dir)).AsQueryable();
                JsFile = await MediaFiles.GetFilesAsync(dir);
                AddMusicDirectory();
            }
            catch
            {
                // User cancelled
            }
        }
        public string TestFilePath { get; set; }

        protected async Task OnMusicClicked(string path)
        {
            byte[] originalContent = await System.IO.File.ReadAllBytesAsync(path);

            string base64 = Convert.ToBase64String(originalContent);

            CurrentSongFilePath = $"data:audio/mp3;base64,{base64}";

            await module.InvokeVoidAsync("play", ComponentElement);
        }

        protected async Task MusicHasChanged(AudioPlayer musicFile)
        {
            if (AudioPlayer != null && AudioPlayer.IsPlaying)
                await AudioPlayer.TogglePlayingAudioAsync();

            AudioPlayer = musicFile;
        }

        //readonly string DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop, Environment.SpecialFolderOption.None));
        protected override async Task OnInitializedAsync()
        {
            if (UseFileWatcher)
            {
                if (!Directory.Exists(CurrentMusicDirectory))
                    Directory.CreateDirectory(CurrentMusicDirectory);

                if (!System.IO.File.Exists(CurrentMusicPath))
                    System.IO.File.Create(CurrentMusicPath);

                RefreshMusicFileFromDirectories();
            }
            base.OnInitialized();
        }

        //public async Task UpdateMp3Async()
        //{
        //	FileInfo fi = new(@"C:\Users\Hp\Desktop\New-folder\music1.mp3");

        //	FileStream WAVFile = new(@"C:\Users\Hp\Desktop\New-folder\music1.mp3", FileMode.Open);
        //	BinaryReader WAVreader = new(WAVFile);
        //	var chunkID = WAVreader.ReadInt32();
        //	var chunkSize = WAVreader.ReadInt32();
        //	var RiffFormat = WAVreader.ReadInt32();

        //	try
        //	{
        //		string musicFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\CoverboxMusic\\music.mp3";
        //		//FileInfo musicFile = new(musicFilePath);
        //		//musicFile.Create();

        //		//StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;

        //		//StorageFile sampleFile = await tempFolder.CreateFileAsync("music.mp3");
        //		//await FileIO.WriteBufferAsync(sampleFile, Windows.Storage.Streams.IBuffer);

        //		fi.CopyTo(musicFilePath, true);
        //	}
        //	catch (Exception ex)
        //	{
        //		string t = ex.Message;
        //	}

        //	Music = Directory.GetFiles("C:/Users/Hp/Desktop/New-Folder", "*.mp3");
        //	Console.WriteLine("Test");
        //}

        private void RefreshMusicFileFromDirectories()
        {
            List<string> musicFiles = new();

            foreach (string directory in MusicDirectories)
                musicFiles.AddRange(Directory.GetFiles(directory, MusicFilesExtensions, SearchOption.AllDirectories));

            Music = musicFiles.ToArray();
        }

        protected void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (OperatingSystem.IsBrowser())
                return;

            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            RefreshMusicFileFromDirectories();
        }
    }
}