using EuroSound_Application.ApplicationPreferences;
using EuroSound_Application.ApplicationRegistryFunctions;
using EuroSound_Application.Clases;
using EuroSound_Application.CurrentProjectFunctions;
using EuroSound_Application.CustomControls.FlagsForm;
using EuroSound_Application.HashCodesFunctions;
using EuroSound_Application.TreeViewLibraryFunctions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EuroSound_Application.SoundBanksEditor
{
    public partial class Frm_EffectProperties : Form
    {
        //*===============================================================================================
        //* Global Variables
        //*===============================================================================================
        private Form OpenForm;
        private EXSound SelectedSound;
        private Thread DataToCombobox;
        private ProjectFile fileProperties;
        private string TreeNodeSoundName, SoundSection;
        private bool OriginalHasSubSFX = false, SubSFXToggled = false, flagsModified = false;
        public Frm_EffectProperties(EXSound SoundToCheck, ProjectFile FileProperties, string SoundName, string Section)
        {
            InitializeComponent();
            SelectedSound = SoundToCheck;
            TreeNodeSoundName = SoundName;
            SoundSection = Section;
            fileProperties = FileProperties;
        }

        //*===============================================================================================
        //* Form Load
        //*===============================================================================================
        private void Frm_EffectProperties_Load(object sender, EventArgs e)
        {
            //Sound Data defines
            if (GenericFunctions.FileIsModified(GlobalPreferences.HT_SoundsDataMD5, GlobalPreferences.HT_SoundsDataPath))
            {
                Hashcodes.LoadSoundDataFile(GlobalPreferences.HT_SoundsDataPath);
            }

            //Sound defines
            if (GenericFunctions.FileIsModified(GlobalPreferences.HT_SoundsMD5, GlobalPreferences.HT_SoundsPath))
            {
                Hashcodes.LoadSoundHashcodes(GlobalPreferences.HT_SoundsPath);
            }

            //Get Parent Form
            OpenForm = GenericFunctions.GetFormByName("Frm_Soundbanks_Main", Tag.ToString());

            //---Add data to combobox--
            AddDataToCombobox();
        }

        private void Frm_EffectProperties_Shown(object sender, EventArgs e)
        {
            //---Required for Engine X---
            numeric_duckerlength.Value = SelectedSound.DuckerLength;
            numeric_mindelay.Value = SelectedSound.MinDelay;
            numeric_maxdelay.Value = SelectedSound.MaxDelay;
            Textbox_InnerRadius.Text = SelectedSound.InnerRadiusReal.ToString();
            Textbox_OuterRadius.Text = SelectedSound.OuterRadiusReal.ToString();
            numeric_reverbsend.Value = SelectedSound.ReverbSend;
            cbx_trackingtype.SelectedIndex = SelectedSound.TrackingType;
            numeric_maxvoices.Value = SelectedSound.MaxVoices;
            numeric_priority.Value = SelectedSound.Priority;
            numeric_ducker.Value = SelectedSound.Ducker;
            numeric_mastervolume.Value = SelectedSound.MasterVolume;
            textbox_flags.Tag = SelectedSound.Flags;
            Checkbox_OutputThisSound.Checked = SelectedSound.OutputThisSound;

            //Target
            Combobox_OutputTarget.SelectedIndex = SelectedSound.OutputTarget;

            //SubSFX
            OriginalHasSubSFX = EXSoundbanksFunctions.SubSFXFlagChecked(SelectedSound.Flags);

            //--Print Flags--
            textbox_flags.Text = GenericFunctions.PrintCheckedFlags("SoundFlags", 16, Convert.ToUInt16(textbox_flags.Tag));

            //---Print Samples--
            Thread printSampleList = new Thread(() =>
            {
                if (SelectedSound.Samples != null)
                {
                    foreach (KeyValuePair<uint, EXSample> storedSample in SelectedSound.Samples)
                    {
                        TreeNode[] nodesCollection = ((Frm_Soundbanks_Main)OpenForm).TreeView_File.Nodes.Find(storedSample.Key.ToString(), true);
                        if (nodesCollection.Length > 0)
                        {
                            string sampleName = nodesCollection[0].Text;

                            //Insert item
                            try
                            {
                                List_Samples.Invoke((MethodInvoker)delegate
                                {
                                    List_Samples.Items.Add(new ListViewItem
                                    {
                                        Text = sampleName,
                                        Tag = storedSample.Key,
                                        ImageIndex = 0,
                                        StateImageIndex = 0
                                    });
                                });
                                Thread.Sleep(60);
                            }
                            catch
                            {
                                //Quit loop, probably the control is disposed
                                break;
                            }
                        }
                    }
                }
            })
            {
                IsBackground = true
            };
            printSampleList.Start();
        }

        //*===============================================================================================
        //* Form Events Controls
        //*===============================================================================================
        private void Button_cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Button_ok_Click(object sender, EventArgs e)
        {
            if (cbx_hashcode.SelectedValue != null)
            {
                SelectedSound.Hashcode = (uint)cbx_hashcode.SelectedValue;
            }
            SelectedSound.DuckerLength = (short)numeric_duckerlength.Value;
            SelectedSound.MinDelay = (short)numeric_mindelay.Value;
            SelectedSound.MaxDelay = (short)numeric_maxdelay.Value;
            SelectedSound.InnerRadiusReal = Convert.ToInt16(Textbox_InnerRadius.Text);
            SelectedSound.OuterRadiusReal = Convert.ToInt16(Textbox_OuterRadius.Text);
            SelectedSound.ReverbSend = (sbyte)numeric_reverbsend.Value;
            SelectedSound.TrackingType = (sbyte)cbx_trackingtype.SelectedIndex;
            SelectedSound.MaxVoices = (sbyte)numeric_maxvoices.Value;
            SelectedSound.Priority = (sbyte)numeric_priority.Value;
            SelectedSound.Ducker = (sbyte)numeric_ducker.Value;
            SelectedSound.MasterVolume = (sbyte)numeric_mastervolume.Value;
            SelectedSound.Flags = Convert.ToUInt16(textbox_flags.Tag);
            SelectedSound.OutputThisSound = Checkbox_OutputThisSound.Checked;
            SelectedSound.OutputTarget = (byte)Combobox_OutputTarget.SelectedIndex;

            //--Change icon in the parent form--
            TreeNode[] nodeSearchResults = ((Frm_Soundbanks_Main)OpenForm).TreeView_File.Nodes.Find(TreeNodeSoundName, true);
            if (nodeSearchResults.Length > 0)
            {
                if (SelectedSound.OutputThisSound)
                {
                    if (nodeSearchResults[0].IsExpanded)
                    {
                        TreeNodeFunctions.TreeNodeSetNodeImage(nodeSearchResults[0], 3, 3);
                    }
                    else
                    {
                        TreeNodeFunctions.TreeNodeSetNodeImage(nodeSearchResults[0], 2, 2);
                    }
                }
                else
                {
                    if (nodeSearchResults[0].IsExpanded)
                    {
                        TreeNodeFunctions.TreeNodeSetNodeImage(nodeSearchResults[0], 6, 6);
                    }
                    else
                    {
                        TreeNodeFunctions.TreeNodeSetNodeImage(nodeSearchResults[0], 5, 5);
                    }
                }
            }

            //Update project status variable
            fileProperties.FileHasBeenModified = true;

            //Close current form
            Close();
        }

        private void Cbx_hashcode_Click(object sender, EventArgs e)
        {
            //Sound defines
            if (GenericFunctions.FileIsModified(GlobalPreferences.HT_SoundsMD5, GlobalPreferences.HT_SoundsPath))
            {
                Hashcodes.LoadSoundHashcodes(GlobalPreferences.HT_SoundsPath);
                if (DataToCombobox != null)
                {
                    DataToCombobox.Abort();
                }
                AddDataToCombobox();
            }

            //Sound Data defines
            if (GenericFunctions.FileIsModified(GlobalPreferences.HT_SoundsDataMD5, GlobalPreferences.HT_SoundsDataPath))
            {
                Hashcodes.LoadSoundDataFile(GlobalPreferences.HT_SoundsDataPath);
            }
        }

        private void Cbx_hashcode_SelectionChangeCommitted(object sender, EventArgs e)
        {
            uint keyToCheck = Convert.ToUInt32(cbx_hashcode.SelectedValue) & 0x00ffffff; //Apply bytes mask, example: 0x1A00005C -> 0x0000005C
            float[] SFXValues = GenericFunctions.GetSoundData(keyToCheck);

            if (SFXValues != null)
            {
                //[0] HashCode;    --USED--
                //[1] InnerRadius; --USED--
                //[2] OuterRadius; --USED--
                //[3] Altertness;
                //[4] Duration;
                //[5] Looping;
                //[6] Tracking3d; --USED--
                //[7] SampleStreamed;

                Textbox_InnerRadius.Text = SFXValues[1].ToString();
                Textbox_OuterRadius.Text = SFXValues[2].ToString();
                cbx_trackingtype.SelectedIndex = int.Parse(SFXValues[6].ToString());
            }
            else
            {
                Textbox_InnerRadius.Text = "0";
                Textbox_OuterRadius.Text = "0";
                cbx_trackingtype.SelectedIndex = 0;
                MessageBox.Show(GenericFunctions.resourcesManager.GetString("HashcodeNotFoundInSFXDataHashTable"), "EuroSound", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Textbox_flags_Click(object sender, EventArgs e)
        {
            using (EuroSound_FlagsForm formFlags = new EuroSound_FlagsForm(int.Parse(textbox_flags.Tag.ToString()), "SoundFlags", 16))
            {
                formFlags.Text = "Sound Flags";
                formFlags.Tag = Tag;
                formFlags.Owner = this;

                if (formFlags.ShowDialog() == DialogResult.OK)
                {
                    //Modify bool var
                    flagsModified = true;
                    SubSFXToggled = EXSoundbanksFunctions.SubSFXFlagChecked(formFlags.CheckedFlags);

                    //Save OriginalValue
                    textbox_flags.Tag = formFlags.CheckedFlags.ToString();
                    textbox_flags.Text = formFlags.CheckedFlagsString;
                }
            }
        }

        private void List_Samples_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (List_Samples.SelectedItems.Count > 0)
            {
                string sampleName = List_Samples.SelectedItems[0].Text;
                uint sampleID = (uint)List_Samples.SelectedItems[0].Tag;
                EXSample selectedSample = EXSoundbanksFunctions.ReturnSampleFromSound(SelectedSound, sampleID);

                if (selectedSample != null)
                {
                    if ((OriginalHasSubSFX == false && SubSFXToggled == true) || (OriginalHasSubSFX == true && SubSFXToggled == false && flagsModified == true))
                    {
                        MessageBox.Show("The \"hasSubSfx\" flag has been toggled; please reopen this dialog to switch between the sample and subSFX selection modes. Until then, you won't be able to change the SFX samples properly. You can also modify the sound samples by accessing them directly in the child node.", "EuroSound", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        if (SoundSection.Equals("Sounds"))
                        {
                            GenericFunctions.SetCurrentFileLabel(sampleName, "SBPanel_LastFile");
                            Frm_SampleProperties formSampleProps = new Frm_SampleProperties(selectedSample, fileProperties, EXSoundbanksFunctions.SubSFXFlagChecked(SelectedSound.Flags))
                            {
                                Text = GenericFunctions.TruncateLongString(sampleName, 25) + " - Properties",
                                Tag = Tag,
                                Owner = this,
                                ShowInTaskbar = false
                            };
                            formSampleProps.ShowDialog();
                            formSampleProps.Dispose();
                        }
                        else
                        {
                            //Open form only if file exists
                            if (File.Exists(GlobalPreferences.StreamFilePath))
                            {
                                GenericFunctions.SetCurrentFileLabel(sampleName, "SBPanel_LastFile");
                                using (Frm_NewStreamSound addStreamSound = new Frm_NewStreamSound(selectedSample, fileProperties))
                                {
                                    addStreamSound.Text = GenericFunctions.TruncateLongString(sampleName, 25) + " - Properties";
                                    addStreamSound.Tag = Tag;
                                    addStreamSound.Owner = this;
                                    addStreamSound.ShowInTaskbar = false;
                                    addStreamSound.ShowDialog();

                                    if (addStreamSound.DialogResult == DialogResult.OK)
                                    {
                                        selectedSample.FileRef = (short)addStreamSound.SelectedSound;
                                    }
                                };
                            }
                            else
                            {
                                DialogResult specifyOtherPathQuestion = MessageBox.Show("The stream sounds file has not found, the file route is: \"" + GlobalPreferences.StreamFilePath + "\", do you want to specify another path ?", "EuroSound", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                                if (specifyOtherPathQuestion == DialogResult.Yes)
                                {
                                    GlobalPreferences.StreamFilePath = BrowsersAndDialogs.FileBrowserDialog("EuroSound Files (*.esf)|*.esf", 0, true);
                                    WindowsRegistryFunctions.SaveExternalFiles("StreamFile", "Path", GlobalPreferences.StreamFilePath);
                                }
                            }
                        }
                    }
                }
            }
        }

        //*===============================================================================================
        //* FUNCTIONS
        //*===============================================================================================
        private void AddDataToCombobox()
        {
            //---AddDataToCombobox
            DataToCombobox = new Thread(() =>
            {
                cbx_hashcode.BeginInvoke((MethodInvoker)delegate
                {
                    cbx_hashcode.Enabled = false;
                    cbx_hashcode.DataSource = null;
                    cbx_hashcode.Items.Clear();
                    cbx_hashcode.DataSource = Hashcodes.SFX_Defines.OrderBy(o => o.Value).ToList();
                });
                cbx_hashcode.BeginInvoke((MethodInvoker)async delegate
                {
                    await Task.Delay(50);
                    cbx_hashcode.ValueMember = "Key";
                    cbx_hashcode.DisplayMember = "Value";
                    cbx_hashcode.SelectedValue = SelectedSound.Hashcode;
                    cbx_hashcode.Enabled = true;
                });
            })
            {
                IsBackground = true
            };
            DataToCombobox.Start();
        }
    }
}