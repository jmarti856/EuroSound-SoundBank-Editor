using EuroSound_Application.ApplicationPreferences;
using EuroSound_Application.ApplicationTargets;
using EuroSound_Application.Clases;
using EuroSound_Application.Clipboard_Stuff;
using EuroSound_Application.CustomControls.DebugTypes;
using EuroSound_Application.CustomControls.MoveMultiplesNodesForm;
using EuroSound_Application.CustomControls.ObjectInstancesForm;
using EuroSound_Application.Editors_and_Tools;
using EuroSound_Application.Editors_and_Tools.ApplicationTargets;
using EuroSound_Application.EuroSound_Audio_File;
using EuroSound_Application.EuroSoundInterchangeFile;
using EuroSound_Application.TreeViewLibraryFunctions;
using EuroSound_Application.TreeViewSorter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EuroSound_Application.SoundBanksEditor
{
    public partial class Frm_Soundbanks_Main
    {
        //*===============================================================================================
        //* ContextMenu_Folders
        //*===============================================================================================
        private void ContextMenu_Folders_AddAudio_Click(object sender, EventArgs e)
        {
            string nodeName = BrowsersAndDialogs.InputBoxDialog("Enter a name for new a new audio.", "New Audio");
            if (!string.IsNullOrEmpty(nodeName))
            {
                if (TreeNodeFunctions.CheckIfNodeExistsByText(TreeView_File, nodeName, true))
                {
                    MessageBox.Show(GenericFunctions.resourcesManager.GetString("Error_Adding_AlreadyExists"), "EuroSound", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    string audioPath = BrowsersAndDialogs.FileBrowserDialog("WAV Files (*.wav)|*.wav", 0, true);
                    if (!string.IsNullOrEmpty(audioPath))
                    {
                        if (GenericFunctions.AudioIsValid(audioPath, GlobalPreferences.SoundbankChannels, GlobalPreferences.SoundbankFrequency))
                        {
                            LoadAudio(audioPath, nodeName, false);
                        }
                        else
                        {
                            DialogResult TryToReload = MessageBox.Show(string.Join("", "Error, this audio file is not correct, the specifies are: ", GlobalPreferences.SoundbankChannels, " channels, the rate must be ", GlobalPreferences.SoundbankFrequency, "Hz, must have ", GlobalPreferences.SoundbankBits, " bits per sample and encoded in ", GlobalPreferences.SoundbankEncoding, Environment.NewLine, Environment.NewLine, "Do you want that EuroSound tries to convert it to a valid format?"), "EuroSound", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                            if (TryToReload == DialogResult.Yes)
                            {
                                LoadAudio(audioPath, nodeName, true);
                            }
                        }
                    }
                }
            }
        }

        private void ContextMenuFolder_Paste_Click(object sender, EventArgs e)
        {
            //Sound object
            if (Clipboard_SFX.SoundObject != null)
            {
                uint NewSoundKey = GenericFunctions.GetNewObjectID(ProjectInfo);
                EXSound soundToAdd = new EXSound()
                {
                    Ducker = Clipboard_SFX.SoundObject.Ducker,
                    DuckerLength = Clipboard_SFX.SoundObject.DuckerLength,
                    Flags = Clipboard_SFX.SoundObject.Flags,
                    Hashcode = Clipboard_SFX.SoundObject.Hashcode,
                    InnerRadiusReal = Clipboard_SFX.SoundObject.InnerRadiusReal,
                    MasterVolume = Clipboard_SFX.SoundObject.MasterVolume,
                    MaxDelay = Clipboard_SFX.SoundObject.MaxDelay,
                    MaxVoices = Clipboard_SFX.SoundObject.MaxVoices,
                    MinDelay = Clipboard_SFX.SoundObject.MinDelay,
                    OuterRadiusReal = Clipboard_SFX.SoundObject.OuterRadiusReal,
                    OutputThisSound = Clipboard_SFX.SoundObject.OutputThisSound,
                    Priority = Clipboard_SFX.SoundObject.Priority,
                    ReverbSend = Clipboard_SFX.SoundObject.ReverbSend,
                    OutputTarget = Clipboard_SFX.SoundObject.OutputTarget,
                    TrackingType = Clipboard_SFX.SoundObject.TrackingType
                };

                //Add samples
                uint[] childNodes = new uint[Clipboard_SFX.SoundObject.Samples.Count];
                int index = 0;
                foreach (var sample in Clipboard_SFX.SoundObject.Samples)
                {
                    uint newKey = GenericFunctions.GetNewObjectID(ProjectInfo);
                    soundToAdd.Samples.Add(newKey, sample.Value);
                    childNodes[index] = newKey;
                    index++;
                }

                //Add new object
                SoundsList.Add(NewSoundKey, soundToAdd);

                //Create tree node
                TreeNode nodeToAdd = new TreeNode
                {
                    Name = NewSoundKey.ToString(),
                    Text = GenericFunctions.GetNextAvailableName(Clipboard_SFX.TreeNodeObject.Text, TreeView_File),
                    SelectedImageIndex = Clipboard_SFX.TreeNodeObject.SelectedImageIndex,
                    ImageIndex = Clipboard_SFX.TreeNodeObject.ImageIndex,
                    Tag = Clipboard_SFX.TreeNodeObject.Tag,
                    ForeColor = Clipboard_SFX.TreeNodeObject.ForeColor
                };

                //Add child nodes
                if (Clipboard_SFX.TreeNodeObject.Nodes.Count > 0)
                {
                    for (int j = 0; j < Clipboard_SFX.TreeNodeObject.Nodes.Count; j++)
                    {
                        TreeNode childNodeToAdd = new TreeNode
                        {
                            Name = childNodes[j].ToString(),
                            Text = GenericFunctions.GetNextAvailableName(Clipboard_SFX.TreeNodeObject.Nodes[j].Text, TreeView_File),
                            SelectedImageIndex = Clipboard_SFX.TreeNodeObject.Nodes[j].SelectedImageIndex,
                            ImageIndex = Clipboard_SFX.TreeNodeObject.Nodes[j].ImageIndex,
                            Tag = Clipboard_SFX.TreeNodeObject.Nodes[j].Tag,
                            ForeColor = Clipboard_SFX.TreeNodeObject.Nodes[j].ForeColor
                        };
                        nodeToAdd.Nodes.Add(childNodeToAdd);
                    }
                }

                //Add node
                if (Clipboard_SFX.IsStream)
                {
                    TreeView_File.Nodes[2].Nodes.Add(nodeToAdd);
                }
                else
                {
                    TreeView_File.Nodes[1].Nodes.Add(nodeToAdd);
                }

                //Clear class
                Clipboard_SFX.SoundObject = null;
                Clipboard_SFX.TreeNodeObject = null;
            }

            //Audio object
            if (Clipboard_Audio.AudioObject != null)
            {
                if (!AudioDataDict.ContainsKey(Clipboard_Audio.MD5Audio))
                {
                    EXAudio audioObject = new EXAudio
                    {
                        Bits = Clipboard_Audio.AudioObject.Bits,
                        Channels = Clipboard_Audio.AudioObject.Channels,
                        Dependencies = Clipboard_Audio.AudioObject.Dependencies,
                        Duration = Clipboard_Audio.AudioObject.Duration,
                        Encoding = Clipboard_Audio.AudioObject.Encoding,
                        Flags = Clipboard_Audio.AudioObject.Flags,
                        Frequency = Clipboard_Audio.AudioObject.Frequency,
                        LoopOffset = Clipboard_Audio.AudioObject.LoopOffset,
                        LoadedFileName = Clipboard_Audio.AudioObject.LoadedFileName,
                        PCMdata = Clipboard_Audio.AudioObject.PCMdata,
                        PSIsample = Clipboard_Audio.AudioObject.PSIsample,
                        FrequencyPS2 = Clipboard_Audio.AudioObject.FrequencyPS2
                    };

                    //Add new object
                    AudioDataDict.Add(Clipboard_Audio.MD5Audio, audioObject);

                    //Create tree node
                    TreeNode nodeToAdd = new TreeNode
                    {
                        Name = Clipboard_Audio.MD5Audio,
                        Text = Clipboard_Audio.TreeNodeObject.Text,
                        SelectedImageIndex = Clipboard_Audio.TreeNodeObject.SelectedImageIndex,
                        ImageIndex = Clipboard_Audio.TreeNodeObject.ImageIndex,
                        Tag = Clipboard_Audio.TreeNodeObject.Tag,
                        ForeColor = Clipboard_Audio.TreeNodeObject.ForeColor
                    };
                    TreeView_File.Nodes[0].Nodes.Add(nodeToAdd);
                }

                //Clear class
                Clipboard_Audio.AudioObject = null;
                Clipboard_Audio.TreeNodeObject = null;
            }
        }

        private void LoadAudio(string AudioPath, string AudioName, bool ConvertData)
        {
            string MD5Hash = GenericFunctions.CalculateMD5(AudioPath);
            if (!AudioDataDict.ContainsKey(MD5Hash))
            {
                //Set Program status
                GenericFunctions.ParentFormStatusBar.ShowProgramStatus(GenericFunctions.resourcesManager.GetString("StatusBar_ConvertingAudio"));

                //Load or load and convert
                EXAudio newAudio;
                if (ConvertData)
                {
                    newAudio = EXSoundbanksFunctions.LoadAndConvertData(AudioPath);
                }
                else
                {
                    newAudio = EXSoundbanksFunctions.LoadAudioData(AudioPath);
                }

                //Add data to dictionary and create tree node
                if (newAudio != null)
                {
                    AudioDataDict.Add(MD5Hash, newAudio);
                    TreeNodeFunctions.TreeNodeAddNewNode(TreeView_File.SelectedNode.Name, MD5Hash, AudioName, 7, 7, (byte)Enumerations.TreeNodeType.Audio, true, true, false, SystemColors.WindowText, TreeView_File);

                    //Sort tree view
                    if (GlobalPreferences.AutomaticallySortNodes)
                    {
                        Point scrollpos = TreeNodeFunctions.GetTreeViewScrollPos(TreeView_File);
                        TreeView_File.TreeViewNodeSorter = new NodeSorter();
                        TreeNodeFunctions.SetTreeViewScrollPos(TreeView_File, scrollpos);
                    }

                    //Update project status variable
                    ProjectInfo.FileHasBeenModified = true;
                }

                //Update Status Bar
                GenericFunctions.ParentFormStatusBar.ShowProgramStatus(GenericFunctions.resourcesManager.GetString("StatusBar_Status_Ready"));
            }
            else
            {
                MessageBox.Show(GenericFunctions.resourcesManager.GetString("Error_Adding_AudioExists"), "EuroSound", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ContextMenu_Folders_AddSample_Click(object sender, EventArgs e)
        {
            string sampleName = BrowsersAndDialogs.InputBoxDialog("Enter a name for new a new sample.", "New Sample");
            if (!string.IsNullOrEmpty(sampleName))
            {
                if (TreeNodeFunctions.CheckIfNodeExistsByText(TreeView_File, sampleName, true))
                {
                    MessageBox.Show(GenericFunctions.resourcesManager.GetString("Error_Adding_AlreadyExists"), "EuroSound", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    //Get ID
                    uint SampleID = GenericFunctions.GetNewObjectID(ProjectInfo);

                    //Add Item
                    TreeNodeFunctions.TreeNodeAddNewNode(TreeView_File.SelectedNode.Name, SampleID.ToString(), sampleName, 4, 4, (byte)Enumerations.TreeNodeType.Sample, true, true, false, SystemColors.WindowText, TreeView_File);
                    EXSoundbanksFunctions.AddSampleToSound(EXSoundbanksFunctions.ReturnSoundFromDictionary(uint.Parse(TreeView_File.SelectedNode.Name), SoundsList), SampleID, true);

                    //Sort tree view
                    if (GlobalPreferences.AutomaticallySortNodes)
                    {
                        Point scrollpos = TreeNodeFunctions.GetTreeViewScrollPos(TreeView_File);
                        TreeView_File.TreeViewNodeSorter = new NodeSorter();
                        TreeNodeFunctions.SetTreeViewScrollPos(TreeView_File, scrollpos);
                    }

                    //Update project status variable
                    ProjectInfo.FileHasBeenModified = true;
                }
            }
        }

        private void ContextMenu_Folders_AddSound_Click(object sender, EventArgs e)
        {
            string soundName = BrowsersAndDialogs.InputBoxDialog("Enter a name for new sound.", "New Sound");
            if (!string.IsNullOrEmpty(soundName))
            {
                if (TreeNodeFunctions.CheckIfNodeExistsByText(TreeView_File, soundName, true))
                {
                    MessageBox.Show(GenericFunctions.resourcesManager.GetString("Error_Adding_AlreadyExists"), "EuroSound", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    uint SoundID = GenericFunctions.GetNewObjectID(ProjectInfo);
                    TreeNodeFunctions.TreeNodeAddNewNode(TreeView_File.SelectedNode.Name, SoundID.ToString(), soundName, 2, 2, (byte)Enumerations.TreeNodeType.Sound, true, true, false, SystemColors.WindowText, TreeView_File);

                    //Add Empty Sound
                    SoundsList.Add(SoundID, new EXSound
                    {
                        Hashcode = 0x00000000
                    });

                    //Sort tree view
                    if (GlobalPreferences.AutomaticallySortNodes)
                    {
                        Point scrollpos = TreeNodeFunctions.GetTreeViewScrollPos(TreeView_File);
                        TreeView_File.TreeViewNodeSorter = new NodeSorter();
                        TreeNodeFunctions.SetTreeViewScrollPos(TreeView_File, scrollpos);
                    }

                    //Update project status variable
                    ProjectInfo.FileHasBeenModified = true;
                }
            }
        }

        private void ContextMenuFolder_AddTarget_Click(object sender, EventArgs e)
        {
            EXAppTarget outTarget = new EXAppTarget
            {
                BinaryName = EXAppTarget_Functions.GetBinaryName(ProjectInfo, GlobalPreferences.SelectedProfileName)
            };
            using (Frm_ApplicationTarget newOutTarget = new Frm_ApplicationTarget(outTarget, null, TreeView_File) { Owner = this })
            {
                newOutTarget.ShowDialog();

                if (newOutTarget.DialogResult == DialogResult.OK)
                {
                    uint SoundID = GenericFunctions.GetNewObjectID(ProjectInfo);
                    TreeNodeFunctions.TreeNodeAddNewNode(TreeView_File.SelectedNode.Name, SoundID.ToString(), outTarget.Name, 10, 10, (byte)Enumerations.TreeNodeType.Target, true, true, false, SystemColors.WindowText, TreeView_File);

                    //Add Target
                    OutputTargets.Add(SoundID, outTarget);

                    //Sort tree view
                    if (GlobalPreferences.AutomaticallySortNodes)
                    {
                        Point scrollpos = TreeNodeFunctions.GetTreeViewScrollPos(TreeView_File);
                        TreeView_File.TreeViewNodeSorter = new NodeSorter();
                        TreeNodeFunctions.SetTreeViewScrollPos(TreeView_File, scrollpos);
                    }

                    //Update project status variable
                    ProjectInfo.FileHasBeenModified = true;
                }
            }
        }

        private void ContextMenu_Folders_Delete_Click(object sender, EventArgs e)
        {
            if (TreeView_File.SelectedNode.Level > 0)
            {
                ToolsCommonFunctions.RemoveEngineXObject("Delete folder:", (int)Enumerations.EXObjectType.EXSoundFolder, TreeView_File, TreeView_File.SelectedNode, SoundsList, null, ProjectInfo, UndoListSounds, UndoListNodes, MenuItem_Edit_Undo, Tag.ToString());
            }
        }

        private void MenuItem_Folder_Collapse_Click(object sender, EventArgs e)
        {
            TreeView_File.SelectedNode.Collapse();
        }

        private void ContextMenu_Folders_Expand_Click(object sender, EventArgs e)
        {
            TreeView_File.SelectedNode.Expand();
        }

        private void ContextMenu_Folders_Move_Click(object sender, EventArgs e)
        {
            using (EuroSound_NodesToFolder SoundsToFolders = new EuroSound_NodesToFolder(TreeView_File, ProjectInfo, TreeNodeFunctions.FindRootNode(TreeView_File.SelectedNode).Name, TreeView_File.SelectedNode.Name) { Owner = this })
            {
                SoundsToFolders.ShowDialog();
            }
        }

        private void ContextMenu_Folders_New_Click(object sender, EventArgs e)
        {
            string folderName = BrowsersAndDialogs.InputBoxDialog("Enter a name for new folder.", "New Folder");
            if (TreeNodeFunctions.CheckIfNodeExistsByText(TreeView_File, folderName, true))
            {
                MessageBox.Show(GenericFunctions.resourcesManager.GetString("Error_Adding_AlreadyExists"), "EuroSound", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (!string.IsNullOrEmpty(folderName))
                {
                    uint FolderID = GenericFunctions.GetNewObjectID(ProjectInfo);
                    TreeNodeFunctions.TreeNodeAddNewNode(TreeView_File.SelectedNode.Name, FolderID.ToString(), folderName, 0, 0, (byte)Enumerations.TreeNodeType.Folder, true, true, false, SystemColors.WindowText, TreeView_File);

                    //Sort tree view
                    if (GlobalPreferences.AutomaticallySortNodes)
                    {
                        Point scrollpos = TreeNodeFunctions.GetTreeViewScrollPos(TreeView_File);
                        TreeView_File.TreeViewNodeSorter = new NodeSorter();
                        TreeNodeFunctions.SetTreeViewScrollPos(TreeView_File, scrollpos);
                    }

                    //Update project status variable
                    ProjectInfo.FileHasBeenModified = true;
                }
            }
        }

        private void ContextMenu_Folders_Sort_Click(object sender, EventArgs e)
        {
            Point scrollpos = TreeNodeFunctions.GetTreeViewScrollPos(TreeView_File);
            TreeView_File.TreeViewNodeSorter = new NodeSorter();
            TreeNodeFunctions.SetTreeViewScrollPos(TreeView_File, scrollpos);
        }

        private void ContextMenuFolder_Purge_Click(object sender, EventArgs e)
        {
            List<string> purgedAudiosList = new List<string>();
            IEnumerable<string> GetAudiosListToRemove = EXSoundbanksFunctions.GetAudiosToPurge(AudioDataDict, SoundsList);

            if (GetAudiosListToRemove.Any())
            {
                bool prevMessagesValue = GlobalPreferences.NoWarningMessagesBox;
                GlobalPreferences.NoWarningMessagesBox = true;

                foreach (string itemToRemove in GetAudiosListToRemove)
                {
                    TreeNode nodeToRemove = TreeView_File.Nodes.Find(itemToRemove, true)[0];
                    if (nodeToRemove != null)
                    {
                        purgedAudiosList.Add("2Purged Audio: " + nodeToRemove.Text);
                        ToolsCommonFunctions.RemoveEngineXObject("Purge:", (int)Enumerations.EXObjectType.EXAudio, TreeView_File, nodeToRemove, AudioDataDict, SoundsList, ProjectInfo, UndoListSounds, UndoListNodes, MenuItem_Edit_Undo, Tag.ToString());
                    }
                }
                purgedAudiosList.TrimExcess();

                //Restore prev value
                GlobalPreferences.NoWarningMessagesBox = prevMessagesValue;

                //Update project status variable
                ProjectInfo.FileHasBeenModified = true;

                //Show messages list
                if (purgedAudiosList.Count > 0)
                {
                    GenericFunctions.ShowErrorsAndWarningsList(purgedAudiosList, "Purged Audios", this);
                }
            }
        }

        private void ContextMenuFolder_Rename_Click(object sender, EventArgs e)
        {
            TreeNodeFunctions.EditNodeLabel(TreeView_File, TreeView_File.SelectedNode);
        }
        private void ContextMenuFolders_TextColor_Click(object sender, EventArgs e)
        {
            TreeNodeFunctions.ChangeNodeColor(TreeView_File.SelectedNode, ProjectInfo);
        }

        private void ContextMenuFolder_ExportSounds_Click(object sender, EventArgs e)
        {
            string exportPath = BrowsersAndDialogs.SaveFileBrowser("EuroSound Interchange File (*.esif)|*.ESIF", 0, true, "");

            if (!string.IsNullOrEmpty(exportPath))
            {
                IList<TreeNode> childNodesCollection = new List<TreeNode>();
                ESIF_Exporter esifExporter = new ESIF_Exporter();

                TreeNodeFunctions.GetNodesInsideFolder(TreeView_File, TreeView_File.SelectedNode, childNodesCollection);
                esifExporter.ExportFolder(childNodesCollection, exportPath, SoundsList, AudioDataDict, TreeView_File);
            }
        }

        private void ContextMenuFolder_ImportESIF_Click(object sender, EventArgs e)
        {
            string filePath = BrowsersAndDialogs.FileBrowserDialog("EuroSound Interchange File (*.ESIF)|*.esif", 0, true);
            if (!string.IsNullOrEmpty(filePath))
            {
                EISF_SoundBankFiles euroSoundPropsFileLoader = new EISF_SoundBankFiles();
                IList<string> importResults = euroSoundPropsFileLoader.LoadSFX_File(filePath, ProjectInfo, SoundsList, AudioDataDict, TreeView_File);
                if (importResults.Count > 0)
                {
                    GenericFunctions.ShowErrorsAndWarningsList(importResults, "Import Results", this);
                }
            }
        }

        private void ContextMenuFolder_ImportESAF_Click(object sender, EventArgs e)
        {
            string filePath = BrowsersAndDialogs.FileBrowserDialog("EuroSound Audio Frequencies File (*.ESAF)|*.esaf", 0, true);
            if (!string.IsNullOrEmpty(filePath))
            {
                ESAF_Loader EuroSoundOffsetFile = new ESAF_Loader();
                EuroSoundOffsetFile.LoadSFX_File(filePath, ProjectInfo, SoundsList, AudioDataDict);
            }
        }

        //*===============================================================================================
        //* ContextMenu_Sound
        //*===============================================================================================
        private void ContextMenu_Sound_Properties_Click(object sender, EventArgs e)
        {
            OpenSoundProperties(TreeView_File.SelectedNode);
        }

        private void ContextMenuSound_Copy_Click(object sender, EventArgs e)
        {
            EXSound selectedSound = EXSoundbanksFunctions.ReturnSoundFromDictionary(uint.Parse(TreeView_File.SelectedNode.Name), SoundsList);
            if (selectedSound != null)
            {
                Clipboard_SFX.TreeNodeObject = TreeView_File.SelectedNode;
                Clipboard_SFX.SoundObject = new EXSound()
                {
                    Ducker = selectedSound.Ducker,
                    DuckerLength = selectedSound.DuckerLength,
                    Flags = selectedSound.Flags,
                    Hashcode = selectedSound.Hashcode,
                    InnerRadiusReal = selectedSound.InnerRadiusReal,
                    MasterVolume = selectedSound.MasterVolume,
                    MaxDelay = selectedSound.MaxDelay,
                    MaxVoices = selectedSound.MaxVoices,
                    MinDelay = selectedSound.MinDelay,
                    OuterRadiusReal = selectedSound.OuterRadiusReal,
                    OutputThisSound = selectedSound.OutputThisSound,
                    Priority = selectedSound.Priority,
                    ReverbSend = selectedSound.ReverbSend,
                    OutputTarget = selectedSound.OutputTarget,
                    Samples = new Dictionary<uint, EXSample>(selectedSound.Samples),
                    TrackingType = selectedSound.TrackingType
                };

                //Check if is a stream
                TreeNode nodeRootName = TreeNodeFunctions.FindRootNode(TreeView_File.SelectedNode);
                if (nodeRootName.Name.Equals("StreamedSounds"))
                {
                    Clipboard_SFX.IsStream = true;
                }
                else
                {
                    Clipboard_SFX.IsStream = false;
                }
            }
        }

        private void ContextMenu_Sound_Remove_Click(object sender, EventArgs e)
        {
            ToolsCommonFunctions.RemoveEngineXObject("Remove SFX:", (int)Enumerations.EXObjectType.EXSound, TreeView_File, TreeView_File.SelectedNode, SoundsList, null, ProjectInfo, UndoListSounds, UndoListNodes, MenuItem_Edit_Undo, Tag.ToString());
        }

        private void ContextMenu_Sound_Rename_Click(object sender, EventArgs e)
        {
            TreeNodeFunctions.EditNodeLabel(TreeView_File, TreeView_File.SelectedNode);
        }

        private void ContextMenu_Sound_TextColor_Click(object sender, EventArgs e)
        {
            TreeNodeFunctions.ChangeNodeColor(TreeView_File.SelectedNode, ProjectInfo);
        }

        private void ContextMenuSound_ExportSingle_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = TreeView_File.SelectedNode;
            string exportPath = BrowsersAndDialogs.SaveFileBrowser("EuroSound Interchange File (*.esif)|*.ESIF", 0, true, selectedNode.Text);

            if (!string.IsNullOrEmpty(exportPath))
            {
                ESIF_Exporter ESIF_Exp = new ESIF_Exporter();
                ESIF_Exp.ExportSingleSFX(exportPath, uint.Parse(selectedNode.Name), SoundsList, AudioDataDict, TreeView_File);
            }
        }

        //*===============================================================================================
        //* ContextMenu_Sample
        //*===============================================================================================
        private void ContextMenu_Sample_Properties_Click(object sender, EventArgs e)
        {
            OpenSampleProperties(TreeView_File.SelectedNode);
        }

        private void ContextMenu_Sample_Remove_Click(object sender, EventArgs e)
        {
            ToolsCommonFunctions.RemoveEngineXObject("Remove sample:", (int)Enumerations.EXObjectType.EXSample, TreeView_File, TreeView_File.SelectedNode, SoundsList, null, ProjectInfo, UndoListSounds, UndoListNodes, MenuItem_Edit_Undo, Tag.ToString());
        }
        private void ContextMenu_Sample_Rename_Click(object sender, System.EventArgs e)
        {
            TreeNodeFunctions.EditNodeLabel(TreeView_File, TreeView_File.SelectedNode);
        }

        private void ContextMenu_Sample_TextColor_Click(object sender, EventArgs e)
        {
            TreeNodeFunctions.ChangeNodeColor(TreeView_File.SelectedNode, ProjectInfo);
        }

        //*===============================================================================================
        //* ContextMenu_Audio
        //*===============================================================================================
        private void ContextMenuAudio_Properties_Click(object sender, EventArgs e)
        {
            OpenAudioProperties(TreeView_File.SelectedNode);
        }

        private void ContextMenuAudio_Copy_Click(object sender, EventArgs e)
        {
            EXAudio selectedSound = TreeNodeFunctions.GetSelectedAudio(TreeView_File.SelectedNode.Name, AudioDataDict);
            if (selectedSound != null)
            {
                Clipboard_Audio.MD5Audio = TreeView_File.SelectedNode.Name;
                Clipboard_Audio.TreeNodeObject = TreeView_File.SelectedNode;
                Clipboard_Audio.AudioObject = new EXAudio()
                {
                    Bits = selectedSound.Bits,
                    Channels = selectedSound.Channels,
                    Dependencies = selectedSound.Dependencies,
                    Duration = selectedSound.Duration,
                    Encoding = selectedSound.Encoding,
                    Flags = selectedSound.Flags,
                    Frequency = selectedSound.Frequency,
                    LoopOffset = selectedSound.LoopOffset,
                    LoadedFileName = selectedSound.LoadedFileName,
                    PCMdata = selectedSound.PCMdata,
                    PSIsample = selectedSound.PSIsample,
                    FrequencyPS2 = selectedSound.FrequencyPS2
                };
            }
        }

        private void ContextMenuAudio_Remove_Click(object sender, EventArgs e)
        {
            //Remove Item
            RemoveAudioAndWarningDependencies(TreeView_File.SelectedNode);
        }

        private void ContextMenuAudio_Rename_Click(object sender, EventArgs e)
        {
            TreeNodeFunctions.EditNodeLabel(TreeView_File, TreeView_File.SelectedNode);
        }

        private void ContextMenuAudio_TextColor_Click(object sender, EventArgs e)
        {
            TreeNodeFunctions.ChangeNodeColor(TreeView_File.SelectedNode, ProjectInfo);
        }

        private void ContextMenuAudio_Usage_Click(object sender, EventArgs e)
        {
            IEnumerable<string> dependenciesList = EXSoundbanksFunctions.GetAudioDependencies(TreeView_File.SelectedNode.Name, TreeView_File.SelectedNode.Text, SoundsList, TreeView_File, true);
            if (dependenciesList.Any())
            {
                EuroSound_ItemUsage ShowDependencies = new EuroSound_ItemUsage(dependenciesList, Tag.ToString())
                {
                    Text = "Audio Usage",
                    Owner = Owner
                };
                ShowDependencies.ShowDialog();
                ShowDependencies.Dispose();
            }
            else
            {
                MessageBox.Show(GenericFunctions.resourcesManager.GetString("ItemHasNoDependencies"), "EuroSound", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //*===============================================================================================
        //* ContextMenu_Target
        //*===============================================================================================
        private void ContextMenuTargets_Delete_Click(object sender, EventArgs e)
        {
            ToolsCommonFunctions.RemoveTargetSelectedNode(TreeView_File.SelectedNode, OutputTargets, TreeView_File, ProjectInfo);
        }

        private void ContextMenuTargets_TextColor_Click(object sender, EventArgs e)
        {
            TreeNodeFunctions.ChangeNodeColor(TreeView_File.SelectedNode, ProjectInfo);
        }

        private void ContextMenuTargets_Properties_Click(object sender, EventArgs e)
        {
            OpenTargetProperties(TreeView_File.SelectedNode);
        }

        private void ContextMenuTargets_Output_Click(object sender, EventArgs e)
        {
            //Debug options form
            int debugOptions = 0;
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                using (EuroSound_DebugTypes DebugOpts = new EuroSound_DebugTypes(new string[] { "SFX Elements", "Sample info elements", "Sample Data" }))
                {
                    DebugOpts.Owner = Owner;
                    if (DebugOpts.ShowDialog() == DialogResult.OK)
                    {
                        debugOptions = DebugOpts.CheckedOptions;
                    }
                }
            }

            //Build form file
            EXAppTarget selectedTarget = OutputTargets[uint.Parse(TreeView_File.SelectedNode.Name.ToString())];
            using (Frm_OutputTargetFileBuilder buildSFX = new Frm_OutputTargetFileBuilder(ProjectInfo, selectedTarget, OutputTargets, debugOptions, Tag.ToString()) { Owner = this })
            {
                buildSFX.ShowDialog();
            }
        }

        //*===============================================================================================
        //* ContextMenu_HashcodesList
        //*===============================================================================================
        private void MenuItem_CopyHashcode_Click(object sender, EventArgs e)
        {
            if (ListView_Hashcodes.SelectedItems.Count > 0)
            {
                Clipboard.Clear();
                Clipboard.SetText(ListView_Hashcodes.SelectedItems[0].SubItems[1].Text);
            }
        }

        private void MenuItem_CopyLabel_Click(object sender, EventArgs e)
        {
            if (ListView_Hashcodes.SelectedItems.Count > 0)
            {
                Clipboard.Clear();
                Clipboard.SetText(ListView_Hashcodes.SelectedItems[0].SubItems[2].Text);
            }
        }
    }
}