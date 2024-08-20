using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAudioPlayer.Player
{
    internal abstract class BasePlayer
    {
        public FileInfo? CurrentFile=null;
        public abstract void Reload(FileInfo filename);
        public abstract void Play();
        public abstract void Pause();
        public abstract bool IsLoaded();
        public abstract bool IsPlaying();
        //range:0~1
        public abstract void SetVolume(float volume);
        public abstract float GetVolume();
        public abstract void Stop();
        public abstract void SetCurrentPositionSec(int seconds);
        //seconds
        public abstract int GetCurrentPositionSec();
        public abstract int GetTotalLengthSec();
        public event EventHandler PlayStopped=delegate { };
        public virtual void CallPlayStoppedHandler() { PlayStopped.Invoke(this,new EventArgs()); }
    }
}
