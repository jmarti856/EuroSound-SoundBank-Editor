using System.Collections.Generic;

namespace EuroSound_Application.Clipboard_Stuff
{
    public static class Clipboard_ContentLists
    {
        public static List<Clipboard_SFX> SFXObjectsList { get; set; } = new List<Clipboard_SFX>();
        public static List<Clipboard_Audio> AudioObjectsList { get; set; } = new List<Clipboard_Audio>();
        public static List<Clipboard_Target> TargetsObjectsList { get; set; } = new List<Clipboard_Target>();
    }
}
