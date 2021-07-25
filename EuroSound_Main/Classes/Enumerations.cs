namespace EuroSound_Application
{
    internal static class Enumerations
    {
        internal enum EXMarkerType : byte
        {
            Start = 10,
            End = 9,
            Goto = 7,
            Loop = 6,
            Pause = 5,
            Jump = 0
        }

        internal enum ESoundFileType : byte
        {
            SoundBanks = 0,
            StreamSounds = 1,
            MusicBanks = 2
        }

        internal enum EXObjectType : byte
        {
            EXAudio = 1,
            EXSound = 2,
            EXSample = 3,
            EXMusic = 4,
            EXSoundFolder = 5,
            EXMusicFolder = 6
        }

        internal enum TreeNodeType : byte
        {
            Folder = 1,
            Sound = 2,
            Sample = 3,
            Music = 4,
            StreamSound = 5,
            Audio = 6,
            Target = 7,
            Root = 8
        }

        internal enum ClipboardObject : byte
        {
            cbSFX = 0,
            cbAudio = 1,
        }

        internal enum OutputTarget : byte
        {
            ALL = 0,
            PC = 1,
            PS2 = 2
        }

        internal static byte GetTreeNodeType(string stringType)
        {
            byte nodeFlag = 0;

            switch (stringType.ToLower())
            {
                case "audio":
                    nodeFlag = (byte)TreeNodeType.Audio;
                    break;
                case "folder":
                    nodeFlag = (byte)TreeNodeType.Folder;
                    break;
                case "sound":
                    nodeFlag = (byte)TreeNodeType.Sound;
                    break;
                case "sample":
                    nodeFlag = (byte)TreeNodeType.Sample;
                    break;
                case "target":
                    nodeFlag = (byte)TreeNodeType.Target;
                    break;
                case "root":
                    nodeFlag = (byte)TreeNodeType.Root;
                    break;
            }

            return nodeFlag;
        }
    }
}
