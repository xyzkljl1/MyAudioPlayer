using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAudioPlayer.PlayList
{
    internal abstract class PlayListBase
    {
        public string Title { get; set; } = "";
        //移动到下N/上N首
        public virtual void Move(int offset) { }
        //获取显示list内容的控件
        public abstract Control GetMainControl();
        public virtual void RefreshMainControl() { }
        //获取当前应当播放的文件
        public abstract FileInfo? GetCurrentFile();
        public virtual string GetCurrentFileDesc() { return ""; }
        public virtual void OpenLocal() { }
        public virtual void OpenWeb() { }
        public virtual void MountSelectedChangeEvent(TreeNodeMouseClickEventHandler handler) { }
        public virtual void UnmountSelectedChangeEvent(TreeNodeMouseClickEventHandler handler) { }
        public virtual void DeleteCurrent() { }
        public virtual void FavCurrent() { }
    }
}
