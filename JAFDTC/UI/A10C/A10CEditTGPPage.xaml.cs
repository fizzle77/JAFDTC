using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.TGP;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 TGP editor.
    /// </summary>
    public sealed partial class A10CEditTGPPage : Page
    {
        private ConfigEditorPageNavArgs _navArgs;
        private A10CConfiguration _config;

        // For configuration linking UI.
        private readonly Dictionary<string, string> _configNameToUID = new Dictionary<string, string>();
        private readonly List<string> _configNameList = new List<string>();

        private bool _isUIUpdatePending = false;
        private TGPSystem _editState;

        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        public static ConfigEditorPageInfo PageInfo
            => new(TGPSystem.SystemTag, "TGP", "TGP", Glyphs.TGP, typeof(A10CEditTGPPage));

        public A10CEditTGPPage()
        {
            InitializeComponent();

            _editState = new TGPSystem();
            _editState.ErrorsChanged += BaseField_DataValidationError;
            _editState.PropertyChanged += BaseField_PropertyChanged;

            _defaultBorderBrush = uiTextLaserCode.BorderBrush;
            _defaultBkgndBrush = uiTextLaserCode.Background;
        }

        // ---- UI helpers  -------------------------------------------------------------------------------------------

        private void CopyConfigToEditState()
        {
            CopyBetweenStates(_config.TGP, _editState);
            UpdateUIFromEditState();
        }

        private void SaveEditStateToConfig()
        {
            CopyBetweenStates(_editState, _config.TGP);
            _config.Save(this, TGPSystem.SystemTag);
        }

        private void CopyBetweenStates(TGPSystem source, TGPSystem dest)
        {
            dest.LaserCode = source.LaserCode;
            dest.LSS = source.LSS;
            dest.Latch = source.Latch;

            dest.VideoMode = source.VideoMode;
            dest.CoordDisplay = source.CoordDisplay;
        }

        private void UpdateUIFromEditState()
        {
            if (_isUIUpdatePending)
                return;
            _isUIUpdatePending = true;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                bool isNotLinked = string.IsNullOrEmpty(_config.SystemLinkedTo(TGPSystem.SystemTag));

                Utilities.SetTextBoxEnabledAndText(uiTextLaserCode, isNotLinked, true, _editState.LaserCode);
                Utilities.SetTextBoxEnabledAndText(uiTextLSS, isNotLinked, true, _editState.LSS);
                Utilities.SetCheckEnabledAndState(uiCheckLatch, isNotLinked, _editState.Latch == "0");

                Utilities.SetComboEnabledAndSelection(uiComboVideoMode, isNotLinked, true, int.Parse(_editState.VideoMode));
                Utilities.SetComboEnabledAndSelection(uiComboCoordDisplay, isNotLinked, true, int.Parse(_editState.CoordDisplay));

                _isUIUpdatePending = false;
            });
        }

        /// <summary>
        /// Lazy load and return all the TextBox controls on this page having a Tag set.
        /// By convention, the tags are all property names. Keys in the returned dictionary
        /// are property names.
        /// </summary>
        private Dictionary<string, TextBox> PageTextBoxes
        {
            get
            {
                if (_pageTextBoxes == null || _pageTextBoxes.Count == 0)
                {
                    var allTextBoxes = new List<TextBox>();
                    Utilities.FindChildren(allTextBoxes, this);

                    _pageTextBoxes = new Dictionary<string, TextBox>(allTextBoxes.Count);
                    foreach (var textBox in allTextBoxes)
                    {
                        if (textBox.Tag != null)
                            _pageTextBoxes[textBox.Tag.ToString()] = textBox;
                    }
                }
                return _pageTextBoxes;
            }
        }
        private Dictionary<string, TextBox> _pageTextBoxes;

        // ---- control event handlers --------------------------------------------------------------------------------

        /// <summary>
        /// Change handler for all of the ComboBoxes that manage a setting.
        /// Sets the corresponding edit state property value and updates the underlying config.
        /// </summary>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            switch (comboBox.Tag)
            {
                case "VideoMode":
                    _editState.VideoMode = comboBox.SelectedIndex.ToString();
                    break;
                case "CoordDisplay":
                    _editState.CoordDisplay = comboBox.SelectedIndex.ToString();
                    break;
                default:
                    throw new ApplicationException("Unexpected ComboBox: " + comboBox.Tag);
            }

            SaveEditStateToConfig();
        }

        /// <summary>
        /// Change handler for all of the TextBoxes that manage a setting.
        /// Sets the corresponding edit state property value and updates the underlying config.
        /// </summary>
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            switch (textBox.Tag)
            {
                case "LaserCode":
                    _editState.LaserCode = textBox.Text;
                    break;
                case "LSS":
                    _editState.LSS = textBox.Text;
                    break;
                default:
                    throw new ApplicationException("Unexpected TextBox: " + textBox.Tag);
            }

            SaveEditStateToConfig();
        }

        /// <summary>
        /// A UX nicety: highlight the whole value in a TextBox when it
        /// gets focus such that the new value can immediately be entered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.SelectAll();
        }

        private void uiCheckLatch_Clicked(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox.IsChecked == true)
                _editState.Latch = "0";
            else
                _editState.Latch = "1";
            SaveEditStateToConfig();
        }

        // ---- field validation -------------------------------------------------------------------------------------------

        private void SetFieldValidVisualState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        private void ValidateEditState(BindableObject editState, string propertyName)
        {
            List<string> errors = (List<string>)editState.GetErrors(propertyName);
            if (PageTextBoxes.ContainsKey(propertyName))
                SetFieldValidVisualState(PageTextBoxes[propertyName], (errors.Count == 0));
        }

        private void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            ValidateEditState(_editState, args.PropertyName);
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void BaseField_PropertyChanged(object sender, EventArgs args)
        {
            UpdateUIFromEditState();
        }

        // ---- page events -------------------------------------------------------------------------------------------

        // on clicks of the reset all button, reset all settings back to default.
        //
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the TGP configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                _config.UnlinkSystem(TGPSystem.SystemTag);
                UpdateLinkControls();
                _config.TGP.Reset();
                _config.Save(this, TGPSystem.SystemTag);
                CopyConfigToEditState();
            }
        }

        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, _config, TGPSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                _config.UnlinkSystem(TGPSystem.SystemTag);
                _config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                _config.LinkSystemTo(TGPSystem.SystemTag, _navArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                _config.Save(this);
            }

            CopyConfigToEditState();
            UpdateLinkControls();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _config = (A10CConfiguration)_navArgs.Config;

            Utilities.BuildSystemLinkLists(_navArgs.UIDtoConfigMap, _config.UID, TGPSystem.SystemTag, _configNameList, _configNameToUID);
            UpdateLinkControls();

            CopyConfigToEditState();

            base.OnNavigatedTo(args);
        }

        private void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, TGPSystem.SystemTag, _navArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }
    }
}
