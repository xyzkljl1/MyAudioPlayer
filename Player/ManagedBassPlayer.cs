using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ManagedBass;
using ManagedBass.Aac;
using ManagedBass.Fx;
//注意除了在nuget中安装ManagedBass/ManagedBass.Fx/ManagedBass.Aac以外，还需要把bass.dll/bass_fx.dll/bass_aac.dll拷贝到运行目录
namespace MyAudioPlayer.Player
{
    internal class ManagedBassPlayer : BasePlayer
    {
        const string OpusPluginFileName = "bassopus.dll";
        const string WebmPluginFileName = "basswebm.dll";
        static int InvalidChannel = 0;
        readonly int opusPlugin;
        readonly int webmPlugin;
        readonly Errors opusPluginError;
        readonly Errors webmPluginError;

        public ManagedBassPlayer()
        {
            //Bass.Init(Flags: DeviceInitFlags.Device3D);
            //Bass.Set3DFactors(1, 1, 1);
            Bass.Init();
            opusPlugin = LoadPlugin(OpusPluginFileName, out opusPluginError);
            webmPlugin = LoadPlugin(WebmPluginFileName, out webmPluginError);
            BassAac.PlayAudioFromMp4 = true;
            BassAac.AacSupportMp4 = true;
        }

        static int LoadPlugin(string filename, out Errors error)
        {
            string assemblyDirectory = Path.GetDirectoryName(typeof(ManagedBassPlayer).Assembly.Location)
                ?? AppContext.BaseDirectory;
            int handle = Bass.PluginLoad(Path.Combine(assemblyDirectory, filename));
            error = handle == 0 ? Bass.LastError : Errors.OK;
            return handle;
        }
        //理论上所有操作都会在channel非法时抛出异常,暂不处理        
        int channel = 0;
        byte[] buffer = Array.Empty<byte>();
        GCHandle bufferHandle;
        float volume = 1.0f;
        public override void Play()
        {
            if (channel != InvalidChannel)
                Bass.ChannelPlay(channel);
        }
        public override void Pause()
        {
            if (channel != InvalidChannel)
                Bass.ChannelPause(channel);
        }
        public override bool IsPlaying()
        {
            return channel != InvalidChannel && Bass.ChannelIsActive(channel) == PlaybackState.Playing;
        }
        public override int GetCurrentPositionSec()
        {
            if (channel == InvalidChannel) return 0;
            return (int)Math.Floor(Bass.ChannelBytes2Seconds(channel, Bass.ChannelGetPosition(channel, PositionFlags.Bytes)));
        }
        public override int GetTotalLengthSec()
        {
            if (channel == InvalidChannel) return 0;
            return (int)Math.Floor(Bass.ChannelBytes2Seconds(channel, Bass.ChannelGetLength(channel, PositionFlags.Bytes)));
        }
        public override bool IsLoaded()
        {
            return channel != InvalidChannel;
        }
        public override void Stop()
        {
            if (channel != InvalidChannel)
            {
                Bass.ChannelStop(channel);
                Bass.StreamFree(channel);
                channel = InvalidChannel;
            }
            CurrentFile = null;
            ReleaseBuffer();
        }
        public override void Shutdown()
        {
            Stop();
            if (webmPlugin != 0)
                Bass.PluginFree(webmPlugin);
            if (opusPlugin != 0)
                Bass.PluginFree(opusPlugin);
            Bass.Free();
        }

        void ReleaseBuffer()
        {
            if (bufferHandle.IsAllocated)
                bufferHandle.Free();
            buffer = Array.Empty<byte>();
        }
        public override void SetCurrentPositionSec(int seconds)
        {
            if (channel == InvalidChannel) return;
            Bass.ChannelSetPosition(channel, Bass.ChannelSeconds2Bytes(channel, seconds), PositionFlags.Bytes);
        }
        public void OnStop(int handle, int channel, int data, IntPtr user)
        {
            CallPlayStoppedHandler();
        }
        public override void Reload(FileInfo file)
        {
            Stop();
            CurrentFile= file;
            var filename = CurrentFile.FullName;
            //如果MusicLoad/SampleLoad->SampleGetChannel,ChannelSetSync会返回Handle错误，why?
            //如果CreateStream->TempoCreate则不会，区别是什么？

            //channel = Bass.MusicLoad(filename, Flags: BassFlags.MusicRamp | BassFlags.Bass3D, Frequency: 1);
            //if (channel == InvalidChannel)
            //    channel = Bass.SampleLoad(filename, 0, 0, 1, BassFlags.Bass3D | BassFlags.Mono);
            //Bass.SampleGetChannel(channel);
            if (!File.Exists(filename))
                throw new Exception($"File not exist:{filename}");
            buffer = File.ReadAllBytes(filename);//读入内存以防止锁文件
            bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            int sourceChannel;
            try
            {
                sourceChannel = CreateDecodeStream(filename, bufferHandle.AddrOfPinnedObject(), buffer.Length);
            }
            catch
            {
                CurrentFile = null;
                ReleaseBuffer();
                throw;
            }
            if (sourceChannel == InvalidChannel)
            {
                var error = Bass.LastError;
                CurrentFile = null;
                ReleaseBuffer();
                throw new Exception(GetDecodeError(filename, error));
            }

            channel = BassFx.TempoCreate(sourceChannel, BassFlags.FxFreeSource);

            if (channel == InvalidChannel)
            {
                var error = Bass.LastError;
                Bass.StreamFree(sourceChannel);
                CurrentFile= null;
                ReleaseBuffer();
                throw new Exception($"ManagedBass Can't Create Tempo Stream:{filename}/Err:{error}");
            }
            var result = Bass.ChannelSetSync(channel, SyncFlags.End, 0, OnStop);
            if (result==0)
            {
                var error = Bass.LastError;
                Stop();
                throw new Exception($"ManagedBass Can't Set Stop Event:{filename}/Err:{error}");
            }
            ApplyVolumeToChannel();
        }

        string GetDecodeError(string filename, Errors error)
        {
            if (Path.GetExtension(filename).Equals(".webm", StringComparison.OrdinalIgnoreCase))
            {
                if (webmPlugin == 0)
                    return $"WebM plugin unavailable ({WebmPluginFileName}):{webmPluginError}";
                if (opusPlugin == 0)
                    return $"WebM audio codec plugin unavailable ({OpusPluginFileName}):{opusPluginError}/DecodeErr:{error}";
            }
            return $"ManagedBass Can't Decode:{filename}/Err:{error}";
        }

        private static int CreateDecodeStream(string filename, IntPtr fileData, int fileLength)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            if (ext == ".mp4" || ext == ".m4a")
            {
                int stream = BassAac.CreateMp4Stream(fileData, 0, fileLength, BassFlags.Decode);
                if (stream != InvalidChannel)
                    return stream;
            }
            if (ext == ".aac")
                return BassAac.CreateStream(fileData, 0, fileLength, BassFlags.Decode);
            return Bass.CreateStream(fileData, 0, fileLength, BassFlags.Decode);
        }

        public override void SetVolume(float volume)
        {
            this.volume = Math.Clamp(volume, .0f, 1.0f);
            ApplyVolumeToChannel();
        }
        public override float GetVolume()
        {
            return volume;
        }
        void ApplyVolumeToChannel()
        {
            if (channel == InvalidChannel)
                return;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, volume);
        }

    }
}
