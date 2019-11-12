﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net.Http;
using System.Net;
using System.Security.Permissions;
using Microsoft.VisualBasic;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Compressors;
using SharpCompress.IO;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Threading;
using System.Resources;
using Path = System.IO.Path;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBoxIcon = System.Windows.Forms.MessageBoxIcon;
using MessageBoxButtons = System.Windows.Forms.MessageBoxButtons;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Sonic3AIR_ModManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class ModManager : Window
    {
        #region Variables

        #region Winforms
        public System.Windows.Forms.NumericUpDown autoLaunchDelayUpDown;
        private System.Windows.Forms.Timer ApiInstallChecker;
        #endregion

        #region Tooltips

        ToolTip AddModTooltip = new ToolTip();
        ToolTip RemoveSelectedModTooltip = new ToolTip();
        ToolTip MoveModUpTooltip = new ToolTip();
        ToolTip MoveModDownTooltip = new ToolTip();
        ToolTip MoveModToTopTooltip = new ToolTip();
        ToolTip MoveModToBottomTooltip = new ToolTip();

        #endregion

        public string nL = Environment.NewLine;
        public static AIR_API.Settings S3AIRSettings;
        public static ModManager Instance;
        public static AIR_API.ActiveModsList S3AIRActiveMods;
        public static AIR_API.GameConfig GameConfig;
        public static AIR_API.VersionMetadata CurrentAIRVersion;
        IList<ModViewerItem> ModsList = new List<ModViewerItem>();
        bool AllowUpdate { get; set; } = true;


        #region Hosted Elements

        public System.Windows.Controls.ListView ModList { get => ModViewer.View; set => ModViewer.View = value; }
        public System.Windows.Controls.ListView VersionsListView { get => VersionsViewer.View; set => VersionsViewer.View = value; }
        public System.Windows.Controls.ListView GameRecordingList { get => RecordingsViewer.View; set => RecordingsViewer.View = value; }

        #endregion

        #endregion

        #region Initialize Methods
        public ModManager(bool autoBoot = false)
        {
            if (Properties.Settings.Default.AutoUpdates)
            {
                if (autoBoot == false && Program.UpdaterState == Updater.UpdateState.NeverStarted) new Updater();
            }


            StartModloader(autoBoot);

        }

        public ModManager(string gamebanana_api)
        {
            StartModloader(false, gamebanana_api);
        }


        public void SetNonDesignerRules()
        {
            LaunchOptionsWarning.Visibility = Visibility.Visible;
        }

        #region WPF Definitions
        private void InitializeHostedComponents()
        {
            ModViewer.View.SelectionChanged += View_SelectionChanged;
            ModViewer.View.MouseUp += View_MouseUp;

            autoLaunchDelayUpDown = new System.Windows.Forms.NumericUpDown();

            this.autoLaunchDelayUpDown.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Sonic3AIR_ModManager.Properties.Settings.Default, "AutoLaunchDelay", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.autoLaunchDelayUpDown.Location = new System.Drawing.Point(9, 94);
            this.autoLaunchDelayUpDown.Name = "autoLaunchDelayUpDown";
            this.autoLaunchDelayUpDown.Size = new System.Drawing.Size(56, 20);
            this.autoLaunchDelayUpDown.TabIndex = 13;
            this.autoLaunchDelayUpDown.Value = global::Sonic3AIR_ModManager.Properties.Settings.Default.AutoLaunchDelay;

            ApiInstallChecker = new System.Windows.Forms.Timer();
            ApiInstallChecker.Tick += apiInstallChecker_Tick;

            IntergerUpDownHost.Child = autoLaunchDelayUpDown;
        }

        #endregion

        private void StartModloader(bool autoBoot = false, string gamebanana_api = "")
        {
            AllowUpdate = false;
            InitializeComponent();
            InitializeHostedComponents();
            SetNonDesignerRules();
            AllowUpdate = true;

            if (ValidateInstall() == true)
            {
                SetTooltips();
                UpdateModsList(true);
                UpdateUI();
                Instance = this;


                if (Properties.Settings.Default.WindowSize != null)
                {
                    this.Width = Properties.Settings.Default.WindowSize.Width;
                    this.Height = Properties.Settings.Default.WindowSize.Height;
                }

                ApiInstallChecker.Enabled = true;
                ApiInstallChecker.Start();

                ModFileManagement.GBAPIWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
                ModFileManagement.GBAPIWatcher.EnableRaisingEvents = true;
                ModFileManagement.GBAPIWatcher.Changed += ModFileManagement.GBAPIWatcher_Changed;

                UserLanguage.ApplyLanguage(ref Instance);
                if (autoBoot) GameHandler.LaunchSonic3AIR();
                if (gamebanana_api != "") ModFileManagement.GamebananaAPI_Install(gamebanana_api);
            }
            else
            {
                Environment.Exit(0);
            }

        }

        #endregion

        #region Events

        private void apiInstallChecker_Tick(object sender, EventArgs e)
        {
            ModFileManagement.GBAPIInstallTrigger();
        }

        public static void UpdateUIFromInvoke()
        {
            Instance.UpdateModsList(true);
        }

        public void DownloadButtonTest_Click(object sender, RoutedEventArgs e)
        {
            ModFileManagement.AddModFromURLLink();
        }

        private void LanguageComboBox_SelectionChangeCommitted(object sender, SelectionChangedEventArgs e)
        {
            if (AllowUpdate) UpdateCurrentLanguage();
        }
        private void OpenDownloadsFolderToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenVersionsFolderToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void airModManagerPlacesButton_Click(object sender, RoutedEventArgs e)
        {
            airModManagerPlacesButton.ContextMenu.IsOpen = true;
        }

        private void OpenConfigFileToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenConfigFile();
        }

        private void FromSettingsFileToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeAIRPathFromSettings();
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (Program.UpdaterState == Updater.UpdateState.NeverStarted || Program.UpdaterState == Updater.UpdateState.Finished) new Updater(true);
        }

        private void SaveInputsButton_Click(object sender, RoutedEventArgs e)
        {
            GameConfig.Save();
        }

        private void ResetInputsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(Program.LanguageResource.GetString("ResetInputMappingsDefaultFormMessage"), Program.LanguageResource.GetString("ResetInputMappingsDefaultFormTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                if (GameConfig != null)
                {
                    GameConfig.ResetDevicesToDefault();
                    RefreshInputMappings();
                }
            }

        }

        private void InputMethodsList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInputMappings();
        }

        private void InputButton_Click(object sender, RoutedEventArgs e)
        {
            if (inputMethodsList.SelectedItem != null) ChangeInputMappings(sender);
        }

        private void AirPlacesButton_Click(object sender, RoutedEventArgs e)
        {
            airPlacesButton.ContextMenu.IsOpen = true;

        }

        private void AirMediaButton_Click(object sender, RoutedEventArgs e)
        {
            airMediaButton.ContextMenu.IsOpen = true;

        }
        private void MoveToTopButton_Click(object sender, RoutedEventArgs e)
        {
            MoveModToTop();
        }

        private void MoveToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            MoveModToBottom();
        }

        private void MoreModOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            moreModOptionsButton.ContextMenu.IsOpen = true;
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveModDown();
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveModUp();
        }

        private void ModStackRadioButtons_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (AllowUpdate)
            {
                AllowUpdate = false;
                if (enableModStackingToolStripMenuItem.IsChecked)
                {
                    Properties.Settings.Default.EnableNewLoaderMethod = true;
                }
                else
                {
                    Properties.Settings.Default.EnableNewLoaderMethod = false;
                }
                Properties.Settings.Default.Save();
                AllowUpdate = true;
                UpdateModsList(true);
            }
        }

        private void S3AIRWebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://sonic3air.org/");
        }

        private void GamebannaButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://gamebanana.com/games/6878");
        }

        private void EukaTwitterButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://twitter.com/eukaryot3k");
        }

        private void CarJemTwitterButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://twitter.com/carter5467_99");
        }

        private void OpenModdingTemplatesFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenModdingTemplatesFolder();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void OpenSampleModsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSampleModsFolder();

        }

        private void OpenUserManualButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUserManual();
        }

        private void OpenModDocumentationButton_Click(object sender, RoutedEventArgs e)
        {
            OpenModDocumentation();
        }

        private void OpenModURLToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenModURL((ModList.SelectedItem as ModViewerItem).Source.URL);
        }

        private void ShowLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenLogFile();
        }

        private void AutoRunCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
           if (AllowUpdate) UpdateUI();
        }

        private void ModManager_WindowClosing(object sender, CancelEventArgs e)
        {
            if (GameHandler.isGameRunning)
            {
                e.Cancel = true;
            }
            else
            {
                Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
                Properties.Settings.Default.Save();
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateInGameButtons();
        }

        private void DeleteRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null)
            {
                AIR_API.Recording recording = GameRecordingList.SelectedItem as AIR_API.Recording;
                if (MessageBox.Show(UserLanguage.DeleteItem(recording.Name), "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        File.Delete(recording.FilePath);
                    }
                    catch
                    {
                        MessageBox.Show(Program.LanguageResource.GetString("UnableToDeleteFile"), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    CollectGameRecordings();
                    GameRecordingList_SelectedIndexChanged(null, null);
                }
            }
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RemoveModToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ModList.SelectedItem != null)
            {
                ModFileManagement.RemoveMod((ModList.SelectedItem as ModViewerItem).Source);
            }
        }

        private void OpenModFolderToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ModList.SelectedItem != null)
            {
                OpenSelectedModFolder(ModList.SelectedItem as ModViewerItem);
            }
        }

        private void View_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void View_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RefreshSelectedModProperties();
        }

        private void AddMods_Click(object sender, RoutedEventArgs e)
        {
            ModFileManagement.AddMod();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModList.SelectedItem != null)
            {
                ModFileManagement.RemoveMod((ModList.SelectedItem as ModViewerItem).Source);
            }
        }
        private void TabControl1_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (tabControl1.SelectedItem == toolsPage)
                {
                    CollectGameRecordings();
                }
                else if (tabControl1.SelectedItem == optionsPage)
                {
                    RefreshInputMappings();
                }
            }
        }
        private void TabControl3_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (tabControl3.SelectedItem == recordingsPage)
                {
                    CollectGameRecordings();
                }
            }
        }

        private void CopyRecordingFilePath_Click(object sender, RoutedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null)
            {
                var item = GameRecordingList.SelectedItem as AIR_API.Recording;
                Clipboard.SetText(item.FilePath);
                MessageBox.Show(Program.LanguageResource.GetString("RecordingPathCopiedToClipboard"));
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null)
            {
                UploadRecordingToFileDotIO(GameRecordingList.SelectedItem as AIR_API.Recording);
            }

        }

        private void GameRecordingList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null)
            {
                openRecordingButton.IsEnabled = true;
                copyRecordingFilePath.IsEnabled = true;
                uploadButton.IsEnabled = true;
                deleteRecordingButton.IsEnabled = true;
            }
            else
            {
                openRecordingButton.IsEnabled = false;
                copyRecordingFilePath.IsEnabled = false;
                uploadButton.IsEnabled = false;
                deleteRecordingButton.IsEnabled = false;
            }
        }

        private void OpenRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            OpenRecordingLocation();
        }

        private void RefreshDebugButton_Click(object sender, RoutedEventArgs e)
        {
            CollectGameRecordings();
            GameRecordingList_SelectedIndexChanged(null, null);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateModsList(true);
        }

        private void UpdateSonic3AIRPath_Click(object sender, RoutedEventArgs e)
        {
            GameHandler.UpdateSonic3AIRLocation(true);
            UpdateAIRSettings();
        }

        private void ChangeSonic3AIRPathButton_Click(object sender, RoutedEventArgs e)
        {
            updateSonic3AIRPathButton.ContextMenu.IsOpen = true;
            UpdateAIRVersionsToolstrips();

        }

        private void ChangeRomPathButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeS3RomPath();
        }

        private void ModsList_ItemCheck()
        {
            UpdateModsList();
        }

        private void ModsList_SelectedValueChanged(object sender, EventArgs e)
        {
            RefreshSelectedModProperties();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
            GameHandler.LaunchSonic3AIR();
            UpdateInGameButtons();
        }

        private void OpenModsFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenModsFolder();
        }

        private void OpenEXEFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEXEFolder();
        }

        private void OpenAppDataFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAppDataFolder();
        }

        private void OpenConfigFile_Click(object sender, RoutedEventArgs e)
        {
            OpenSettingsFile();
        }

        private void ModsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshSelectedModProperties();
        }

        private void FixGlitchesCheckbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBoolSettings(S3AIRSetting.FixGlitches, fixGlitchesCheckbox.IsChecked.Value);
        }

        private void FailSafeModeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBoolSettings(S3AIRSetting.FailSafeMode, failSafeModeCheckbox.IsChecked.Value);
        }

        private void devModeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBoolSettings(S3AIRSetting.EnableDevMode, devModeCheckbox.IsChecked.Value);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            App.Instance.Shutdown();
        }

        private void OpenGamepadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchSystemGamepadSettings();
        }

        private void InputDeviceNamesList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInputDeviceNamesList();
        }

        private void AddDeviceNameButton_Click(object sender, RoutedEventArgs e)
        {
            AddInputDeviceName();
        }

        private void RemoveDeviceNameButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveInputDeviceName();
        }

        #endregion

        #region Refreshing and Updating

        public void SetTooltips()
        {
            AddModTooltip.Content = Program.LanguageResource.GetString("AddAMod");
            RemoveSelectedModTooltip.Content = Program.LanguageResource.GetString("RemoveSelectedMod");
            MoveModUpTooltip.Content = Program.LanguageResource.GetString("IncreaseModPriority");
            MoveModDownTooltip.Content = Program.LanguageResource.GetString("DecreaseModPriority");
            MoveModToTopTooltip.Content = Program.LanguageResource.GetString("IncreaseModPriorityToMax");
            MoveModToBottomTooltip.Content = Program.LanguageResource.GetString("DecreaseModPriorityToMin");

            removeButton.ToolTip = RemoveSelectedModTooltip;
            addMods.ToolTip = AddModTooltip;
            moveUpButton.ToolTip = MoveModUpTooltip;
            moveDownButton.ToolTip = MoveModDownTooltip;
            moveToTopButton.ToolTip = MoveModToTopTooltip;
            moveToBottomButton.ToolTip = MoveModToBottomTooltip;

            aboutLabel.Text = aboutLabel.Text.Replace("{version}", Program.Version);
            this.Title = this.Title.Replace("{version}", Program.Version);
        }

        public void UpdateInGameButtons()
        {
            bool enabled = !GameHandler.isGameRunning;
            saveAndLoadButton.IsEnabled = enabled;
            saveButton.IsEnabled = enabled;
            exitButton.IsEnabled = enabled;
            keepLoaderOpenCheckBox.IsEnabled = enabled;
            keepOpenOnQuitCheckBox.IsEnabled = enabled;
            sonic3AIRPathBox.IsEnabled = enabled;
            romPathBox.IsEnabled = enabled;
            fixGlitchesCheckbox.IsEnabled = enabled;
            failSafeModeCheckbox.IsEnabled = enabled;
            modPanel.IsEnabled = enabled;
            autoRunCheckbox.IsEnabled = enabled;
            inputPanel.IsEnabled = enabled;
            checkForUpdatesButton.IsEnabled = enabled;
            devModeCheckbox.IsEnabled = enabled;
            settingsTabControl.IsEnabled = enabled;
        }

        private void UpdateUI()
        {
            UpdateAIRSettings();
            UpdateModStackingToggle();
            autoLaunchDelayLabel.IsEnabled = Properties.Settings.Default.AutoLaunch;
            autoLaunchDelayUpDown.Enabled = Properties.Settings.Default.AutoLaunch;
        }

        private void UpdateModStackingToggle()
        {
            AllowUpdate = false;
            enableModStackingToolStripMenuItem.IsChecked = Properties.Settings.Default.EnableNewLoaderMethod;
            AllowUpdate = true;
        }

        private void ChangeS3RomPath()
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Filter = $"{Program.LanguageResource.GetString("Sonic3KRomFile")} (*.bin)|*.bin",
                InitialDirectory = Path.GetDirectoryName(S3AIRSettings.Sonic3KRomPath),
                Title = Program.LanguageResource.GetString("SelectSonic3KRomFile")

            };
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                S3AIRSettings.Sonic3KRomPath = fileDialog.FileName;
                S3AIRSettings.SaveSettings();
            }
            UpdateAIRSettings();
        }

        private enum S3AIRSetting : int
        {
            FailSafeMode = 0,
            FixGlitches = 1,
            EnableDevMode = 2
        }

        private void UpdateBoolSettings(S3AIRSetting setting, bool isChecked)
        {
            if (setting == S3AIRSetting.FailSafeMode)
            {
                S3AIRSettings.FailSafeMode = isChecked;
            }
            else if (setting == S3AIRSetting.FixGlitches)
            {
                S3AIRSettings.FixGlitches = isChecked;
            }
            else
            {
                S3AIRSettings.EnableDevMode = isChecked;
            }
            S3AIRSettings.SaveSettings();
        }

        private void UpdateCurrentLanguage()
        {
            if (languageComboBox.SelectedItem != null)
            {
                switch ((languageComboBox.SelectedItem as ComboBoxItem).Tag.ToString())
                {
                    case "EN_US":
                        UserLanguage.CurrentLanguage = UserLanguage.Language.EN_US;
                        break;
                    case "GR":
                        UserLanguage.CurrentLanguage = UserLanguage.Language.GR;
                        break;
                    case "FR":
                        UserLanguage.CurrentLanguage = UserLanguage.Language.FR;
                        break;
                    case "NULL":
                        UserLanguage.CurrentLanguage = UserLanguage.Language.NULL;
                        break;
                    default:
                        UserLanguage.CurrentLanguage = UserLanguage.Language.NULL;
                        break;
                }

                UserLanguage.ApplyLanguage(ref Instance);
                UpdateAIRSettings();
            }



        }

        private void GetLanguageSelection()
        {
            if (languageComboBox != null)
            {
                languageComboBox.Items.Clear();

                Binding LangItemsWidth = new Binding("ActualWidth") { ElementName = "languageComboBox" };

                AllowUpdate = false;

                ComboBoxItem EN_US = new ComboBoxItem()
                {
                    Tag = "EN_US",
                    Content = "English (US)",
                };
                ComboBoxItem GR = new ComboBoxItem()
                {
                    Tag = "GR",
                    Content = "Deutsch"
                };
                ComboBoxItem FR = new ComboBoxItem()
                {
                    Tag = "FR",
                    Content = "Français"
                };
                ComboBoxItem NULL = new ComboBoxItem()
                {
                    Tag = "NULL",
                    Content = "NULL"
                };

                EN_US.SetBinding(FrameworkElement.WidthProperty, LangItemsWidth);
                GR.SetBinding(FrameworkElement.WidthProperty, LangItemsWidth);
                FR.SetBinding(FrameworkElement.WidthProperty, LangItemsWidth);
                NULL.SetBinding(FrameworkElement.WidthProperty, LangItemsWidth);

                languageComboBox.Items.Add(EN_US);
                languageComboBox.Items.Add(GR);
                languageComboBox.Items.Add(FR);
                languageComboBox.Items.Add(NULL);

                if (UserLanguage.CurrentLanguage == UserLanguage.Language.EN_US) languageComboBox.SelectedItem = EN_US;
                else if (UserLanguage.CurrentLanguage == UserLanguage.Language.GR) languageComboBox.SelectedItem = GR;
                else if (UserLanguage.CurrentLanguage == UserLanguage.Language.FR) languageComboBox.SelectedItem = FR;
                else if (UserLanguage.CurrentLanguage == UserLanguage.Language.NULL) languageComboBox.SelectedItem = NULL;
                else languageComboBox.SelectedItem = NULL;

                AllowUpdate = true;
            }

        }

        private void UpdateAIRSettings()
        {
            sonic3AIRPathBox.Text = ProgramPaths.Sonic3AIRPath;
            if (S3AIRSettings != null)
            {
                romPathBox.Text = S3AIRSettings.Sonic3KRomPath;
                fixGlitchesCheckbox.IsChecked = S3AIRSettings.FixGlitches;
                failSafeModeCheckbox.IsChecked = S3AIRSettings.FailSafeMode;
                devModeCheckbox.IsChecked = S3AIRSettings.EnableDevMode;
                FullscreenTypeComboBox.SelectedIndex = S3AIRSettings.Fullscreen;
            }

            GetLanguageSelection();

            bool loaderMethodPast = Properties.Settings.Default.EnableNewLoaderMethod;

            if (File.Exists(ProgramPaths.Sonic3AIRPath))
            {
                string metaDataFile = Directory.GetFiles(Path.GetDirectoryName(ProgramPaths.Sonic3AIRPath), "metadata.json", SearchOption.AllDirectories).FirstOrDefault();
                if (metaDataFile != null)
                {
                    try
                    {
                        CurrentAIRVersion = new AIR_API.VersionMetadata(new FileInfo(metaDataFile));
                        if (CurrentAIRVersion.Version.CompareTo(new Version("19.09.0.0")) >= 0)
                        {
                            Properties.Settings.Default.EnableNewLoaderMethod = true;
                            enableModStackingToolStripMenuItem.IsEnabled = true;
                            airVersionLabel.Text = $"{Program.LanguageResource.GetString("AIRVersion")}: {CurrentAIRVersion.VersionString}";
                            airVersionLabel.Text += Environment.NewLine + $"{Program.LanguageResource.GetString("SettingsVersionLabel")}: {S3AIRSettings.Version.ToString()}";
                        }
                        else
                        {
                            Properties.Settings.Default.EnableNewLoaderMethod = false;
                            enableModStackingToolStripMenuItem.IsEnabled = false;
                            airVersionLabel.Text = $"{Program.LanguageResource.GetString("AIRVersion")}: {CurrentAIRVersion.VersionString}";
                            airVersionLabel.Text += Environment.NewLine + $"{Program.LanguageResource.GetString("SettingsVersionLabel")}: {S3AIRSettings.Version.ToString()}";
                        }
                    }
                    catch
                    {
                        NullSituation();

                    }

                }
                else
                {
                    NullSituation();
                }
            }
            else NullSituation();


            UpdateModStackingToggle();
            Properties.Settings.Default.Save();
            if (Properties.Settings.Default.EnableNewLoaderMethod != loaderMethodPast) UpdateModsList(true);

            void NullSituation()
            {
                Properties.Settings.Default.EnableNewLoaderMethod = false;
                enableModStackingToolStripMenuItem.IsEnabled = false;
                if (airVersionLabel != null)
                {
                    airVersionLabel.Text = $"{Program.LanguageResource.GetString("AIRVersion")}: NULL";
                    airVersionLabel.Text += Environment.NewLine + $"{Program.LanguageResource.GetString("SettingsVersionLabel")}: {S3AIRSettings.Version.ToString()}";
                }
            }

        }

        private void UpdateAIRGameConfigLaunchOptions()
        {
            if (SceneComboBox != null && PlayerComboBox != null && StartPhaseComboBox != null)
            {
                SceneComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;
                PlayerComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;
                StartPhaseComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;


                if (GameConfig != null)
                {
                    if ((SceneComboBox.SelectedItem as ComboBoxItem).Tag.ToString() != "NONE")
                    {
                        GameConfig.LoadLevel = (SceneComboBox.SelectedItem as ComboBoxItem).Tag.ToString();
                    }
                    else GameConfig.LoadLevel = null;
                    if ((PlayerComboBox.SelectedItem as ComboBoxItem).Tag.ToString() != "NONE")
                    {
                        if (int.TryParse((PlayerComboBox.SelectedItem as ComboBoxItem).Tag.ToString(), out int result))
                        {
                            GameConfig.UseCharacters = result;
                        }
                    }
                    else GameConfig.UseCharacters = null;
                    if ((StartPhaseComboBox.SelectedItem as ComboBoxItem).Tag.ToString() != "NONE")
                    {
                        if (int.TryParse((StartPhaseComboBox.SelectedItem as ComboBoxItem).Tag.ToString(), out int result))
                        {
                            GameConfig.StartPhase = result;
                        }
                    }
                    else GameConfig.StartPhase = null;

                    GameConfig.Save();
                }

                CollectGameConfig();
                if (GameConfig != null)
                {
                    if (GameConfig.LoadLevel != null) SceneComboBox.SelectedItem = SceneComboBox.Items.Cast<ComboBoxItem>().Where(x => x.Tag.ToString() == GameConfig.LoadLevel.ToString());
                    else SceneComboBox.SelectedIndex = 0;

                    if (GameConfig.UseCharacters != null) PlayerComboBox.SelectedItem = PlayerComboBox.Items.Cast<ComboBoxItem>().Where(x => x.Tag.ToString() == GameConfig.UseCharacters.ToString());
                    else PlayerComboBox.SelectedIndex = 0;

                    if (GameConfig.StartPhase != null) StartPhaseComboBox.SelectedItem = StartPhaseComboBox.Items.Cast<ComboBoxItem>().Where(x => x.Tag.ToString() == GameConfig.StartPhase.ToString());
                    else StartPhaseComboBox.SelectedIndex = 0;
                }

                if (GameConfig == null) AIRGameConfigNullSituation(2);

                SceneComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
                PlayerComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
                StartPhaseComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
            }






        }

        public void RefreshSelectedModProperties()
        {
            if (ModList.SelectedItem != null)
            {
                if (Properties.Settings.Default.EnableNewLoaderMethod)
                {
                    moveUpButton.IsEnabled = (ModsList.IndexOf((ModList.SelectedItem as ModViewerItem)) > 0);
                    moveDownButton.IsEnabled = (ModsList.IndexOf((ModList.SelectedItem as ModViewerItem)) < ModsList.Count - 1);
                    moveToTopButton.IsEnabled = (ModsList.IndexOf((ModList.SelectedItem as ModViewerItem)) > 0);
                    moveToBottomButton.IsEnabled = (ModsList.IndexOf((ModList.SelectedItem as ModViewerItem)) < ModsList.Count - 1);
                }
                else
                {
                    moveUpButton.IsEnabled = false;
                    moveDownButton.IsEnabled = false;
                    moveToTopButton.IsEnabled = false;
                    moveToBottomButton.IsEnabled = false;
                }
                removeButton.IsEnabled = true;
                removeModToolStripMenuItem.IsEnabled = true;
                openModFolderToolStripMenuItem.IsEnabled = true;
                openModURLToolStripMenuItem.IsEnabled = ((ModList.SelectedItem as ModViewerItem).Source.URL != null);
            }
            else
            {
                moveUpButton.IsEnabled = false;
                moveDownButton.IsEnabled = false;
                removeButton.IsEnabled = false;
                removeModToolStripMenuItem.IsEnabled = false;
                openModFolderToolStripMenuItem.IsEnabled = false;
                openModURLToolStripMenuItem.IsEnabled = false;
            }



            if (ModList.SelectedItem != null)
            {
                AIR_API.Mod item = (ModList.SelectedItem as ModViewerItem).Source;
                if (item != null)
                {

                    string author = $"{Program.LanguageResource.GetString("By")}: {item.Author}";
                    string version = $"{Program.LanguageResource.GetString("Version")}: {item.ModVersion}";
                    string air_version = $"{Program.LanguageResource.GetString("AIRVersion")}: {item.GameVersion}";
                    string tech_name = $"{item.TechnicalName}";

                    string description = item.Description;
                    if (description == "No Description Provided.")
                    {
                        description = Program.LanguageResource.GetString("NoModDescript");
                    }

                    Paragraph author_p = new Paragraph(new Run(author));
                    Paragraph version_p = new Paragraph(new Run(version));
                    Paragraph air_version_p = new Paragraph(new Run(air_version));
                    Paragraph tech_name_p = new Paragraph(new Run(tech_name));
                    Paragraph description_p = new Paragraph(new Run($"{nL}{description}"));


                    author_p.FontWeight = FontWeights.Normal;                    
                    version_p.FontWeight = FontWeights.Normal;
                    air_version_p.FontWeight = FontWeights.Normal;
                    tech_name_p.FontWeight = FontWeights.Bold;
                    description_p.FontWeight = FontWeights.Normal;

                    var no_margin = new Thickness(0);
                    author_p.Margin = no_margin;
                    version_p.Margin = no_margin;
                    air_version_p.Margin = no_margin;
                    tech_name_p.Margin = no_margin;
                    description_p.Margin = no_margin;

                    modInfoTextBox.Document.Blocks.Clear();

                    modInfoTextBox.Document.Blocks.Add(author_p);
                    modInfoTextBox.Document.Blocks.Add(version_p);
                    modInfoTextBox.Document.Blocks.Add(air_version_p);
                    modInfoTextBox.Document.Blocks.Add(tech_name_p);
                    modInfoTextBox.Document.Blocks.Add(description_p);
                }
                else
                {
                    modInfoTextBox.Document.Blocks.Clear();
                }
            }
            else
            {
                modInfoTextBox.Document.Blocks.Clear();
            }
        }
        #endregion

        #region Information Retriving

        private void CollectGameRecordings()
        {
            GameRecordingList.Items.Clear();
            if (File.Exists(ProgramPaths.Sonic3AIRPath))
            {
                recordingsErrorMessagePanel.Visibility = Visibility.Collapsed;

                string baseDirectory = Path.GetDirectoryName(ProgramPaths.Sonic3AIRPath);
                if (Directory.Exists(baseDirectory))
                {
                    Regex reg = new Regex(@"(gamerecording_)\d{6}(_)\d{6}");
                    DirectoryInfo directoryInfo = new DirectoryInfo(baseDirectory);
                    FileInfo[] fileInfo = directoryInfo.GetFiles("*.bin").Where(path => reg.IsMatch(path.Name)).ToArray();
                    foreach (var file in fileInfo)
                    {
                        AIR_API.Recording recording = new AIR_API.Recording(file);
                        GameRecordingList.Items.Add(recording);
                    }
                }
            }
            else
            {
                recordingsErrorMessagePanel.Visibility = Visibility.Visible;
            }

        }

        private void CollectInputMappings()
        {
            inputMethodsList.SelectionChanged -= InputMethodsList_SelectedIndexChanged;
            AIR_API.InputMappings.Device selectedItem = null;
            if (inputMethodsList.SelectedItem != null)
            {
                selectedItem = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
            }
            inputMethodsList.ItemsSource = null;
            inputMethodsList.Items.Refresh();
            if (GameConfig != null)
            {
                if (inputMethodsList.Items.Count != 0 && inputMethodsList.ItemsSource == null) inputMethodsList.Items.Clear();
                inputMethodsList.ItemsSource = GameConfig.InputDevices;
                if (selectedItem != null && inputMethodsList.Items.Contains(selectedItem)) inputMethodsList.SelectedItem = selectedItem;
            }
            else
            {
                CollectGameConfig();
                RecollectInputMappings();
            }
            inputMethodsList.SelectionChanged += InputMethodsList_SelectedIndexChanged;
        }

        private void RecollectInputMappings()
        {
            HideErrorGameConfigErrorPanels();

            if (ProgramPaths.Sonic3AIRPath != null && ProgramPaths.Sonic3AIRPath != "" && File.Exists(ProgramPaths.Sonic3AIRPath))
            {
                if (GameConfig != null)
                {
                    try
                    {
                        foreach (var inputMethod in GameConfig.InputDevices)
                        {
                            inputMethodsList.Items.Add(inputMethod);
                        }
                    }
                    catch
                    {
                        AIRGameConfigNullSituation(1);
                    }
                }
                else AIRGameConfigNullSituation(2);

            }
            else AIRGameConfigNullSituation();



        }

        private void HideErrorGameConfigErrorPanels()
        {
            inputPanel.IsEnabled = true;
            inputErrorMessage.Visibility = Visibility.Collapsed;

            LaunchOptionsFailureMessageBackground.Visibility = Visibility.Collapsed;
            airLaunchPanel.IsEnabled = true;
        }

        private void ShowGameConfigErrorPanels()
        {
            inputPanel.IsEnabled = false;
            inputErrorMessage.Visibility = Visibility.Visible;

            airLaunchPanel.IsEnabled = false;
            LaunchOptionsFailureMessageBackground.Visibility = Visibility.Visible;
        }


        private void CollectGameConfig()
        {
            HideErrorGameConfigErrorPanels();

            if (ProgramPaths.Sonic3AIRPath != null && ProgramPaths.Sonic3AIRPath != "" && File.Exists(ProgramPaths.Sonic3AIRPath))
            {
                string Sonic3AIREXEFolder = Path.GetDirectoryName(ProgramPaths.Sonic3AIRPath);
                FileInfo config = new FileInfo($"{Sonic3AIREXEFolder}//config.json");
                if (config.Exists)
                {
                    try
                    {
                        GameConfig = new AIR_API.GameConfig(config);
                    }
                    catch
                    {
                        AIRGameConfigNullSituation(1);
                    }

                }
                else AIRGameConfigNullSituation(2);
            }
            else AIRGameConfigNullSituation();

        }

        private void AIRGameConfigNullSituation(int situation = 0)
        {
            if (situation == 0) inputErrorMessage.Content = Program.LanguageResource.GetString("InputMappingError1");
            else if (situation == 1) inputErrorMessage.Content = Program.LanguageResource.GetString("InputMappingError2");
            else if (situation == 2) inputErrorMessage.Content = Program.LanguageResource.GetString("InputMappingError3");

            if (situation == 0) LaunchOptionsFailureMessage.Text = Program.LanguageResource.GetString("InputMappingError1");
            else if (situation == 1) LaunchOptionsFailureMessage.Text = Program.LanguageResource.GetString("InputMappingError2");
            else if (situation == 2) LaunchOptionsFailureMessage.Text = Program.LanguageResource.GetString("InputMappingError3");

            ShowGameConfigErrorPanels();
        }



        private bool ValidateInstall()
        {
            return ProgramPaths.ValidateInstall(ref S3AIRActiveMods, ref S3AIRSettings);
        }

        #endregion

        #region Input Mapping


        private void UpdateInputDeviceButtons()
        {
            if (inputMethodsList.SelectedItem != null)
            {
                removeInputMethodButton.IsEnabled = true;
                exportConfigButton.IsEnabled = true;
            }
            else
            {
                removeInputMethodButton.IsEnabled = false;
                exportConfigButton.IsEnabled = false;
            }
        }

        private void UpdateInputMappings()
        {
            UpdateInputDeviceButtons();
            inputDeviceNamesList.Items.Clear();
            if (GameConfig != null)
            {
                if (inputMethodsList.SelectedItem != null)
                {
                    if (inputMethodsList.SelectedItem is AIR_API.InputMappings.Device)
                    {
                        AIR_API.InputMappings.Device device = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
                        aInputButton.Content = (device.A.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.A.FirstOrDefault());
                        bInputButton.Content = (device.B.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.B.FirstOrDefault());
                        xInputButton.Content = (device.X.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.X.FirstOrDefault());
                        yInputButton.Content = (device.Y.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Y.FirstOrDefault());
                        upInputButton.Content = (device.Up.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Up.FirstOrDefault());
                        downInputButton.Content = (device.Down.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Down.FirstOrDefault());
                        leftInputButton.Content = (device.Left.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Left.FirstOrDefault());
                        rightInputButton.Content = (device.Right.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Right.FirstOrDefault());
                        startInputButton.Content = (device.Start.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Start.FirstOrDefault());
                        backInputButton.Content = (device.Back.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Back.FirstOrDefault());

                        if (aInputButton.Content == null) aInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (bInputButton.Content == null) bInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (xInputButton.Content == null) xInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (yInputButton.Content == null) yInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (upInputButton.Content == null) upInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (downInputButton.Content == null) downInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (leftInputButton.Content == null) leftInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (rightInputButton.Content == null) rightInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (startInputButton.Content == null) startInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (backInputButton.Content == null) backInputButton.Content = Program.LanguageResource.GetString("Input_NONE");

                        UpdateInputDeviceNamesList(true);



                    }
                }
                else
                {

                    DisableMappings();
                }
            }
        }

        private void ToggleDeviceNamesUI(bool enabled)
        {
            inputDeviceNamesList.IsEnabled = enabled;
            addDeviceNameButton.IsEnabled = enabled;
            removeDeviceNameButton.IsEnabled = (enabled == true ? inputDeviceNamesList.SelectedItem != null : enabled);
        }

        private void DisableMappings()
        {
            inputDeviceNamesList.Items.Clear();
            aInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            bInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            xInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            yInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            upInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            downInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            leftInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            rightInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            startInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            backInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            inputDeviceNamesList.Items.Add((Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL")));
        }

        private void ChangeInputMappings(object sender)
        {
            AIR_API.InputMappings.Device device = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;

            if (sender.Equals(aInputButton)) ChangeMappings(ref device, "A");
            else if (sender.Equals(bInputButton)) ChangeMappings(ref device, "B");
            else if (sender.Equals(xInputButton)) ChangeMappings(ref device, "X");
            else if (sender.Equals(yInputButton)) ChangeMappings(ref device, "Y");
            else if (sender.Equals(upInputButton)) ChangeMappings(ref device, "Up");
            else if (sender.Equals(downInputButton)) ChangeMappings(ref device, "Down");
            else if (sender.Equals(leftInputButton)) ChangeMappings(ref device, "Left");
            else if (sender.Equals(rightInputButton)) ChangeMappings(ref device, "Right");
            else if (sender.Equals(startInputButton)) ChangeMappings(ref device, "Start");
            else if (sender.Equals(backInputButton)) ChangeMappings(ref device, "Back");

            void ChangeMappings(ref AIR_API.InputMappings.Device button, string input)
            {
                switch (input)
                {
                    case "A":
                        MappingDialog(ref button.A);
                        break;
                    case "B":
                        MappingDialog(ref button.B);
                        break;
                    case "X":
                        MappingDialog(ref button.X);
                        break;
                    case "Y":
                        MappingDialog(ref button.Y);
                        break;
                    case "Up":
                        MappingDialog(ref button.Up);
                        break;
                    case "Down":
                        MappingDialog(ref button.Down);
                        break;
                    case "Left":
                        MappingDialog(ref button.Left);
                        break;
                    case "Right":
                        MappingDialog(ref button.Right);
                        break;
                    case "Start":
                        MappingDialog(ref button.Start);
                        break;
                    case "Back":
                        MappingDialog(ref button.Back);
                        break;
                }
                UpdateInputMappings();

                void MappingDialog(ref List<string> mappings)
                {
                    var mD = new KeybindingsListDialogV2(mappings);
                    mD.ShowDialog();
                }

            }
        }

        private void AddInputDeviceName()
        {
            if (inputMethodsList.SelectedItem != null)
            {
                int index = inputMethodsList.SelectedIndex;
                string newDevice = "New Device";
                DeviceNameDialogV2 deviceNameDialog = new DeviceNameDialogV2();
                bool? result = deviceNameDialog.ShowDeviceNameDialog(ref newDevice, Program.LanguageResource.GetString("AddNewDeviceTitle"), Program.LanguageResource.GetString("AddNewDeviceDescription"));
                if (result == true)
                {
                    GameConfig.InputDevices[inputMethodsList.SelectedIndex].DeviceNames.Add(newDevice);
                    UpdateInputMappings();
                }


            }

        }

        private void AddInputDevice()
        {
            string new_name = "NewController";
            bool finished = false;
            char[] acceptable_char = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_1234567890".ToArray();


            while (!finished)
            {
                DialogResult result = ExtraDialog.ShowInputDialog(ref new_name, "Add a Input Device...", "Input Device Name:");
                bool containsKey = GameConfig.Devices.ContainsKey(new_name);
                bool unacceptable_char = new_name.ContainsOnly(acceptable_char);
                if (result != System.Windows.Forms.DialogResult.Cancel && !containsKey && unacceptable_char)
                {
                    finished = true;
                    GameConfig.Devices.Add(new_name, new AIR_API.InputMappings.Device(new_name));
                    RefreshInputMappings();
                }
                else if (result != System.Windows.Forms.DialogResult.Cancel)
                {
                    if (containsKey)
                    {
                        MessageBox.Show(string.Format("\"{0}\" already exists in the directory, pick another name!", new_name));
                    }
                    else
                    {
                        MessageBox.Show(string.Format("\"{0}\" uses unacceptable characters, please try to use only underscrores and numeranic/alphabettical characters.", new_name));
                    }

                }
                else
                {
                    finished = true;
                }
            }



        }

        private void RemoveInputDevice()
        {
            if (inputMethodsList.SelectedItem != null)
            {
                if (inputMethodsList.SelectedItem is AIR_API.InputMappings.Device)
                {

                    var deviceToRemove = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
                    DialogResult result = MessageBox.Show(UserLanguage.RemoveInputDevice(deviceToRemove.EntryName), Program.LanguageResource.GetString("DeleteDeviceTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        GameConfig.Devices.Remove(deviceToRemove.EntryName);
                        RefreshInputMappings();
                    }
                }
            }
        }

        private void RefreshInputMappings()
        {
            DisableMappings();
            CollectInputMappings();
            UpdateInputDeviceButtons();
        }

        private void ImportInputDevice()
        {
            if (GameConfig != null)
            {
                ModFileManagement.ImportInputMappings(GameConfig);
                RefreshInputMappings();
            }
        }

        private void ExportInputDevice()
        {
            if (inputMethodsList.SelectedItem != null)
            {
                if (inputMethodsList.SelectedItem is AIR_API.InputMappings.Device)
                {
                    AIR_API.InputMappings.Device device = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
                    ModFileManagement.ExportInputMappings(device);
                }
            }
        }

        private void RemoveInputDeviceName()
        {
            if (inputMethodsList.SelectedItem != null && inputDeviceNamesList.SelectedItem != null)
            {
                DialogResult result = MessageBox.Show(UserLanguage.RemoveInputDevice(inputDeviceNamesList.SelectedItem.ToString()), Program.LanguageResource.GetString("DeleteDeviceTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    int index = inputDeviceNamesList.SelectedIndex;
                    GameConfig.InputDevices[inputMethodsList.SelectedIndex].DeviceNames.RemoveAt(index);
                    UpdateInputMappings();
                }
            }
        }

        private void UpdateInputDeviceNamesList(bool refreshItems = false)
        {
            if (GameConfig != null)
            {
                if (inputMethodsList.SelectedItem != null)
                {
                    if (inputMethodsList.SelectedItem is AIR_API.InputMappings.Device)
                    {
                        AIR_API.InputMappings.Device device = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
                        if (device.HasDeviceNames)
                        {
                            if (refreshItems)
                            {
                                foreach (var name in device.DeviceNames)
                                {
                                    inputDeviceNamesList.Items.Add(name);
                                }
                            }
                            ToggleDeviceNamesUI(true);
                        }
                        else
                        {
                            inputDeviceNamesList.Items.Add((Program.LanguageResource.GetString("Input_UNSUPPORTED") == null ? "" : Program.LanguageResource.GetString("Input_UNSUPPORTED")));
                            ToggleDeviceNamesUI(false);
                        }
                    }
                }
            }
        }

        private void LaunchSystemGamepadSettings()
        {
            Process.Start("joy.cpl");

        }

        #endregion

        #region Legacy Mod Management

        private void UpdateModsListLegacy(bool FullReload = false)
        {
            if (FullReload) FetchModsLegacy();
            RefreshSelectedModProperties();
        }

        private void SaveLegacy()
        {
            foreach (var mod in ModsList)
            {
                UpdateMods(mod.Source);
            }
            UpdateModsList(true);

            void UpdateMods(AIR_API.Mod item)
            {
                if (item.IsEnabled != item.EnabledLocal)
                {
                    if (item.IsEnabled == true) EnableModLegacy(item);
                    else DisableModLegacy(item);
                }
            }
        }

        private void FetchModsLegacy()
        {
            ModsList.Clear();
            ModsList = new List<ModViewerItem>();
            GetModsCheckStateLegacy();
            UpdateNewModsListItems();

        }

        private void GetModsCheckStateLegacy()
        {
            DirectoryInfo d = new DirectoryInfo(ProgramPaths.Sonic3AIRModsFolder);
            DirectoryInfo[] folders = d.GetDirectories();
            foreach (DirectoryInfo folder in folders)
            {
                DirectoryInfo f = new DirectoryInfo(folder.FullName);
                var root = f.GetFiles("mod.json").FirstOrDefault();
                AIR_API.Mod mod;
                if (root != null)
                {
                    try
                    {
                        mod = new AIR_API.Mod(root);
                        if (mod != null)
                        {
                            if (folder.Name.Contains("#"))
                            {
                                mod.IsEnabled = false;
                                mod.EnabledLocal = false;
                                ModsList.Add(new ModViewerItem(mod));
                            }
                            else
                            {
                                mod.IsEnabled = true;
                                mod.EnabledLocal = true;
                                ModsList.Add(new ModViewerItem(mod));
                            }
                        }
                    }
                    catch (Newtonsoft.Json.JsonReaderException ex)
                    {
                        MessageBox.Show(UserLanguage.LegacyModError1(folder.Name, ex.Message));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(UserLanguage.LegacyModError2(folder.Name, ex.Message));
                    }


                }



            }
        }

        private void DisableModLegacy(AIR_API.Mod mod)
        {
            try
            {
                string result = ProgramPaths.Sonic3AIRModsFolder + "\\" + "#" + mod.FolderName.Replace("#", "");
                Directory.Move(mod.FolderPath, result);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + Program.LanguageResource.GetString("PleaseRefreshTheModList"));
            }
        }

        private void EnableModLegacy(AIR_API.Mod mod)
        {
            try
            {
                string result = ProgramPaths.Sonic3AIRModsFolder + "\\" + mod.FolderName.Replace("#", "");
                Directory.Move(mod.FolderPath, result);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + Program.LanguageResource.GetString("PleaseRefreshTheModList"));
            }
        }

        #endregion

        #region Modern Mod Management

        private void UpdateModsList(bool FullReload = false)
        {
            if (File.Exists(ProgramPaths.Sonic3AIRPath))
            {
                modErrorTextPanel.Visibility = Visibility.Collapsed;
                ModViewer.ItemCheck = null;
                if (!Properties.Settings.Default.EnableNewLoaderMethod) UpdateModsListLegacy(FullReload);
                else
                {

                    if (FullReload) FetchMods();
                    else
                    {
                        UpdateNewModsListItems();
                    }
                    RefreshSelectedModProperties();
                }
                ModViewer.ItemCheck = ModsList_ItemCheck;

            }
            else
            {
                modErrorTextPanel.Visibility = Visibility.Visible;
            }

        }

        private void UpdateNewModsListItems()
        {
            ProgramPaths.ValidateSettingsAndActiveMods(ref S3AIRActiveMods, ref S3AIRSettings);
            ModList.Items.Clear();
            foreach (ModViewerItem mod in ModsList)
            {
                ModList.Items.Add(mod);
            }
            ModList.Items.Refresh();

        }

        private void FetchMods()
        {
            ModsList.Clear();
            ModsList = new List<ModViewerItem>();
            EnableAllLegacyDisabledMods();
            GetModsCheckState();
            UpdateNewModsListItems();
        }

        private void GetModsCheckState()
        {
            DirectoryInfo d = new DirectoryInfo(ProgramPaths.Sonic3AIRModsFolder);
            DirectoryInfo[] folders = d.GetDirectories();
            IList<Tuple<AIR_API.Mod, int>> ActiveMods = new List<Tuple<AIR_API.Mod, int>>();
            foreach (DirectoryInfo folder in folders)
            {
                DirectoryInfo f = new DirectoryInfo(folder.FullName);
                var root = f.GetFiles("mod.json").FirstOrDefault();
                AIR_API.Mod mod;
                if (root != null)
                {
                    try
                    {
                        mod = new AIR_API.Mod(root);
                        if (S3AIRActiveMods.ActiveMods.Contains(mod.FolderName))
                        {
                            mod.IsEnabled = true;
                            mod.EnabledLocal = true;
                            ActiveMods.Add(new Tuple<AIR_API.Mod, int>(mod, S3AIRActiveMods.ActiveMods.IndexOf(mod.FolderName)));
                        }
                        else
                        {
                            mod.IsEnabled = false;
                            mod.EnabledLocal = false;
                            ModsList.Add(new ModViewerItem(mod));
                        }
                    }
                    catch (Newtonsoft.Json.JsonReaderException ex)
                    {
                        MessageBox.Show(UserLanguage.LegacyModError1(folder.Name, ex.Message));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(UserLanguage.LegacyModError2(folder.Name, ex.Message));
                    }


                }
            }
            foreach (var enabledMod in ActiveMods.OrderBy(x => x.Item2).ToList())
            {
                ModsList.Insert(0, new ModViewerItem(enabledMod.Item1));
            }

        }

        private void MoveModToTop()
        {
            int index = ModList.SelectedIndex;
            if (index != 0)
            {
                ModsList.Move(index, 0);
                UpdateModsList();
                ModList.SelectedIndex = 0;
            }
        }

        private void MoveModUp()
        {
            int index = ModList.SelectedIndex;
            if (index != 0)
            {
                ModsList.Move(index, index - 1);
                UpdateModsList();
                ModList.SelectedIndex = index - 1;
            }
        }
        private void MoveModDown()
        {
            int index = ModList.SelectedIndex;
            if (index != ModsList.Count - 1)
            {
                ModsList.Move(index, index + 1);
                UpdateModsList();
                ModList.SelectedIndex = index + 1;
            }
        }

        private void MoveModToBottom()
        {
            int index = ModList.SelectedIndex;
            if (index != ModsList.Count - 1)
            {
                ModsList.Move(index, ModsList.Count - 1);
                UpdateModsList();
                ModList.SelectedIndex = ModsList.Count - 1;
            }
        }

        private void Save()
        {
            if (!Properties.Settings.Default.EnableNewLoaderMethod) SaveLegacy();
            else
            {
                foreach (var mod in ModsList)
                {
                    UpdateMods(mod.Source);
                }
                S3AIRActiveMods.Save(ModsList.Where(x => x.IsEnabled).Select(x => x.Source.FolderName).Reverse().ToList());
                UpdateModsList(true);

                void UpdateMods(AIR_API.Mod item)
                {
                    if (item.IsEnabled != item.EnabledLocal)
                    {
                        if (item.IsEnabled == true) EnableMod(item);
                        else DisableMod(item);
                    }
                }
            }
        }

        private void DisableMod(AIR_API.Mod mod)
        {
            S3AIRActiveMods.ActiveMods.Remove(mod.FolderName);
        }

        private void EnableMod(AIR_API.Mod mod)
        {
            S3AIRActiveMods.ActiveMods.Add(mod.FolderName);
        }

        private void EnableAllLegacyDisabledMods()
        {
            DirectoryInfo d = new DirectoryInfo(ProgramPaths.Sonic3AIRModsFolder);
            DirectoryInfo[] folders = d.GetDirectories();
            List<string> DisabledFolders = new List<string>();
            foreach (DirectoryInfo folder in folders)
            {
                DirectoryInfo f = new DirectoryInfo(folder.FullName);
                var root = f.GetFiles("mod.json").FirstOrDefault();
                if (root != null)
                {
                    if (folder.Name.Contains("#")) DisabledFolders.Add(folder.Name);
                }
            }

            foreach (string folder in DisabledFolders)
            {
                string destination = ProgramPaths.Sonic3AIRModsFolder + "\\" + folder.Replace("#", "");
                string source = ProgramPaths.Sonic3AIRModsFolder + "\\" + folder;
                Directory.Move(source, destination);
            }
        }


        #endregion

        #region Launching Events

        private void AddRemoveURLHandlerButton_Click(object sender, RoutedEventArgs e)
        {
            string ModLoaderPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string InstallerPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "//GameBanana API Installer.exe";
            Process.Start($"\"{InstallerPath}\"", $"\"{ModLoaderPath}\"");
        }

        private void OpenEXEFolder()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                string filename = ProgramPaths.Sonic3AIRPath;
                Process.Start(Path.GetDirectoryName(filename));
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    string filename = ProgramPaths.Sonic3AIRPath;
                    Process.Start(Path.GetDirectoryName(filename));
                }
            }
        }

        private void OpenAppDataFolder()
        {
            Process.Start(ProgramPaths.Sonic3AIRAppDataFolder);
        }

        private void OpenModsFolder()
        {
            Process.Start(ProgramPaths.Sonic3AIRModsFolder);
        }

        private void OpenSelectedModFolder(ModViewerItem mod)
        {
            Process.Start(mod.Source.FolderPath);
        }

        private void OpenConfigFile()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                string filename = ProgramPaths.Sonic3AIRPath;
                if (File.Exists(Path.GetDirectoryName(filename) + "//config.json"))
                {
                    Process.Start(Path.GetDirectoryName(filename) + "//config.json");
                }
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    string filename = ProgramPaths.Sonic3AIRPath;
                    if (File.Exists(Path.GetDirectoryName(filename) + "//config.json"))
                    {
                        Process.Start(Path.GetDirectoryName(filename) + "//config.json");
                    }
                }
            }
        }

        private void OpenLogFile()
        {
            if (File.Exists(ProgramPaths.Sonic3AIRAppDataFolder + "//logfile.txt"))
            {
                Process.Start(ProgramPaths.Sonic3AIRAppDataFolder + "//logfile.txt");
            }
            else
            {
                MessageBox.Show($"{Program.LanguageResource.GetString("LogFileNotFound")}: {nL}{ProgramPaths.Sonic3AIRAppDataFolder}\\logfile.txt");
            }

        }

        private void OpenModdingTemplatesFolder()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                if (ProgramPaths.ValidateSonic3AIRModdingTemplatesFolderPath()) Process.Start(ProgramPaths.Sonic3AIRModdingTemplatesFolder);
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    if (ProgramPaths.ValidateSonic3AIRModdingTemplatesFolderPath()) Process.Start(ProgramPaths.Sonic3AIRModdingTemplatesFolder);
                }
            }
        }

        private void OpenSampleModsFolder()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                if (ProgramPaths.ValidateSonic3AIRSampleModsFolderPath()) Process.Start(ProgramPaths.Sonic3AIRSampleModsFolder);
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    if (ProgramPaths.ValidateSonic3AIRSampleModsFolderPath()) Process.Start(ProgramPaths.Sonic3AIRSampleModsFolder);
                }
            }
        }

        private void OpenUserManual()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                if (ProgramPaths.ValidateSonic3AIRUserManualFilePath()) Process.Start(ProgramPaths.Sonic3AIRUserManualFile);
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    if (ProgramPaths.ValidateSonic3AIRUserManualFilePath()) Process.Start(ProgramPaths.Sonic3AIRUserManualFile);
                }
            }
        }

        private void OpenModDocumentation()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                if (ProgramPaths.ValidateSonic3AIRModDocumentationFilePath()) Process.Start(ProgramPaths.Sonic3AIRModDocumentationFile);
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    if (ProgramPaths.ValidateSonic3AIRModDocumentationFilePath()) Process.Start(ProgramPaths.Sonic3AIRModDocumentationFile);
                }
            }
        }

        private void OpenModURL(string url)
        {
            Process.Start(url);
        }

        private void OpenSettingsFile()
        {
            Process.Start(ProgramPaths.Sonic3AIRAppDataFolder + "//settings.json");
        }

        private void OpenRecordingLocation()
        {
            if (GameRecordingList.SelectedItem != null)
            {
                AIR_API.Recording item = GameRecordingList.SelectedItem as AIR_API.Recording;
                if (File.Exists(item.FilePath))
                {
                    Process.Start("explorer.exe", "/select, " + item.FilePath);
                }
            }

        }

        #endregion

        #region Information Sending

        private async void UploadRecordingToFileDotIO(AIR_API.Recording recording)
        {
            string expires = "/?expires=1w";
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://file.io" + expires))
                {
                    var multipartContent = new MultipartFormDataContent();
                    multipartContent.Add(new ByteArrayContent(File.ReadAllBytes(recording.FilePath)), "file", Path.GetFileName(recording.FilePath));
                    request.Content = multipartContent;

                    var response = await httpClient.SendAsync(request);
                    string result = await response.Content.ReadAsStringAsync();
                    dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result);
                    string url = jsonObj.link;

                    string message = UserLanguage.RecordingUploaded(url);
                    Clipboard.SetText(url);
                    MessageBox.Show(message);

                }
            }
        }

        #endregion

        #region Protocol Handler

        public void CreateGameBananaShortcut()
        {
            ProgramPaths.Sonic3AIRGBLinkPath = ProgramPaths.Sonic3AIRAppDataFolder + "//AIRModLoader.lnk";
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut = shell.CreateShortcut(ProgramPaths.Sonic3AIRGBLinkPath) as IWshRuntimeLibrary.IWshShortcut;
            shortcut.TargetPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            shortcut.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            shortcut.Save();
        }




        #endregion

        #region AIR EXE Version Handler Toolstrip / Path Management

        private void AIRVersionZIPToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            InstallVersionFromZIP();
        }

        private void UpdateAIRVersionsToolstrips()
        {
            CleanUpInstalledVersionsToolStrip();
            if (Directory.Exists(ProgramPaths.Sonic3AIR_MM_VersionsFolder))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(ProgramPaths.Sonic3AIR_MM_VersionsFolder);
                var folders = directoryInfo.GetDirectories().ToList();
                if (folders.Count != 0)
                {
                    foreach (var folder in folders.VersionSort().Reverse())
                    {
                        string filePath = Path.Combine(folder.FullName, "sonic3air_game", "Sonic3AIR.exe");
                        if (File.Exists(filePath))
                        {
                            installedVersionsToolStripMenuItem.Items.Add(GenerateInstalledVersionsToolstripItem(folder.Name, filePath));
                        }


                    }
                }

            }

        }

        private void CleanUpInstalledVersionsToolStrip()
        {
            foreach (var item in installedVersionsToolStripMenuItem.Items.Cast<MenuItem>())
            {
                item.Click -= ChangeAIRPathByInstalls;
            }
            installedVersionsToolStripMenuItem.Items.Clear();
        }

        private MenuItem GenerateInstalledVersionsToolstripItem(string name, string filepath)
        {
            MenuItem item = new MenuItem();
            item.Header = name;
            item.Tag = filepath;
            item.Click += ChangeAIRPathByInstalls;
            return item;
        }

        private void ChangeAIRPathByInstalls(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            ProgramPaths.Sonic3AIRPath = item.Tag.ToString();
            Properties.Settings.Default.Save();
            UpdateAIRSettings();
        }

        private void ChangeAIRPathFromSettings()
        {
            if (S3AIRSettings != null)
            {
                if (S3AIRSettings.HasEXEPath)
                {
                    if (File.Exists(S3AIRSettings.AIREXEPath))
                    {
                        ProgramPaths.Sonic3AIRPath = S3AIRSettings.AIREXEPath;
                        Properties.Settings.Default.Save();
                        UpdateAIRSettings();
                    }
                    else
                    {
                        MessageBox.Show(Program.LanguageResource.GetString("AIRChangePathNoLongerExists"));
                    }
                }
            }
        }

        private void InstallVersionFromZIP()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = $"{Program.LanguageResource.GetString("SonicAIRVersionZIP")} (*.zip)|*.zip",
                Title = Program.LanguageResource.GetString("SelectSonicAIRVersionZIP")
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string destination = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Sonic3AIR_MM\\downloads";
                string output = destination;

                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(ofd.FileName))
                {
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(output, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }


                string metaDataFile = Directory.GetFiles(destination, "metadata.json", SearchOption.AllDirectories).FirstOrDefault();
                AIR_API.VersionMetadata ver = new AIR_API.VersionMetadata(new FileInfo(metaDataFile));


                string output2 = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Sonic3AIR_MM\\air_versions\\{ver.VersionString}";

                Directory.Move(destination, output2);

                Directory.CreateDirectory(destination);

                MessageBox.Show(UserLanguage.VersionInstalled(output2));
            }


        }



        #endregion

        #region A.I.R. Version Manager List

        private void RefreshVersionsList(bool fullRefresh = false)
        {
            if (fullRefresh)
            {
                VersionsListView.Items.Clear();
                DirectoryInfo directoryInfo = new DirectoryInfo(ProgramPaths.Sonic3AIR_MM_VersionsFolder);
                var folders = directoryInfo.GetDirectories().ToList();
                if (folders.Count != 0)
                {
                    foreach (var folder in folders.VersionSort().Reverse())
                    {
                        string filePath = Path.Combine(folder.FullName, "sonic3air_game", "Sonic3AIR.exe");
                        if (File.Exists(filePath))
                        {
                            VersionsListView.Items.Add(new AIRVersionListItem(folder.Name, folder.FullName));
                        }


                    }
                }
            }

            bool enabled = VersionsListView.SelectedItem != null;
            removeVersionButton.IsEnabled = enabled;
            openVersionLocationButton.IsEnabled = enabled;
        }

        private class AIRVersionListItem
        {
            public string Name { get { return _name; } }
            private string _name;

            public string FilePath { get { return _filePath; } }
            private string _filePath;

            public override string ToString()
            {
                return $"{Program.LanguageResource.GetString("Version")} {Name}";
            }

            public AIRVersionListItem(string name, string filePath)
            {
                _name = name;
                _filePath = filePath;
            }
        }

        private void VersionsListBox_SelectedValueChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshVersionsList();
        }

        private void TabControl2_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (settingsTabControl.SelectedItem == versionsPage)
                {
                    RefreshVersionsList(true);
                }
                else if (settingsTabControl.SelectedItem == gameOptionsPage || settingsTabControl.SelectedItem == inputPage)
                {
                    CollectGameConfig();
                    if (settingsTabControl.SelectedItem == inputPage)
                    {
                        RefreshInputMappings();
                    }
                }
            }
        }

        private void OpenVersionLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (VersionsListView.SelectedItem != null && VersionsListView.SelectedItem is AIRVersionListItem)
            {
                AIRVersionListItem item = VersionsListView.SelectedItem as AIRVersionListItem;
                Process.Start(item.FilePath);
            }
        }

        private void RemoveVersionButton_Click(object sender, RoutedEventArgs e)
        {
            if (VersionsListView.SelectedItem != null && VersionsListView.SelectedItem is AIRVersionListItem)
            {
                AIRVersionListItem item = VersionsListView.SelectedItem as AIRVersionListItem;
                if (MessageBox.Show(UserLanguage.RemoveVersion(item.Name), "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        ModFileManagement.WipeFolderContents(item.FilePath);
                        Directory.Delete(item.FilePath);
                    }
                    catch
                    {
                        MessageBox.Show(Program.LanguageResource.GetString("UnableToRemoveVersion"), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    RefreshVersionsList(true);
                }

            }
        }




        #endregion

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void addInputMethodButton_Click(object sender, RoutedEventArgs e)
        {
            AddInputDevice();
        }

        private void removeInputMethodButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveInputDevice();
        }

        private void importConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ImportInputDevice();
        }

        private void exportConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ExportInputDevice();
        }

        private void useDarkModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.UseDarkTheme)
            {
                App.ChangeSkin(Skin.Dark);
            }
            else
            {
                App.ChangeSkin(Skin.Light);
            }
            RefreshTheming();


            void RefreshTheming()
            {
                this.InvalidateVisual();
                foreach (UIElement element in Extensions.FindVisualChildren<UIElement>(this))
                {
                    element.InvalidateVisual();
                }
            }
        }

        private void LaunchOptionsUnderstandingButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchOptionsWarning.Visibility = Visibility.Collapsed;
        }

        private void LaunchOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded) UpdateAIRGameConfigLaunchOptions();
        }

        private void CurrentWindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (GameConfig != null)
                {
                    S3AIRSettings.Fullscreen = FullscreenTypeComboBox.SelectedIndex;
                    S3AIRSettings.SaveSettings();
                    UpdateAIRSettings();
                }

            }
        }

        private void sonic3AIRPathBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            ProgramPaths.Sonic3AIRPath = sonic3AIRPathBox.Text;
            // your event handler here
            e.Handled = true;
            UpdateAIRSettings();
        }
    }
}
