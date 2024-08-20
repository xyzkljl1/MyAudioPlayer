using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ManagedBass;
using ManagedBass.Fx;
//注意除了在nuget中安装ManagedBass/ManagedBass.Fx以外，还需要把bass.dll/bass_fx.dll拷贝到运行目录
namespace MyAudioPlayer.Player
{
    internal class ManagedBassPlayer : BasePlayer
    {
        static int InvalidChannel = 0;
        public ManagedBassPlayer()
        {
            //Bass.Init(Flags: DeviceInitFlags.Device3D);
            //Bass.Set3DFactors(1, 1, 1);
            Bass.Init();
        }
        //理论上所有操作都会在channel非法时抛出异常,暂不处理        
        int channel = 0;
        byte[] buffer;
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
            CurrentFile = null;
            Bass.ChannelStop(channel);
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
        unsafe public override void Reload(FileInfo file) 
        {
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
            fixed (byte* p = buffer)
                channel = Bass.CreateStream((IntPtr)p, 0, buffer.Length, BassFlags.Decode);

            channel = BassFx.TempoCreate(channel, BassFlags.FxFreeSource);

            if (channel == InvalidChannel)
            {
                CurrentFile= null;
                // not a WAV/MP3 or MOD
                throw new Exception($"ManagedBass Can't Load:{filename}/Err:{ManagedBass.Bass.LastError}");
            }
            var result = Bass.ChannelSetSync(channel, SyncFlags.End, 0, OnStop);
            if (result==0)
                throw new Exception($"ManagedBass Can't Set Stop Event:{filename}/Err:{ManagedBass.Bass.LastError}");
        }
        public override void SetVolume(float volume)
        {
            Bass.Volume = (double)volume;
        }
        public override float GetVolume()
        {
            return (float)Bass.Volume;
        }

    }
}
