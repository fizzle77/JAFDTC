// ********************************************************************************************************************
//
// F16CEditSteerpointPage.xaml.cs : ui c# for viper steerpoint editor page
//
// Copyright(C) 2023 ilominar/raven
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.UI;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using JAFDTC.Models.F16C.Misc;
using Windows.Graphics.Printing.Workflow;
using Microsoft.VisualBasic.FileIO;
using JAFDTC.Utilities.Networking;
using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// navigation argument for pages that navigate to the steerpoint editor. this provides the configuration being
    /// edited along with the specific steerpoint within the configuration that should be edited.
    /// </summary>
    public sealed class F16CEditStptPageNavArgs
    {
        public F16CConfiguration Config { get; set; }

        public F16CEditSteerpointListPage ParentEditor { get; set; }

        public int IndexStpt { get; set; }

        public bool IsUnlinked { get; set; }

        public F16CEditStptPageNavArgs(F16CEditSteerpointListPage parentEditor, F16CConfiguration config, int indexStpt,
                                       bool isUnlinked)
            => (ParentEditor, Config, IndexStpt, IsUnlinked) = (parentEditor, config, indexStpt, isUnlinked);
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class F16CEditSteerpointPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditStptPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. edits by the ui are usually
        // NOTE: directed at the EditStpt/EditStptIndex properties (exceptions occur when the edit requires changes
        // NOTE: to other steerpoints, such as add or replace vip/vrp).
        //
        private F16CConfiguration Config { get; set; }

        private SteerpointInfo EditStpt { get; set; }

        private int EditStptIndex { get; set; }

        private bool IsRebuildPending { get; set; }

        private List<PointOfInterest> CurPoIs { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, TextBox> _curStptFieldValueMap;
        private readonly List<TextBlock> _oap0FieldTitles;
        private readonly List<TextBlock> _oap1FieldTitles;
        private readonly Dictionary<string, TextBox> _oap0FieldValueMap;
        private readonly Dictionary<string, TextBox> _oap1FieldValueMap;
        private readonly List<TextBlock> _vxpFieldTitles;
        private readonly Dictionary<string, TextBox> _vxp0FieldValueMap;
        private readonly Dictionary<string, TextBox> _vxp1FieldValueMap;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditSteerpointPage()
        {
            InitializeComponent();

            EditStpt = new();
            EditStpt.ErrorsChanged += EditStpt_DataValidationError;
            EditStpt.PropertyChanged += EditField_PropertyChanged;
            for (int i = 0; i < EditStpt.OAP.Length; i++)
            {
                EditStpt.OAP[i].PropertyChanged += EditField_PropertyChanged;
                EditStpt.VxP[i].PropertyChanged += EditField_PropertyChanged;
            }
            EditStpt.OAP[0].ErrorsChanged += OAP0_DataValidationError;
            EditStpt.OAP[1].ErrorsChanged += OAP1_DataValidationError;
            EditStpt.VxP[0].ErrorsChanged += VxP0_DataValidationError;
            EditStpt.VxP[1].ErrorsChanged += VxP1_DataValidationError;

            CurPoIs = PointOfInterestDbase.Instance.Find();
            CurPoIs.Insert(0, new(PointOfInterestType.UNKNOWN, null, null, null, null, null));

            IsRebuildPending = false;

            _curStptFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Lat"] = uiStptValueLat,
                ["Lon"] = uiStptValueLon,
                ["Alt"] = uiStptValueAlt,
                ["TOS"] = uiStptValueTOS
            };
            _oap0FieldTitles = new List<TextBlock>()
            {
                uiStptOAP0TextTitle
            };
            _oap1FieldTitles = new List<TextBlock>()
            {
                uiStptOAP1TextTitle
            };
            _oap0FieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Range"] = uiStptOAPValueRange0,  ["Brng"] = uiStptOAPValueBrng0, ["Elev"] = uiStptOAPValueElev0,
            };
            _oap1FieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Range"] = uiStptOAPValueRange1, ["Brng"] = uiStptOAPValueBrng1, ["Elev"] = uiStptOAPValueElev1
            };
            _vxpFieldTitles = new List<TextBlock>()
            {
                uiStptVxP0TextTitle, uiStptVxP1TextTitle
            };
            _vxp0FieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Range"] = uiStptVxPValueRange0, ["Brng"] = uiStptVxPValueBrng0, ["Elev"] = uiStptVxPValueElev0,
            };
            _vxp1FieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Range"] = uiStptVxPValueRange1, ["Brng"] = uiStptVxPValueBrng1, ["Elev"] = uiStptVxPValueElev1
            };
            _defaultBorderBrush = uiStptOAPValueBrng1.BorderBrush;
            _defaultBkgndBrush = uiStptOAPValueBrng1.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local steerpoint setting and the appropriate steerpoint in the stpt configuration.
        // as we edit outside the config, we will make a deep copy. we cannot Clone() here as the UI is tied to the
        // specific EditStpt instance we set up at load.
        //
        private void CopyConfigToEdit(int index)
        {
            SteerpointInfo stptSrc = Config.STPT.Points[index];
            EditStpt.Number = stptSrc.Number;
            EditStpt.Name = new(stptSrc.Name);
            EditStpt.LatUI = NavpointInfoBase.ConvertFromLatDD(stptSrc.Lat, NavpointInfoBase.LLFormat.DDM_P3ZF);
            EditStpt.LonUI = NavpointInfoBase.ConvertFromLonDD(stptSrc.Lon, NavpointInfoBase.LLFormat.DDM_P3ZF);
            EditStpt.Alt = new(stptSrc.Alt);
            EditStpt.TOS = new(stptSrc.TOS);

            for (int i = 0; i < EditStpt.OAP.Length; i++)
            {
                EditStpt.OAP[i].Type = stptSrc.OAP[i].Type;
                EditStpt.OAP[i].Range = new(stptSrc.OAP[i].Range);
                EditStpt.OAP[i].Brng = new(stptSrc.OAP[i].Brng);
                EditStpt.OAP[i].Elev = new(stptSrc.OAP[i].Elev);

                EditStpt.VxP[i].Type = stptSrc.VxP[i].Type;
                EditStpt.VxP[i].Range = new(stptSrc.VxP[i].Range);
                EditStpt.VxP[i].Brng = new(stptSrc.VxP[i].Brng);
                EditStpt.VxP[i].Elev = new(stptSrc.VxP[i].Elev);
            }
        }

        private void CopyEditToConfig(int index, bool isPersist = false)
        {
            if (!EditStpt.HasErrors)
            {
                SteerpointInfo stptDst = Config.STPT.Points[index];
                stptDst.Number = EditStpt.Number;
                stptDst.Name = EditStpt.Name;
                stptDst.Lat = EditStpt.Lat;
                stptDst.Lon = EditStpt.Lon;
                stptDst.Alt = EditStpt.Alt;
                //
                // TOS field uses text mask and can come back as "--:--:--" when empty. this is really "" and, since
                // that value is OK, remove the error.
                //
                stptDst.TOS = (EditStpt.TOS == "––:––:––") ? "" : EditStpt.TOS;

                for (int i = 0; i < EditStpt.OAP.Length; i++)
                {
                    stptDst.OAP[i].Type = EditStpt.OAP[i].Type;
                    stptDst.OAP[i].Range = EditStpt.OAP[i].Range;
                    stptDst.OAP[i].Brng = EditStpt.OAP[i].Brng;
                    stptDst.OAP[i].Elev = EditStpt.OAP[i].Elev;

                    stptDst.VxP[i].Type = EditStpt.VxP[i].Type;
                    stptDst.VxP[i].Range = EditStpt.VxP[i].Range;
                    stptDst.VxP[i].Brng = EditStpt.VxP[i].Brng;
                    stptDst.VxP[i].Elev = EditStpt.VxP[i].Elev;
                }

                if (isPersist)
                {
                    Config.Save(NavArgs.ParentEditor, STPTSystem.SystemTag);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// set the border brush and background for a TextBox based on validity. valid fields use the defaults, invalid
        /// fields use ErrorFieldBorderBrush from the resources.
        /// </summary>
        private void SetFieldValidState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
        }

        private void ValidateAllFields(Dictionary<string, TextBox> fields, IEnumerable errors)
        {
            Dictionary<string, bool> map = new();
            foreach (string error in errors)
            {
                map[error] = true;
            }
            foreach (KeyValuePair<string, TextBox> kvp in fields)
            {
                SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key));
            }
        }

        private void CoreDataValidationError(INotifyDataErrorInfo obj, string propertyName, Dictionary<string, TextBox> fields)
        {
            if (propertyName == null)
            {
                ValidateAllFields(fields, obj.GetErrors(null));
            }
            else
            {
                List<string> errors = (List<string>)obj.GetErrors(propertyName);
                if (fields.ContainsKey(propertyName))
                {
                    SetFieldValidState(fields[propertyName], (errors.Count == 0));
                }
            }
            RebuildInterfaceState();
        }

        // TODO: document
        private void OAP0_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            CoreDataValidationError(EditStpt.OAP[0], args.PropertyName, _oap0FieldValueMap);
        }

        private void OAP1_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            CoreDataValidationError(EditStpt.OAP[1], args.PropertyName, _oap1FieldValueMap);
        }

        // TODO: document
        private void VxP0_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            CoreDataValidationError(EditStpt.VxP[0], args.PropertyName, _vxp0FieldValueMap);
        }

        private void VxP1_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            CoreDataValidationError(EditStpt.VxP[1], args.PropertyName, _vxp1FieldValueMap);
        }

        // TODO: document
        private void EditStpt_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if ((args.PropertyName == "TOS") && (EditStpt.TOS == "––:––:––"))
            {
                // TOS field uses text mask and can come back as "--:--:--" when empty. this is really "" and, since
                // that value is OK, remove the error.
                //
                EditStpt.ClearErrors("TOS");
                TextBox field = (TextBox)_curStptFieldValueMap[args.PropertyName];
                SetFieldValidState(field, true);
            }
            else
            {
                CoreDataValidationError(EditStpt, args.PropertyName, _curStptFieldValueMap);
            }
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void EditField_PropertyChanged(object sender, EventArgs args)
        {
            RebuildInterfaceState();
        }

        // returns true if the current state has errors, false otherwise.
        //
        private bool CurStateHasErrors()
        {
            bool hasErrors = EditStpt.HasErrors;
            for (int i = 0; i < EditStpt.OAP.Length; i++)
            {
                hasErrors = (EditStpt.OAP[i].HasErrors || EditStpt.VxP[i].HasErrors) || hasErrors;
            }
            return hasErrors;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        // rebuild the reference point interface state for the vxp reference point ui. enables for the text boxes
        // are handled in RebuildEnableState().
        //
        private void RebuildRefPointState(RefPointTypes rpType, ComboBox combo, List<TextBlock> titles)
        {
            combo.SelectedIndex = rpType switch
            {
                RefPointTypes.NONE => 0,
                RefPointTypes.OAP => 1,
                RefPointTypes.VIP => 1,
                RefPointTypes.VRP => 2,
                _ => 0,
            };
            bool isNone = (combo.SelectedIndex == 0);
            foreach (TextBlock title in titles)
            {
                title.Style = (Style)((isNone) ? Resources["DisabledStaticTextStyle"]
                                               : Resources["EnabledStaticTextStyle"]);
            }
            if (rpType == RefPointTypes.VRP)
            {
                uiStptVxPRngTextHeader.Text = "Range (nm)";
                uiStptVxPValueRange0.PlaceholderText = "0.0";
                uiStptVxPValueRange1.PlaceholderText = "0.0";
                uiStptVxP0TextTitle.Text = "Target to VRP";
                uiStptVxP1TextTitle.Text = "Target to PUP";
            }
            else if (rpType == RefPointTypes.VIP)
            {
                uiStptVxPRngTextHeader.Text = "Range (ft)";
                uiStptVxPValueRange0.PlaceholderText = "0";
                uiStptVxPValueRange1.PlaceholderText = "0";
                uiStptVxP0TextTitle.Text = "VIP to Target";
                uiStptVxP1TextTitle.Text = "VIP to PUP";
            }
        }

        // rebuild the enable state of the buttons in the ui based on current configuration setup.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));

            Utilities.SetEnableState(uiPoIComboSelect, isEditable);
            Utilities.SetEnableState(uiPoIBtnApply, isEditable);

            Utilities.SetEnableState(uiStptValueName, isEditable);
            foreach (KeyValuePair<string, TextBox> kvp in _curStptFieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable);
            }

            Utilities.SetEnableState(uiStptOAP0Combo, isEditable);
            Utilities.SetEnableState(uiStptOAP1Combo, isEditable);
            foreach (KeyValuePair<string, TextBox> kvp in _oap0FieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable && (EditStpt.OAP[0].Type == RefPointTypes.OAP));
            }
            foreach (KeyValuePair<string, TextBox> kvp in _oap1FieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable && (EditStpt.OAP[1].Type == RefPointTypes.OAP));
            }

            Utilities.SetEnableState(uiStptVxPCombo, isEditable);
            foreach (KeyValuePair<string, TextBox> kvp in _vxp0FieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable && (EditStpt.VxP[0].Type != RefPointTypes.NONE));
            }
            foreach (KeyValuePair<string, TextBox> kvp in _vxp1FieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable && (EditStpt.VxP[0].Type != RefPointTypes.NONE));
            }

            Utilities.SetEnableState(uiPoIBtnApply, isEditable && (uiPoIComboSelect.SelectedIndex > 0));
            Utilities.SetEnableState(uiPoIBtnCapture, isEditable);

            Utilities.SetEnableState(uiStptBtnPrev, !CurStateHasErrors() && (EditStptIndex > 0));
            Utilities.SetEnableState(uiStptBtnAdd, isEditable && !CurStateHasErrors());
            Utilities.SetEnableState(uiStptBtnNext, !CurStateHasErrors() && (EditStptIndex < (Config.STPT.Points.Count - 1)));

            // TODO: ok button should also enable if you have lat/lon/alt specified, if vrp/vip both points needed
            Utilities.SetEnableState(uiAcceptBtnOK, isEditable && !CurStateHasErrors());
        }

        // rebuild the state of controls on the page in response to a change in the configuration. the configuration
        // is saved if requested.
        //
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    uiStptTextNum.Text = $"Steerpoint {EditStpt.Number} Information";
                    RebuildRefPointState(EditStpt.OAP[0].Type, uiStptOAP0Combo, _oap0FieldTitles);
                    RebuildRefPointState(EditStpt.OAP[1].Type, uiStptOAP1Combo, _oap1FieldTitles);
                    RebuildRefPointState(EditStpt.VxP[0].Type, uiStptVxPCombo, _vxpFieldTitles);
                    RebuildEnableState();
                    IsRebuildPending = false;
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- buttons -----------------------------------------------------------------------------------------------

        // cancel click: unwind navigation without saving any changes to the configuration.
        //
        private void AcceptBtnCancel_Click(object sender, RoutedEventArgs args)
        {
            Frame.GoBack();
        }

        // ok click: save configuration and navigate back to previous page in nav stack.
        //
        private void AcceptBtnOK_Click(object sender, RoutedEventArgs args)
        {
            if (CurStateHasErrors())
            {
                RebuildEnableState();
            }
            else
            {
                CopyEditToConfig(EditStptIndex, true);
                Frame.GoBack();
            }
        }

        // ---- poi management ----------------------------------------------------------------------------------------

        // poi combo selection changed: update enable state in the ui.
        //
        private void PoIComboSelect_SelectionChanged(object sender, RoutedEventArgs args)
        {
            RebuildEnableState();
        }

        // apply poi click: copy poi information into current steerpoint and reset poi selection to "none".
        //
        private void PoIBtnApply_Click(object sender, RoutedEventArgs args) 
        {
            PointOfInterest poi = (PointOfInterest)uiPoIComboSelect.SelectedItem;
            EditStpt.Name = poi.Name;
            EditStpt.LatUI = NavpointInfoBase.ConvertFromLatDD(poi.Latitude, NavpointInfoBase.LLFormat.DDM_P3ZF);
            EditStpt.LonUI = NavpointInfoBase.ConvertFromLonDD(poi.Longitude, NavpointInfoBase.LLFormat.DDM_P3ZF);
            EditStpt.Alt = poi.Elevation;
            EditStpt.TOS = "";
            EditStpt.ClearErrors();

            uiPoIComboSelect.SelectedIndex = 0;

            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PoIBtnCapture_Click(object sender, RoutedEventArgs args)
        {
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += PoIBtnCapture_WyptCaptureDataReceived;
            await Utilities.CaptureSingleDialog(Content.XamlRoot, "Steerpoint");
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= PoIBtnCapture_WyptCaptureDataReceived;

            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void PoIBtnCapture_WyptCaptureDataReceived(WyptCaptureData[] wypts)
        {
            if ((wypts.Length > 0) && !wypts[0].IsTarget)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    EditStpt.Name = "DCS Capture";
                    EditStpt.LatUI = NavpointInfoBase.ConvertFromLatDD(wypts[0].Latitude, NavpointInfoBase.LLFormat.DDM_P3ZF);
                    EditStpt.LonUI = NavpointInfoBase.ConvertFromLonDD(wypts[0].Longitude, NavpointInfoBase.LLFormat.DDM_P3ZF);
                    EditStpt.Alt = wypts[0].Elevation.ToString();
                    EditStpt.TOS = "";
                    EditStpt.ClearErrors();
                });
            }
        }

        // ---- steerpoint management ---------------------------------------------------------------------------------

        // steerpoint previous click: save the current steerpoint and move to the previous steerpoint.
        //
        private void StptBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditStptIndex, true);
            EditStptIndex -= 1;
            CopyConfigToEdit(EditStptIndex);
            RebuildInterfaceState();
        }

        // steerpoint previous click: save the current steerpoint and move to the next steerpoint.
        //
        private void StptBtnNext_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditStptIndex, true);
            EditStptIndex += 1;
            CopyConfigToEdit(EditStptIndex);
            RebuildInterfaceState();
        }

        // steerpoint add click: save the current steerpoint and add a new steerpoint to the end of the list.
        //
        private void StptBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditStptIndex, true);
            SteerpointInfo stpt = Config.STPT.Add();
            EditStptIndex = Config.STPT.Points.IndexOf(stpt);
            CopyConfigToEdit(EditStptIndex);
            RebuildInterfaceState();
        }

        // ---- reference point type selection ------------------------------------------------------------------------

        // oap combo click: update the selection and ui state for the oap point if the configuration has changed.
        //
        private void OAPCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            ComboBox combo = (ComboBox)sender;
            int index = (combo.Tag as string == "0") ? 0 : 1;
            if ((EditStpt.OAP[index].Type == RefPointTypes.OAP) && (combo.SelectedIndex == 0))
            {
                EditStpt.OAP[index].Reset();
                EditStpt.OAP[index].Type = RefPointTypes.NONE;
                RebuildInterfaceState();
            }
            else if ((EditStpt.OAP[index].Type == RefPointTypes.NONE) && (combo.SelectedIndex == 1))
            {
                EditStpt.OAP[index].Reset();
                EditStpt.OAP[index].Type = RefPointTypes.OAP;
                RebuildInterfaceState();
            }
        }

        // vxp combo click: update the selection and ui state for the vxp point if the configuration has changed.
        //
        private async void VizCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            ComboBox combo = (ComboBox)sender;
            if ((EditStpt.VxP[0].Type != RefPointTypes.NONE) && (combo.SelectedIndex == 0))
            {
                for (int i = 0; i < EditStpt.VxP.Length; i++)
                {
                    EditStpt.VxP[i].Reset();
                    EditStpt.VxP[i].Type = RefPointTypes.NONE;
                }
                RebuildInterfaceState();
            }
            else
            {
                RefPointTypes rpType = RefPointTypes.NONE;
                string rpString = null;

                if ((EditStpt.VxP[0].Type != RefPointTypes.VIP) && (combo.SelectedIndex == 1))
                {
                    rpType = RefPointTypes.VIP;
                    rpString = "VIP";
                }
                else if ((EditStpt.VxP[0].Type != RefPointTypes.VRP) && (combo.SelectedIndex == 2))
                {
                    rpType = RefPointTypes.VRP;
                    rpString = "VRP";
                }

                if (rpType != RefPointTypes.NONE)
                {
                    ContentDialogResult result = ContentDialogResult.Primary;
                    int sp;

                    for (sp = 0;
                         (sp < Config.STPT.Points.Count) && (Config.STPT.Points[sp].VxP[0].Type != rpType);
                         sp++)
                        ;
                    if (sp < Config.STPT.Points.Count)
                    {
                        result = await Utilities.Message2BDialog(
                            Content.XamlRoot,
                            $"{rpString} Defined",
                            $"A {rpString} is already defined on Steerpoint {sp+1}. Would you like to remove it and reference {rpString} to this steerpoint instead?",
                            "Remove");
                    }
                    if (result == ContentDialogResult.Primary)
                    {
                        if (sp < Config.STPT.Points.Count)
                        {
                            Config.STPT.Points[sp].VxP[0].Reset();
                            Config.STPT.Points[sp].VxP[1].Reset();
                            Config.Save(NavArgs.ParentEditor, STPTSystem.SystemTag);
                        }
                        for (int i = 0; i < EditStpt.VxP.Length; i++)
                        {
                            EditStpt.VxP[i].Reset();
                            EditStpt.VxP[i].Type = rpType;
                        }
                        RebuildInterfaceState();
                    }
                }
            }
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        // TODO: document
        private void StptTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            TextBox textBox = (TextBox)sender;
            if ((textBox == uiStptValueTOS) && (textBox.Text == "––:––:––"))
            {
                // TOS field uses text mask and can come back as "--:--:--" when empty. this is really "" and, since
                // that value is OK, remove the error. note that as we just lost focus, the bound property in
                // EditStpt.TOS may not yet be set up.
                //
                EditStpt.ClearErrors("TOS");
                SetFieldValidState(uiStptValueTOS, true);
            }
            RebuildEnableState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        // on navigating to this page, set up and tear down our internal and ui state based on the configuration we
        // are editing.
        //
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (F16CEditStptPageNavArgs)args.Parameter;

            Config = NavArgs.Config;

            EditStptIndex = NavArgs.IndexStpt;
            CopyConfigToEdit(EditStptIndex);

            ValidateAllFields(_curStptFieldValueMap, EditStpt.GetErrors(null));
            ValidateAllFields(_oap0FieldValueMap, EditStpt.OAP[0].GetErrors(null));
            ValidateAllFields(_oap1FieldValueMap, EditStpt.OAP[1].GetErrors(null));
            ValidateAllFields(_vxp0FieldValueMap, EditStpt.VxP[0].GetErrors(null));
            ValidateAllFields(_vxp1FieldValueMap, EditStpt.VxP[1].GetErrors(null));
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }
    }
}
