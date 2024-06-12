using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAudioPlayer.PlayList
{
    public class MyFileEditEventArgs : EventArgs
    {
        public bool needContinue { get; set; }=false;
        public long prevPosition { get; set; }=0;
        public FileInfo? prevFile { get; set; } = null;
        public MyFileEditEventArgs(){}
    }
    public delegate void MyFileEditEventHandler(object? sender, MyFileEditEventArgs e);
    internal abstract class PlayListBase
    {
        public string Title { get; set; } = "";
        public bool needDelPartButton = true;
        public bool needWebButton = true;
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
        //编辑文件(Fav/Del/DelPart操作)开始和结束事件
        //由于某些操作会移动文件，如果当前文件被占用(即正在播放)会导致操作失败，需要在移动文件前通知主窗口
        protected event MyFileEditEventHandler OnFileEditBegin;
        protected event MyFileEditEventHandler OnFileEditEnd;
        protected void MountFileEditEvent(MyFileEditEventHandler begin, MyFileEditEventHandler end)
        {
            OnFileEditBegin += begin;
            OnFileEditEnd += end;
        }
        //子类不能直接调用OnFileEditBegin，需要在基类用一个函数包一下
        public void RasieFileEditBeginEvent(MyFileEditEventArgs args)
        {
            OnFileEditBegin(null, args);
        }
        public void RasieFileEditEndEvent(MyFileEditEventArgs args)
        {
            OnFileEditEnd(null, args);
        }
    }
}
