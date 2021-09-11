using EuroSound_Application.AudioFunctionsLibrary;
using EuroSound_Application.Editors_and_Tools;
using EuroSound_Application.StreamSounds;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EuroSound_Application.Musics
{
    internal class GenerateSFXMusicBank
    {
        private List<long> MarkersStartList = new List<long>();
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
            uint Hashcode = 0x00E00000 | (FileHashcode & 0x000fffff); //Apply bytes mask, example: 0x1BE0005C -> 0x0000005C
            BWriter.WriteUInt32(Hashcode);
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
        //* FILE SECTION 1
        //*===============================================================================================
        public void WriteFileSection1(BinaryStream BWriter, Dictionary<uint, EXMusic> MusicsDictionary, ProgressBar Bar)
        {
            //Update GUI
            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, MusicsDictionary.Count);

            BWriter.Seek((int)FileStart1, SeekOrigin.Begin);

            foreach (KeyValuePair<uint, EXMusic> MusicToExport in MusicsDictionary)
            {
                uint SoundStartOffset = (uint)BWriter.BaseStream.Position;
                MarkersStartList.Add(SoundStartOffset - FileStart2);

                //Start marker count
                BWriter.WriteUInt32((uint)MusicToExport.Value.StartMarkers.Count);
                //Marker count
                BWriter.WriteUInt32((uint)MusicToExport.Value.Markers.Count);
                //Start marker offset
                BWriter.WriteUInt32(0);
                //Marker offset
                BWriter.WriteUInt32(0);
                //Base volume
                BWriter.WriteUInt32(MusicToExport.Value.BaseVolume);

                long StartMarkerOffset = BWriter.BaseStream.Position - SoundStartOffset;

                //Start Markers Data
                for (int i = 0; i < MusicToExport.Value.StartMarkers.Count; i++)
                {
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].Name);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].Position);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].MusicMakerType);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].Flags);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].Extra);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].LoopStart);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].MarkerCount);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].LoopMarkerCount);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].MarkerPos);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].IsInstant);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].InstantBuffer);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].StateA);
                    BWriter.WriteUInt32(MusicToExport.Value.StartMarkers[i].StateB);
                }

                uint MarkerDataOffset = (uint)(BWriter.BaseStream.Position - SoundStartOffset);
                //Markers
                for (int j = 0; j < MusicToExport.Value.Markers.Count; j++)
                {
                    BWriter.WriteInt32(MusicToExport.Value.Markers[j].Name);
                    BWriter.WriteUInt32(MusicToExport.Value.Markers[j].Position);
                    BWriter.WriteUInt32(MusicToExport.Value.Markers[j].MusicMakerType);
                    BWriter.WriteUInt32(MusicToExport.Value.Markers[j].Flags);
                    BWriter.WriteUInt32(MusicToExport.Value.Markers[j].Extra);
                    BWriter.WriteUInt32(MusicToExport.Value.Markers[j].LoopStart);
                    BWriter.WriteUInt32(MusicToExport.Value.Markers[j].MarkerCount);
                    BWriter.WriteUInt32(MusicToExport.Value.Markers[j].LoopMarkerCount);
                }

                //Write Marker Data Offset
                long PrevPos = BWriter.BaseStream.Position;
                BWriter.Seek((int)SoundStartOffset, SeekOrigin.Begin);
                BWriter.Seek(8, SeekOrigin.Current);
                BWriter.WriteUInt32((uint)StartMarkerOffset);
                BWriter.WriteUInt32(MarkerDataOffset);
                BWriter.BaseStream.Seek(PrevPos, SeekOrigin.Begin);

                //Update GUI
                ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            }
            FileLength1 = BWriter.BaseStream.Position - FileStart1;
        }

        //*===============================================================================================
        //* FILE SECTION 2
        //*===============================================================================================
        public void WriteFileSection2(BinaryStream BWriter, Dictionary<uint, EXMusic> MusicsDictionary, ProgressBar Bar, string Target)
        {
            //Update GUI
            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, MusicsDictionary.Count);

            //Write ADPCM Data
            BWriter.Seek((int)FileStart2, SeekOrigin.Begin);

            foreach (KeyValuePair<uint, EXMusic> MusicToExport in MusicsDictionary)
            {
                //Initialize variables
                int IndexLC = 0;
                int IndexRC = 0;
                bool StereoInterleaving = true;
                int TotalLength = MusicToExport.Value.IMA_ADPCM_DATA_LeftChannel.Length + MusicToExport.Value.IMA_ADPCM_DATA_RightChannel.Length;

                if (Target.Equals("PC", StringComparison.OrdinalIgnoreCase))
                {
                    //Write ima data
                    for (int i = 0; i < TotalLength; i++)
                    {
                        if (StereoInterleaving)
                        {
                            BWriter.Write(MusicToExport.Value.IMA_ADPCM_DATA_LeftChannel[IndexLC]);
                            IndexLC++;
                        }
                        else
                        {
                            BWriter.Write(MusicToExport.Value.IMA_ADPCM_DATA_RightChannel[IndexRC]);
                            IndexRC++;
                        }
                        StereoInterleaving = !StereoInterleaving;
                    }
                }
                else if (Target.Equals("PS2", StringComparison.OrdinalIgnoreCase))
                {
                    //Write vag data
                    for (int i = 0; i < TotalLength; i++)
                    {
                        if (StereoInterleaving)
                        {
                            for (int j = 0; j < 128; j++)
                            {
                                if (IndexLC < MusicToExport.Value.IMA_ADPCM_DATA_LeftChannel.Length)
                                {
                                    BWriter.Write(MusicToExport.Value.IMA_ADPCM_DATA_LeftChannel[IndexLC]);
                                    IndexLC++;
                                }
                            }
                        }
                        else
                        {
                            for (int j = 0; j < 128; j++)
                            {
                                if (IndexRC < MusicToExport.Value.IMA_ADPCM_DATA_RightChannel.Length)
                                {
                                    BWriter.Write(MusicToExport.Value.IMA_ADPCM_DATA_RightChannel[IndexRC]);
                                    IndexRC++;
                                }
                            }
                        }
                        StereoInterleaving = !StereoInterleaving;
                    }
                }

                FileLength2 = TotalLength;
                FullFileLength = BWriter.BaseStream.Position;

                //Update GUI
                ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            }
        }

        //*===============================================================================================
        //* FINAL OFFSETS
        //*===============================================================================================
        public void WriteFinalOffsets(BinaryStream BWriter, ProgressBar Bar)
        {
            //Update GUI
            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, 3);

            //File Full Size
            BWriter.BaseStream.Seek(0xC, SeekOrigin.Begin);
            BWriter.WriteUInt32((uint)FullFileLength);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);

            //File length 1
            BWriter.BaseStream.Seek(0x14, SeekOrigin.Begin);
            BWriter.WriteUInt32((uint)FileLength1);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);

            //File length 2
            BWriter.BaseStream.Seek(0x1C, SeekOrigin.Begin);
            BWriter.WriteUInt32((uint)FileLength2);
            ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
        }

        //*===============================================================================================
        //* FUNCTIONS
        //*===============================================================================================
        internal Dictionary<uint, EXMusic> GetFinalMusicsDictionary(Dictionary<uint, EXMusic> MusicsList, ProgressBar Bar, Label LabelInfo, string outputTarget)
        {
            Dictionary<uint, EXMusic> FinalSoundsDict = new Dictionary<uint, EXMusic>();

            ToolsCommonFunctions.ProgressBarReset(Bar);
            GenericFunctions.ProgressBarSetMaximum(Bar, MusicsList.Count());

            //Discard SFXs that has checked as "no output"
            foreach (KeyValuePair<uint, EXMusic> MusicToCheck in MusicsList)
            {
                if (MusicToCheck.Value.OutputThisSound)
                {
                    //Check platform
                    if (outputTarget.Equals("PC", StringComparison.OrdinalIgnoreCase))
                    {
                        FinalSoundsDict.Add(MusicToCheck.Key, MusicToCheck.Value);
                    }
                    else if (outputTarget.Equals("PS2", StringComparison.OrdinalIgnoreCase))
                    {
                        //Temporal Music
                        EXMusic musicToExport = new EXMusic
                        {
                            StartMarkers = new List<EXStreamStartMarker>(),
                            Markers = new List<EXStreamMarker>()
                        };

                        //Clone Start Markers WITHOUT REFERENCE
                        foreach (EXStreamStartMarker startMarker in MusicToCheck.Value.StartMarkers)
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
                            musicToExport.StartMarkers.Add(startMarkerCloned);
                        }

                        //Clone Markers WITHOUT REFERENCE
                        foreach (EXStreamMarker streamMarker in MusicToCheck.Value.Markers)
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
                            musicToExport.Markers.Add(streamMarkerCloned);
                        }

                        //Copy properties
                        Reflection.CopyProperties(MusicToCheck.Value, musicToExport);
                        musicToExport.OutputThisSound = MusicToCheck.Value.OutputThisSound;
                        musicToExport.BaseVolume = MusicToCheck.Value.BaseVolume;

                        //Initialize VAG Encoder
                        AudioFunctions audiof = new AudioFunctions();
                        VAG_Encoder_Decoder.VagFunctions vagF = new VAG_Encoder_Decoder.VagFunctions();

                        //Parse audio to VAG
                        byte[] encodedVagDataL = vagF.VAGEncoder(audiof.ConvertPCMDataToShortArray(musicToExport.PCM_Data_LeftChannel), 16, 0, false);
                        byte[] encodedVagDataR = vagF.VAGEncoder(audiof.ConvertPCMDataToShortArray(musicToExport.PCM_Data_RightChannel), 16, 0, false);

                        //Set markers to 0 and calculate loop offset
                        foreach (EXStreamStartMarker strtMarker in musicToExport.StartMarkers)
                        {
                            strtMarker.StateA = 0;
                            strtMarker.StateB = 0;
                            strtMarker.Position = vagF.CalculateVAGLoopOffset(encodedVagDataL.Length, strtMarker.Position, musicToExport.PCM_Data_LeftChannel.Length);
                            strtMarker.LoopStart = vagF.CalculateVAGLoopOffset(encodedVagDataL.Length, strtMarker.LoopStart, musicToExport.PCM_Data_LeftChannel.Length);
                        }

                        //Calculate loop offset
                        foreach (EXStreamMarker dataMarker in musicToExport.Markers)
                        {
                            dataMarker.Position = vagF.CalculateVAGLoopOffset(encodedVagDataL.Length, dataMarker.Position, musicToExport.PCM_Data_LeftChannel.Length);
                            dataMarker.LoopStart = vagF.CalculateVAGLoopOffset(encodedVagDataL.Length, dataMarker.LoopStart, musicToExport.PCM_Data_LeftChannel.Length);
                        }

                        //Change data
                        musicToExport.IMA_ADPCM_DATA_RightChannel = encodedVagDataR;
                        musicToExport.IMA_ADPCM_DATA_LeftChannel = encodedVagDataL;

                        FinalSoundsDict.Add(MusicToCheck.Key, musicToExport);
                    }
                }
                GenericFunctions.SetLabelText(LabelInfo, "Checking Stream Data");
                ToolsCommonFunctions.ProgressBarAddValue(Bar, 1);
            }

            return FinalSoundsDict;
        }
    }
}
