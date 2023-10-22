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
        public virtual void MoveCurrent(int offset) { }
        //获取显示list内容的控件
        public abstract Control GetMainControl();
        //获取当前应当播放的文件
        //Current:当前正在播放的文件，Selected:选中的文件
        //单机选中但是不影响播放和上/下一曲，双击选中并播放，Fav/Delete等操作针对选中的曲目        
        public abstract FileInfo? GetCurrentFile();
        public virtual string GetCurrentFileDesc() { return ""; }
        public virtual void OpenLocalSelected() { }
        public virtual void OpenWebSelected() { }
        public virtual void MountDoubleClickEvent(TreeNodeMouseClickEventHandler handler) { }
        public virtual void UnmountDoubleClickEvent(TreeNodeMouseClickEventHandler handler) { }
        //private virtual void DeleteSelectedPart() { }
        //private virtual void DeleteSelected() { }
        //private virtual void FavSelected() { }
        public virtual void DeleteCurrentPart() { }
        public virtual void DeleteCurrent() { }
        public virtual void FavCurrent() { }
        public virtual void SelectCurrent() { }
    }
}
