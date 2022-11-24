using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using _7AngryMusicPlayer;
using _7AngryMusicPlayer.Shared;
using MediaFilesAPI;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Reflection.Emit;

namespace _7AngryMusicPlayer.Component
{
    public partial class AudioPlayer
    {
        [Parameter, EditorRequired]
        public MediaFiles.JSFile File { get; set; } = default!;
        [Parameter]
        public EventCallback<AudioPlayer> OnMusicChanging { get; set; }

        //public bool IsPlaying { get; set; }

        IJSObjectReference? playingSource;
        public bool IsPlaying => playingSource is not null;
        public string? message => IsPlaying ? "Stop" : "Play";

        CancellationTokenSource disposalCts = new();
        public void Dispose() => disposalCts.Cancel();
        public async Task TogglePlayingAudioAsync()
        {
            if (playingSource is null)
                playingSource = await MediaFiles.PlayAudioFileAsync(File);
            else
            {
                await playingSource.InvokeVoidAsync("stop");
                await playingSource.DisposeAsync();
                playingSource = null;
            }

            await OnMusicChanging.InvokeAsync(this);
        }
    }
}