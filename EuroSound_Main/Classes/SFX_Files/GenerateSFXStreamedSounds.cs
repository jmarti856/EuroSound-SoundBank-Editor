using EuroSound_Application.AudioFunctionsLibrary;
using EuroSound_Application.Editors_and_Tools;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EuroSound_Application.StreamSounds
{
    internal class GenerateSFXStreamedSounds
    {
        private List<uint> StreamObjectsStart = new List<uint>();
        private List<long> MarkerOffsets = new List<long>();
        private List<long> AudioOffsets = new List<long>();
        private long FileLength1, FileLength2, FullFileLength;

        private const uint FileStart1 = 0x800;
        private const uint FileStart2 = 0x1000;

        //*===============================================================================================
        //* HEADER
        //*===============================================================================================
        internal void WriteFileHeader(BinaryStream BWriter, uint FileHashcode, ProgressBar Bar)
        {
            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, 4);

            //--magic[magic value]--
            BWriter.Write(Encoding.ASCII.GetBytes("MUSX"));
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);

            //--hashc[Hashcode for the current soundbank without the section prefix]--
            BWriter.WriteUInt32(FileHashcode);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);

            //--offst[Constant offset to the next section,]--
            BWriter.WriteUInt32(0xC9);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);

            //--fulls[Size of the whole file, in bytes. Unused. ]--
            BWriter.WriteUInt32(0);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
        }

        //*===============================================================================================
        //* Write Sections
        //*===============================================================================================
        internal void WriteFileSections(BinaryStream BWriter, ProgressBar Bar)
        {
            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, 6);

            //--File start 1[an offset that points to the stream look-up file details, always 0x800]--
            BWriter.WriteUInt32(FileStart1);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            //--File length 1[size of the first section, in bytes]--
            BWriter.WriteUInt32(0);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);

            //--File start 2[offset to the second section with the sample data]--
            BWriter.WriteUInt32(FileStart2);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            //--File length 2[size of the second section, in bytes]--
            BWriter.WriteUInt32(0);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);

            //--File start 3[unused and uses the same sample data offset as dummy for some reason]--
            BWriter.WriteUInt32(0);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            //--File length 3[unused and set to zero]--
            BWriter.WriteUInt32(0);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
        }

        //*===============================================================================================
        //* Look up Table
        //*===============================================================================================
        public void WriteLookUptable(BinaryStream BWriter, Dictionary<uint, EXSoundStream> StreamSoundsList, ProgressBar Bar)
        {
            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, StreamSoundsList.Keys.Count);

            BWriter.Seek((int)FileStart1, SeekOrigin.Begin);
            for (int i = 0; i < StreamSoundsList.Count; i++)
            {
                BWriter.WriteUInt32(0);
                ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            }
            FileLength1 = BWriter.BaseStream.Position - FileStart1;
        }

        //*===============================================================================================
        //* WRITE MARKERS
        //*===============================================================================================
        public void WriteStreamFile(BinaryStream BWriter, Dictionary<uint, EXSoundStream> StreamSoundsList, ProgressBar Bar)
        {
            //Update GUI
            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, StreamSoundsList.Keys.Count);
            uint soundStart = 0;
            uint mediaStart = 0;

            BWriter.Seek((int)FileStart2, SeekOrigin.Begin);
            foreach (KeyValuePair<uint, EXSoundStream> SoundToWrtie in StreamSoundsList)
            {
                //Align bytes
                BWriter.Align(FileStart2);

                //Realign if necessary
                if (soundStart > 0)
                {
                    if ((BWriter.BaseStream.Position - mediaStart) < FileStart1)
                    {
                        uint Offset = ((uint)(BWriter.BaseStream.Position + FileStart1 & ~(FileStart1 - 1)));
                        BWriter.Seek(Offset, SeekOrigin.Begin);
                    }
                }

                //Offset to write in look-up table
                soundStart = (uint)(BWriter.BaseStream.Position);
                StreamObjectsStart.Add(soundStart - FileStart2);

                //Marker size
                uint MarkersSize = (uint)((52 * SoundToWrtie.Value.StartMarkers.Count) + (32 * SoundToWrtie.Value.Markers.Count));
                BWriter.WriteUInt32(MarkersSize + 20);
                //Audio Offset
                BWriter.WriteUInt32(0);
                //Audio size
                BWriter.WriteUInt32((uint)SoundToWrtie.Value.IMA_ADPCM_DATA.Length);
                //Start marker count
                BWriter.WriteUInt32((uint)SoundToWrtie.Value.StartMarkers.Count);
                //Marker count
                BWriter.WriteUInt32((uint)SoundToWrtie.Value.Markers.Count);
                //Start marker offset
                BWriter.WriteUInt32(20);
                //Marker offset
                BWriter.WriteUInt32(0);
                //Base volume
                BWriter.WriteUInt32(SoundToWrtie.Value.BaseVolume);

                //Start Markers Data
                long MarkerOffsetStart = BWriter.BaseStream.Position;
                for (int i = 0; i < SoundToWrtie.Value.StartMarkers.Count; i++)
                {
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].Name);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].Position);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].MusicMakerType);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].Flags);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].Extra);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].LoopStart);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].MarkerCount);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].LoopMarkerCount);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].MarkerPos);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].IsInstant);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].InstantBuffer);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].StateA);
                    BWriter.WriteUInt32(SoundToWrtie.Value.StartMarkers[i].StateB);
                }
                MarkerOffsets.Add((BWriter.BaseStream.Position + 20) - MarkerOffsetStart);

                //Markers
                for (int j = 0; j < SoundToWrtie.Value.Markers.Count; j++)
                {
                    BWriter.WriteInt32(SoundToWrtie.Value.Markers[j].Name);
                    BWriter.WriteUInt32(SoundToWrtie.Value.Markers[j].Position);
                    BWriter.WriteUInt32(SoundToWrtie.Value.Markers[j].MusicMakerType);
                    BWriter.WriteUInt32(SoundToWrtie.Value.Markers[j].Flags);
                    BWriter.WriteUInt32(SoundToWrtie.Value.Markers[j].Extra);
                    BWriter.WriteUInt32(SoundToWrtie.Value.Markers[j].LoopStart);
                    BWriter.WriteUInt32(SoundToWrtie.Value.Markers[j].MarkerCount);
                    BWriter.WriteUInt32(SoundToWrtie.Value.Markers[j].LoopMarkerCount);
                }
                uint soundEnd = (uint)BWriter.BaseStream.Position;

                //Align bytes
                BWriter.Align(FileStart2);

                //Realign if necessary
                if (soundEnd > 0)
                {
                    if ((BWriter.BaseStream.Position - soundEnd) < FileStart1)
                    {
                        uint Offset = ((uint)(BWriter.BaseStream.Position + FileStart1 & ~(FileStart1 - 1)));
                        BWriter.Seek(Offset, SeekOrigin.Begin);
                    }
                }
                //Write IMA data
                AudioOffsets.Add(BWriter.BaseStream.Position - FileStart2);
                BWriter.Write(SoundToWrtie.Value.IMA_ADPCM_DATA);
                mediaStart = (uint)BWriter.BaseStream.Position;

                //Update GUI
                ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            }
            FileLength2 = BWriter.BaseStream.Position - FileStart2;
            FullFileLength = BWriter.BaseStream.Position;
        }

        //*===============================================================================================
        //* FINAL OFFSETS
        //*===============================================================================================
        public void WriteFinalOffsets(BinaryStream BWriter, ProgressBar Bar)
        {
            //Update GUI
            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, StreamObjectsStart.Count * 2);

            //File Full Size
            BWriter.BaseStream.Seek(0xC, SeekOrigin.Begin);
            BWriter.WriteUInt32((uint)FullFileLength);

            //File length 1
            BWriter.BaseStream.Seek(0x14, SeekOrigin.Begin);
            BWriter.WriteUInt32((uint)FileLength1);

            //File length 2
            BWriter.BaseStream.Seek(0x1C, SeekOrigin.Begin);
            BWriter.WriteUInt32((uint)FileLength2);

            //Write Look-up Table
            BWriter.Seek(FileStart1, SeekOrigin.Begin);
            foreach (long Offset in StreamObjectsStart)
            {
                BWriter.WriteUInt32((uint)Offset);
                ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            }

            //Write Offsets
            BWriter.Seek(FileStart2, SeekOrigin.Begin);
            for (int i = 0; i < StreamObjectsStart.Count; i++)
            {
                BWriter.Seek(StreamObjectsStart[i] + FileStart2, SeekOrigin.Begin);
                BWriter.Seek(4, SeekOrigin.Current);
                BWriter.WriteUInt32((uint)AudioOffsets[i]);
                BWriter.Seek(16, SeekOrigin.Current);
                BWriter.WriteUInt32((uint)MarkerOffsets[i]);
                ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            }
        }

        //*===============================================================================================
        //* FUNCTIONS
        //*===============================================================================================
        internal Dictionary<uint, EXSoundStream> GetFinalSoundsDictionary(Dictionary<uint, EXSoundStream> SoundsList, ProgressBar Bar, Label LabelInfo, string outputTarget)
        {
            Dictionary<uint, EXSoundStream> FinalSoundsDict = new Dictionary<uint, EXSoundStream>();

            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, SoundsList.Count());

            //Discard SFXs that has checked as "no output"
            foreach (KeyValuePair<uint, EXSoundStream> SoundToCheck in SoundsList)
            {
                if (SoundToCheck.Value.OutputThisSound)
                {
                    //Temporal Music
                    EXSoundStream soundToExport = new EXSoundStream
                    {
                        StartMarkers = new List<EXStreamStartMarker>(),
                        Markers = new List<EXStreamMarker>()
                    };

                    //Clone Start Markers WITHOUT REFERENCE
                    foreach (EXStreamStartMarker startMarker in SoundToCheck.Value.StartMarkers)
                    {
                        EXStreamStartMarker startMarkerCloned = new EXStreamStartMarker
                        {
                            Name = startMarker.Name,
                            Position = startMarker.Position,
                            MusicMakerType = startMarker.MusicMakerType,
                            Flags = startMarker.Flags,
                            Extra = startMarker.Extra,
                            LoopStart = startMarker.LoopStart,
                            MarkerCount = startMarker.MarkerCount,
                            LoopMarkerCount = startMarker.LoopMarkerCount,
                            MarkerPos = startMarker.MarkerPos,
                            IsInstant = startMarker.IsInstant,
                            InstantBuffer = startMarker.InstantBuffer,
                            StateA = startMarker.StateA,
                            StateB = startMarker.StateB
                        };
                        soundToExport.StartMarkers.Add(startMarkerCloned);
                    }

                    //Clone Markers WITHOUT REFERENCE
                    foreach (EXStreamMarker streamMarker in SoundToCheck.Value.Markers)
                    {
                        EXStreamMarker streamMarkerCloned = new EXStreamMarker
                        {
                            Name = streamMarker.Name,
                            Position = streamMarker.Position,
                            MusicMakerType = streamMarker.MusicMakerType,
                            Flags = streamMarker.Flags,
                            Extra = streamMarker.Extra,
                            LoopStart = streamMarker.LoopStart,
                            MarkerCount = streamMarker.MarkerCount,
                            LoopMarkerCount = streamMarker.LoopMarkerCount
                        };
                        soundToExport.Markers.Add(streamMarkerCloned);
                    }

                    //Copy properties
                    Reflection.CopyProperties(SoundToCheck.Value, soundToExport);
                    soundToExport.OutputThisSound = SoundToCheck.Value.OutputThisSound;
                    soundToExport.BaseVolume = SoundToCheck.Value.BaseVolume;

                    //Check platform
                    if (outputTarget.Equals("PC", StringComparison.OrdinalIgnoreCase))
                    {
                        FinalSoundsDict.Add(SoundToCheck.Key, soundToExport);
                    }
                    else if (outputTarget.Equals("PS2", StringComparison.OrdinalIgnoreCase))
                    {
                        AudioFunctions audiof = new AudioFunctions();
                        VAG_Encoder_Decoder.VagFunctions vagF = new VAG_Encoder_Decoder.VagFunctions();

                        //Parse audio to VAG
                        byte[] encodedVAGData = vagF.VAGEncoder(audiof.ConvertPCMDataToShortArray(soundToExport.PCM_Data), 16, 0, false);

                        //Set markers to 0 and calculate loop offset
                        foreach (EXStreamStartMarker strtMarker in soundToExport.StartMarkers)
                        {
                            strtMarker.StateA = 0;
                            strtMarker.StateB = 0;
                            strtMarker.Position = vagF.CalculateVAGLoopOffset(encodedVAGData.Length, strtMarker.Position, soundToExport.PCM_Data.Length);
                            strtMarker.LoopStart = vagF.CalculateVAGLoopOffset(encodedVAGData.Length, strtMarker.LoopStart, soundToExport.PCM_Data.Length);
                        }

                        //Calculate loop offset
                        foreach (EXStreamMarker dataMarker in soundToExport.Markers)
                        {
                            dataMarker.Position = vagF.CalculateVAGLoopOffset(encodedVAGData.Length, dataMarker.Position, soundToExport.PCM_Data.Length);
                            dataMarker.LoopStart = vagF.CalculateVAGLoopOffset(encodedVAGData.Length, dataMarker.LoopStart, soundToExport.PCM_Data.Length);
                        }

                        //Change data
                        soundToExport.IMA_ADPCM_DATA = encodedVAGData;

                        FinalSoundsDict.Add(SoundToCheck.Key, soundToExport);
                    }
                }
                GenericFunctions.SetLabelText(LabelInfo, "Checking Stream Data");
                ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            }

            return FinalSoundsDict;
        }
    }
}
