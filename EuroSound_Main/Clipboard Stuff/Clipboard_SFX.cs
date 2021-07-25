using EuroSound_Application.SoundBanksEditor;
using System.Windows.Forms;

namespace EuroSound_Application.Clipboard_Stuff
{
    public static class Clipboard_SFX
    {
        public static bool IsStream { get; set; } = false;
        public static TreeNode TreeNodeObject { get; set; }
        public static EXSound SoundObject { get; set; }
    }
}
