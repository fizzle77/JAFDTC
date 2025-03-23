// ********************************************************************************************************************
//
// FA18CEditPreplanPage.xaml.cs : ui c# for hornet pre-planned editor page
//
// Copyright(C) 2024 ilominar/raven
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

using CommunityToolkit.Common;
using JAFDTC.Models;
using JAFDTC.Models.DCS;
using JAFDTC.Models.FA18C;
using JAFDTC.Models.FA18C.PP;
using JAFDTC.Models.FA18C.WYPT;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.EnterpriseData;
using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;
using static System.Collections.Specialized.BitVector32;

namespace JAFDTC.UI.FA18C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class FA18CEditPreplanPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(PPSystem.SystemTag, "Pre-Planned", "PP", Glyphs.PP, typeof(FA18CEditPreplanPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => ((FA18CConfiguration)Config).PP;

        protected override String SystemTag => PPSystem.SystemTag;

        protected override string SystemName => "pre-planned";

        protected override bool IsPageStateDefault => SystemConfig.IsDefault;

        // ---- internal properties

        // NOTE: the ui always interacts with Edit* when editing pre-planned system values, EditProgNum defines which program
        // NOTE: the ui is currently editing.
        //
        private PPCoordinateInfo EditCoordInfo { get; set; }

        private Weapons EditWeapon { get; set; }

        private int EditBoxedPPNum { get; set; }

        private int EditStationNum { get; set; }

        private int EditProgIdx { get; set; }

        private int EditCoordIdx { get; set; }

        private int EditCoordSrcIdx { get; set; }

        private PointOfInterest CurSelectedPoI { get; set; }

        private PoIFilterSpec FilterSpec { get; set; }

        private bool IsCoordSupressError { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, TextBox> _curPosnFieldValueMap;
        private readonly List<FontIcon> _staSelComboIcons;
        private readonly List<FontIcon> _pgmSelComboIcons;
        private readonly List<TextBlock> _pgmSelComboTextBlocks;
        private readonly Brush _brushEnabledText;
        private readonly Brush _brushDisabledText;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CEditPreplanPage()
        {
            EditWeapon = Weapons.NONE;
            EditBoxedPPNum = 0;
            EditStationNum = 2;
            EditProgIdx = 0;
            EditCoordSrcIdx = 0;
            EditCoordIdx = 0;
            EditCoordInfo = new();

            InitializeComponent();
            InitializeBase(EditCoordInfo, uiPosnValueName, uiCtlLinkResetBtns);

            CurSelectedPoI = null;
            IsCoordSupressError = true;

            _curPosnFieldValueMap = new Dictionary<string, TextBox>
            {
                ["Lat"] = uiPosnValueLat,
                ["Lon"] = uiPosnValueLon,
                ["Alt"] = uiPosnValueAlt
            };
            _staSelComboIcons = new List<FontIcon>()
            {
                uiStationSelectItem2Icon, uiStationSelectItem3Icon, uiStationSelectItem7Icon, uiStationSelectItem8Icon
            };
            _pgmSelComboIcons = new List<FontIcon>()
            {
                uiProgSelectItem0Icon, uiProgSelectItem1Icon, uiProgSelectItem2Icon, uiProgSelectItem3Icon,
                uiProgSelectItem4Icon, uiProgSelectItem5Icon
            };
            _pgmSelComboTextBlocks = new List<TextBlock>()
            {
                uiProgSelectItem0Text, uiProgSelectItem1Text, uiProgSelectItem2Text, uiProgSelectItem3Text,
                uiProgSelectItem4Text, uiProgSelectItem5Text
            };

            // HACK: this is a stupid hack because i'm too lazy to figure out how to get this from a resource. fix
            // HACK: this at some point...
            //
            TextBlock tmpTxtBlk = new();
            _brushEnabledText = tmpTxtBlk.Foreground;
            tmpTxtBlk.Style = Application.Current.Resources["TableHeaderTextStyle"] as Style;
            _brushDisabledText = tmpTxtBlk.Foreground;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Copy data from the system configuration object to the edit objects the page interacts with. this copies
        /// from the station, program, and coordinate given by EditStationNum, EditProgIdx, and EditCoordIdx.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            FA18CConfiguration config = (FA18CConfiguration)Config;
            int station = EditStationNum;
            int pgm = EditProgIdx;
            int coord = EditCoordIdx;

            StartUIRebuild();

            EditWeapon = config.PP.Stations[station].Weapon;
            EditBoxedPPNum = config.PP.Stations[station].BoxedPP;

            // if there is no coord yet at the given index (because it hasn't been yet defined), we'll set up empty
            // and CopyConfigToEditState will handle adding the STP[] in the configuration.
            //
            // if in waypoint mode with an invalid waypoint number, revert to position mode. for position mode, copy
            // out the name/lat/lon/elev from the config. for waypoint mode, set these fields to match the waypoint.
            //
            PPCoordinateInfo srcCoordInfo = null;
            if (coord == 0)
                srcCoordInfo = config.PP.Stations[station].PP[pgm];
            else if (config.PP.Stations[station].STP.Count >= coord)
                srcCoordInfo = config.PP.Stations[station].STP[coord - 1];

            EditCoordInfo.Reset();
            if ((srcCoordInfo == null) || (srcCoordInfo.WaypointNumber > config.WYPT.Points.Count))
            {
                EditCoordInfo.WaypointNumber = 0;
            }
            else if (srcCoordInfo.WaypointNumber == 0)
            {
                EditCoordInfo.WaypointNumber = srcCoordInfo.WaypointNumber;
                if (srcCoordInfo.IsValid)
                {
                    EditCoordInfo.Name = srcCoordInfo.Name;
                    //
                    // NOTE: conversion necessary as srcCoordInfo and EditCoordInfo may have different ui formats.
                    //
                    EditCoordInfo.LatUI = Coord.ConvertFromLatDD(srcCoordInfo.Lat, LLFormat.DMDS_P2ZF);
                    EditCoordInfo.LonUI = Coord.ConvertFromLonDD(srcCoordInfo.Lon, LLFormat.DMDS_P2ZF);
                    EditCoordInfo.Alt = srcCoordInfo.Alt;
                }
            }
            else
            {
                WaypointInfo wypt = config.WYPT.Points[srcCoordInfo.WaypointNumber - 1];
                EditCoordInfo.WaypointNumber = srcCoordInfo.WaypointNumber;
                if (wypt.IsValid)
                {
                    EditCoordInfo.Name = wypt.Name;
                    //
                    // NOTE: conversion necessary as wypt and EditCoordInfo have different ui formats.
                    //
                    EditCoordInfo.LatUI = Coord.ConvertFromLatDD(wypt.Lat, LLFormat.DMDS_P2ZF);
                    EditCoordInfo.LonUI = Coord.ConvertFromLonDD(wypt.Lon, LLFormat.DMDS_P2ZF);
                    EditCoordInfo.Alt = wypt.Alt;
                }
            }

            IsCoordSupressError = true;
            SetPosnFieldValidVisualState(null);

            FinishUIRebuild();

            UpdateUIFromEditState();
        }

        /// <summary>
        /// Copy data from the edit objects the page interacts with to the system configuration object and persist the
        /// updated configuration to disk. this updates the station, program, and coordinate given by EditStationNum,
        /// EditProgIdx, and EditCoordIdx.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            if (!CurStateHasErrors() && !IsUIRebuilding)
            {
                FA18CConfiguration config = (FA18CConfiguration)Config;
                int station = EditStationNum;
                int pgm = EditProgIdx;
                int coord = EditCoordIdx;

                config.PP.Stations[station].Weapon = EditWeapon;
                config.PP.Stations[station].BoxedPP = EditBoxedPPNum;

                if (EditWeapon == Weapons.NONE)
                {
                    config.PP.Stations[station].Reset();
                }
                else
                {
                    bool isDelete = (EditCoordInfo.WaypointNumber == -1);
                    if ((config.PP.Stations[station].STP.Count > 0) &&
                        ((isDelete && (coord == 0)) || (EditWeapon != Weapons.SLAM_ER)))
                    {
                        // reset stp coord list. this can occur if the stp coord list has more than one element and
                        // (a) user clears target coord, or (b) weapon does not support stp coords.
                        //
                        config.PP.Stations[station].STP.Clear();
                        coord = (coord != 0) ? -1 : coord;
                    }
                    else if (isDelete && (coord != 0))
                    {
                        // remove stp coordinate from steerpoint list.
                        //
                        config.PP.Stations[station].STP.RemoveRange(coord - 1, 1);
                        coord = -1;
                    }
                    if (isDelete)
                        EditCoordInfo.Reset();

                    if (coord != -1)
                    {
                        while ((coord != 0) && (config.PP.Stations[station].STP.Count < coord))
                            config.PP.Stations[station].STP.Add(new());
                        PPCoordinateInfo dstCoordInfo = (coord == 0) ? config.PP.Stations[station].PP[pgm]
                                                                     : config.PP.Stations[station].STP[coord - 1];
                        dstCoordInfo.Reset();
                        dstCoordInfo.WaypointNumber = EditCoordInfo.WaypointNumber;

                        // for position mode, copy out the name/lat/lon/elev from the editor. for waypoint mode, these
                        // fields are empty strings due to the reset above.
                        //
                        if (EditCoordInfo.WaypointNumber == 0)
                        {
                            dstCoordInfo.Name = EditCoordInfo.Name;
                            //
                            // NOTE: use Lat/Lon instead of LatUI/LonUI as dstCoordInfo and EditCoordInfo may have different
                            // NOTE: ui formats.
                            //
                            dstCoordInfo.Lat = EditCoordInfo.Lat;
                            dstCoordInfo.Lon = EditCoordInfo.Lon;
                            dstCoordInfo.Alt = EditCoordInfo.Alt;
                        }
                    }
                }

                Config.Save(this, SystemTag);
            }

            UpdateUIFromEditState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        private void SetPosnFieldValidVisualState(string propertyName)
        {
            string name = (propertyName == "LatUI") ? "Lat" : ((propertyName == "LonUI") ? "Lon" : propertyName);

            List<string> errors = (List<string>)EditCoordInfo.GetErrors();
            if ((propertyName != null) && _curPosnFieldValueMap.ContainsKey(name))
                SetFieldValidVisualState(_curPosnFieldValueMap[name], IsCoordSupressError || !errors.Contains(propertyName));
            else if (propertyName == null)
                foreach (KeyValuePair<string, TextBox> kvp in _curPosnFieldValueMap)
                    SetFieldValidVisualState(_curPosnFieldValueMap[kvp.Key], IsCoordSupressError || !errors.Contains(kvp.Key));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        protected override void EditState_ErrorsChanged(object sender, DataErrorsChangedEventArgs args)
        {
            if (!IsUIRebuilding)
            {
                SetPosnFieldValidVisualState(args.PropertyName);
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// property changed: rebuild interface state to account for configuration changes.
        /// </summary>
        protected override void EditState_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (!IsUIRebuilding)
            {
                string name = (IsCoordSupressError) ? null : args.PropertyName;
                IsCoordSupressError = false;
                SetPosnFieldValidVisualState(name);
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// return true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            return ((EditCoordInfo.WaypointNumber == 0) && EditCoordInfo.HasErrors && !EditCoordInfo.IsEmpty);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// build contents of the coordinate source selection combo. this menu contains "Position" along with each
        /// of the defined nav waypoints. as such, it should not need to be rebuilt while editing a pre-planned system.
        /// </summary>
        private void BuildCoordSourceSelectMenu()
        {
            List<TextBlock> items = new()
            {
                new TextBlock { Text = "Position", Tag = "0", HorizontalAlignment = HorizontalAlignment.Left },
            };
            int num = 1;
            foreach (WaypointInfo wypt in ((FA18CConfiguration)Config).WYPT.Points)
            {
                int max = Math.Min(40, wypt.Name.Length);
                items.Add(new TextBlock
                {
                    Text = $"WP{num}: " + ((wypt.Name.Length > max) ? (wypt.Name[..max] + "...") : wypt.Name),
                    Tag = num.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Left
                });
                num++;
            }

            StartUIRebuild();
            uiCoordSrcSelectCombo.ItemsSource = items;
            uiCoordSrcSelectCombo.SelectedIndex = 0;
            FinishUIRebuild();
        }

        /// <summary>
        /// core interface code for unloading a weapon from a station. prompts user for confirmation before resetting
        /// the station, saving the config, and rebuilding ui state.
        /// </summary>
        /// <returns></returns>
        private async Task<ContentDialogResult> CoreUnloadWeaponUI()
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Unload Weapon?",
                $"Are you sure you want to unload the ordnance on station {EditStationNum}?" +
                $" This will clear all programming for the weapon and station. This action cannot be undone.",
                "Unload"
            );
            if (result == ContentDialogResult.Primary)
            {
                EditWeapon = Weapons.NONE;
                EditProgIdx = 0;
                EditCoordSrcIdx = 0;
                EditCoordIdx = 0;
                //
                // NOTE: CopyEditToConfig takes care of resetting the station in response to removing weapon.
                //
                // TODO: this feels wrong?
                SaveEditStateToConfig();
                CopyConfigToEditState();
                ResetComboSelectionState();
            }
            return result;
        }

        /// <summary>
        /// change the weapon station selection. this resets the program and coordinate selection to 0 and sets the
        /// coordinate source consistent with the program 0, coordinate 0 source.
        /// </summary>
        private void SelectStation(int station)
        {
            if (EditStationNum != station)
            {
                SaveEditStateToConfig();
                EditStationNum = station;
                EditProgIdx = 0;
                EditCoordIdx = 0;
                CopyConfigToEditState();
                EditCoordSrcIdx = EditCoordInfo.WaypointNumber;
                ResetComboSelectionState();
            }
        }

        /// <summary>
        /// change the station program selection. this resets the coordinate selection to 0 and sets the coordiante
        /// source consistent with the coordinate 0 source.
        /// </summary>
        private void SelectProgram(int program)
        {
            if (EditProgIdx != program)
            {
                SaveEditStateToConfig();
                EditProgIdx = program;
                EditCoordIdx = 0;
                CopyConfigToEditState();
                EditCoordSrcIdx = EditCoordInfo.WaypointNumber;
                ResetComboSelectionState();
            }
        }

        /// <summary>
        /// change the station program coordinate selection. this sets the coordinate source consistent with the
        /// coordinate 0 source.
        /// </summary>
        private void SelectCoord(int coord)
        {
            if (EditCoordIdx != coord)
            {
                SaveEditStateToConfig();
                EditCoordIdx = coord;
                CopyConfigToEditState();
                EditCoordSrcIdx = EditCoordInfo.WaypointNumber;
                ResetComboSelectionState();
            }
        }

        /// <summary>
        /// change the station program coordiante source selection. updates the coordinate edit fields to match the
        /// selected waypoint if position source is not selected. src is a menu index: 0 => position, >0 => waypoint.
        /// </summary>
        private void SelectCoordSource(int src)
        {
            if (EditCoordSrcIdx != src)
            {
                EditCoordInfo.WaypointNumber = src;
                if (src > 0)
                {
                    WaypointInfo wypt = ((FA18CConfiguration)Config).WYPT.Points[src - 1];
                    EditCoordInfo.Name = wypt.Name;
                    //
                    // NOTE: use Lat/Lon instead of LatUI/LonUI as wypt and EditCoordInfo have different ui formats.
                    //
                    EditCoordInfo.LatUI = Coord.ConvertFromLatDD(wypt.Lat, LLFormat.DMDS_P2ZF);
                    EditCoordInfo.LonUI = Coord.ConvertFromLonDD(wypt.Lon, LLFormat.DMDS_P2ZF);
                    EditCoordInfo.Alt = wypt.Alt;
                }
                SaveEditStateToConfig();
                EditCoordSrcIdx = src;
                CopyConfigToEditState();
                EditCoordSrcIdx = EditCoordInfo.WaypointNumber;
                ResetComboSelectionState();
            }
        }

        /// <summary>
        /// reset the selection state of the weapon, program, coordinate source, and coordinate combo boxes based on
        /// the current editor state.
        /// </summary>
        private void ResetComboSelectionState()
        {
            StartUIRebuild();

            RebuildCoordSelectMenu();

            uiWeaponSelectCombo.SelectedIndex = (int)(((FA18CConfiguration)Config).PP.Stations[EditStationNum].Weapon);
            uiProgSelectCombo.SelectedIndex = EditProgIdx;
            uiCoordSelectCombo.SelectedIndex = EditCoordIdx;
            uiCoordSrcSelectCombo.SelectedIndex = EditCoordSrcIdx;

            FinishUIRebuild();

            UpdateUIFromEditState();
        }

        /// <summary>
        /// update visibility of the "modified" icons in the station select combo to match the config default state.
        /// </summary>
        private void RebuildStationSelectMenu()
        {
            for (int i = 0; i < uiStationSelectCombo.Items.Count; i++)
            {
                Grid item = uiStationSelectCombo.Items[i] as Grid;
                PPStation station = ((FA18CConfiguration)Config).PP.Stations[int.Parse((string)item.Tag)];
                _staSelComboIcons[i].Visibility = (station.IsDefault) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// update visibility of the "modified" icons in the program select combo to match the config default state.
        /// </summary>
        private void RebuildProgramSelectMenu()
        {
            FA18CConfiguration config = (FA18CConfiguration)Config;
            for (int i = 0; i < uiProgSelectCombo.Items.Count; i++)
            {
                Grid item = uiProgSelectCombo.Items[i] as Grid;
                bool iIsHidden = config.PP.Stations[EditStationNum].IsPPDefault(int.Parse((string)item.Tag));
                _pgmSelComboIcons[i].Visibility = (iIsHidden) ? Visibility.Collapsed : Visibility.Visible;
                _pgmSelComboTextBlocks[i].FontWeight = ((EditBoxedPPNum - 1) == i) ? FontWeights.Bold : FontWeights.Normal;
            }
        }

        /// <summary>
        /// rebuild contents of the coordiante select menu. this menu contains "target" followed by defined stps
        /// for slam-er weapons.
        /// </summary>
        private void RebuildCoordSelectMenu()
        {
            PPStation station = ((FA18CConfiguration)Config).PP.Stations[EditStationNum];
            bool isTargetOnly = ((EditWeapon != Weapons.SLAM_ER) || !station.PP[EditProgIdx].IsValid);
            int selIndex = uiCoordSelectCombo.SelectedIndex;

            List<TextBlock> items = new()
            {
                new TextBlock { Text = "Target", Tag = "0", HorizontalAlignment = HorizontalAlignment.Left },
            };
            int stpMaxNum = Math.Min(5, station.STP.Count + 1);
            for (int i = 1; !isTargetOnly && (i <= stpMaxNum); i++)
            {
                items.Add(new TextBlock
                {
                    Text = $"STP {i}",
                    Tag = i.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Left
                });
                if ((i > station.STP.Count) || !station.STP[i - 1].IsValid)
                    break;
            }

            if (uiCoordSelectCombo.Items.Count != items.Count)
            {
                StartUIRebuild();
                uiCoordSelectCombo.ItemsSource = items;
                uiCoordSelectCombo.SelectedIndex = (selIndex < items.Count) ? selIndex : 0;
                FinishUIRebuild();
            }
        }

        /// <summary>
        /// rebuild the point of interest list in the filter box.
        /// </summary>
        private void RebuildPointsOfInterest()
        {
            uiPoINameFilterBox.ItemsSource = NavpointUIHelper.RebuildPointsOfInterest(FilterSpec, uiPoINameFilterBox.Text);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// vi RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            FA18CConfiguration config = (FA18CConfiguration)Config;
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(PPSystem.SystemTag));
            bool isDCSListening = curApp.IsDCSAvailable && (curApp.DCSActiveAirframe == Config.Airframe);
            bool isWyptAvailable = (uiCoordSrcSelectCombo.Items.Count > 1);
            bool isWeaponLoaded = (EditWeapon != Weapons.NONE);
            bool isSLAMER = (EditWeapon == Weapons.SLAM_ER);
            bool isProgDefault = config.PP.Stations[EditStationNum].IsPPDefault(EditProgIdx);
            bool isCoordPosn = (EditCoordSrcIdx == 0);
            bool isCoordValid = false;
            if (EditCoordIdx == 0)
                isCoordValid = config.PP.Stations[EditStationNum].PP[EditProgIdx].IsValid;
            else if (config.PP.Stations[EditStationNum].STP.Count >= EditCoordIdx)
                isCoordValid = config.PP.Stations[EditStationNum].STP[EditCoordIdx - 1].IsValid;
            bool isPosnEnabled = isEditable && isWeaponLoaded && isCoordPosn;
            Brush posnBrush = (isPosnEnabled) ? _brushEnabledText : _brushDisabledText;

            Utilities.SetEnableState(uiPoINameFilterBox, isPosnEnabled);
            Utilities.SetEnableState(uiPoIBtnFilter, isPosnEnabled);
            Utilities.SetEnableState(uiPoIBtnApply, isPosnEnabled && (CurSelectedPoI != null));
            Utilities.SetEnableState(uiPosnBtnCapture, isPosnEnabled && isDCSListening);
            uiPosnTextPoI.Foreground = posnBrush;

            uiPoIBtnFilter.IsChecked = (FilterSpec.IsFiltered && isEditable);

            uiCoordTextSrc.Foreground = (isWeaponLoaded) ? _brushEnabledText : _brushDisabledText;

            Utilities.SetEnableState(uiStationBtnPrev, (EditStationNum != 2));
            Utilities.SetEnableState(uiStationBtnNext, (EditStationNum != 8));
            Utilities.SetEnableState(uiStationBtnUnload, isEditable && isWeaponLoaded);

            Utilities.SetEnableState(uiProgBtnPrev, isWeaponLoaded && (EditProgIdx > 0));
            Utilities.SetEnableState(uiProgBtnNext, isWeaponLoaded && (EditProgIdx < (uiProgSelectCombo.Items.Count - 1)));
            Utilities.SetEnableState(uiProgSelectCombo, isEditable && isWeaponLoaded);
            Utilities.SetEnableState(uiProgCkbxBoxed, isEditable && isWeaponLoaded && !isProgDefault);
            Utilities.SetEnableState(uiProgBtnReset, isEditable && isWeaponLoaded && !isProgDefault);

            if (isSLAMER)
            {
                uiCoordBtnPrev.Visibility = Visibility.Visible;
                uiCoordBtnNext.Visibility = Visibility.Visible;
                Utilities.SetEnableState(uiCoordBtnPrev, (EditCoordIdx > 0));
                Utilities.SetEnableState(uiCoordBtnNext, (EditCoordIdx < (uiCoordSelectCombo.Items.Count - 1)));
            }
            else
            {
                uiCoordBtnPrev.Visibility = Visibility.Collapsed;
                uiCoordBtnNext.Visibility = Visibility.Collapsed;
            }
            Utilities.SetEnableState(uiCoordSelectCombo, isEditable && isSLAMER);
            Utilities.SetEnableState(uiCoordSrcSelectCombo, isEditable && isWeaponLoaded && isWyptAvailable);
            Utilities.SetEnableState(uiCoordBtnDelete, isEditable && isWeaponLoaded && isCoordValid);

            Utilities.SetEnableState(uiPosnValueName, isPosnEnabled);
            Utilities.SetEnableState(uiPosnValueLat, isPosnEnabled);
            Utilities.SetEnableState(uiPosnValueLon, isPosnEnabled);
            Utilities.SetEnableState(uiPosnValueAlt, isPosnEnabled);
            uiPosnTextLocn.Foreground = posnBrush;
            uiPosnTextName.Foreground = posnBrush;
            uiPosnTextAltUnits.Foreground = posnBrush;
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            uiCoordBtnDeleteTitle.Text = (uiCoordSelectCombo.SelectedIndex == 0) ? "Clear" : "Delete";
            uiProgCkbxBoxed.IsChecked = (EditProgIdx == (EditBoxedPPNum - 1));
            RebuildStationSelectMenu();
            RebuildProgramSelectMenu();
            RebuildCoordSelectMenu();
            RebuildEnableState();
        }

        protected override void ResetConfigToDefault()
        {
            ((FA18CConfiguration)Config).PP.Reset();

            StartUIRebuild();
            uiStationSelectCombo.SelectedIndex = 0;
            FinishUIRebuild();

            EditStationNum = 2;
            EditProgIdx = 0;
            EditCoordIdx = 0;
            EditCoordSrcIdx = 0;
            CopyConfigToEditState();
            ResetComboSelectionState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- component delete buttons ------------------------------------------------------------------------------

        /// <summary>
        /// weapon unload click: unload the weapon from the current station on confirmation from the user.
        /// </summary>
        private async void StationBtnUnload_Click(object sender, RoutedEventArgs args)
        {
            await CoreUnloadWeaponUI();
        }

        /// <summary>
        /// program reset button click: reset the currently selected program restoring it to its default state.
        /// </summary>
        private async void ProgBtnReset_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Program?",
                $"Are you sure you want to reset PP{EditProgIdx + 1} for the weapon on station {EditStationNum}?" +
                $" This will clear this program for the weapon leaving any station steerpoints untouched. This" +
                $" action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                ((FA18CConfiguration)Config).PP.Stations[EditStationNum].PP[EditProgIdx].Reset();
                CopyConfigToEditState();
                if ((EditBoxedPPNum - 1) == EditProgIdx)
                {
                    EditBoxedPPNum = 0;
                    uiProgCkbxBoxed.IsChecked = false;
                }
                EditCoordIdx = 0;
                EditCoordSrcIdx = 0;
                SaveEditStateToConfig();
                ResetComboSelectionState();
            }
        }

        /// <summary>
        /// coordinate clear/delete button click: clear out the currently selected coordinate.
        /// </summary>
        private async void CoordBtnDelete_Click(object sender, RoutedEventArgs args)
        {
            string auxMsg = "";
            if ((EditCoordIdx == 0) && ((EditWeapon == Weapons.SLAM) || (EditWeapon == Weapons.SLAM_ER)))
                auxMsg = " This will also remove all defined STP coordinates.";

            string action = uiCoordBtnDeleteTitle.Text;
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                $"{action} Coordiante?",
                $"Are you sure you want to {action.ToLower()} this coordiante?{auxMsg} This action cannot be undone.",
                action
            );
            if (result == ContentDialogResult.Primary)
            {
                EditCoordInfo.Reset();
                EditCoordInfo.WaypointNumber = -1;
                //
                // NOTE: SaveEditStateToConfig takes care of deleting/clearing coordinate at EditCoordIdx when
                // NOTE: WaypointNumber is -1
                // 
                SaveEditStateToConfig();
                EditCoordIdx = EditCoordIdx - ((EditCoordIdx == 0) ? 0 : 1);
                CopyConfigToEditState();
                EditCoordSrcIdx = EditCoordInfo.WaypointNumber;
                ResetComboSelectionState();
            }
        }

        // ---- poi management ----------------------------------------------------------------------------------------

        /// <summary>
        /// filter box focused: show the suggestion list when the control gains focus.
        /// </summary>
        private void PoINameFilterBox_GotFocus(object sender, RoutedEventArgs args)
        {
            AutoSuggestBox box = (AutoSuggestBox)sender;
            box.IsSuggestionListOpen = true;
        }

        /// <summary>
        /// filter box text changed: update the items in the search box based on the value in the field.
        /// </summary>
        private void PoINameFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                CurSelectedPoI = null;
                RebuildPointsOfInterest();
                RebuildEnableState();
            }
        }

        /// <summary>
        /// filter box query submitted: apply the query text filter to the pois listed in the poi list.
        /// </summary>
        private void PoINameFilterBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                CurSelectedPoI = (args.ChosenSuggestion as Base.PoIListItem).PoI;
            }
            else
            {
                CurSelectedPoI = null;
                foreach (Base.PoIListItem poi in uiPoINameFilterBox.ItemsSource as IEnumerable<Base.PoIListItem>)
                {
                    if (poi.Name == args.QueryText)
                    {
                        CurSelectedPoI = poi.PoI;
                        break;
                    }
                }
            }
            UpdateUIFromEditState();
        }

        /// <summary>
        /// filter button click: setup the filter setup.
        /// </summary>
        private async void PoIBtnFilter_Click(object sender, RoutedEventArgs args)
        {
            ToggleButton button = (ToggleButton)sender;
            PoIFilterSpec spec = await NavpointUIHelper.FilterSpecDialog(Content.XamlRoot, FilterSpec, button);
            if (spec != null)
            {
                FilterSpec = spec;
                button.IsChecked = FilterSpec.IsFiltered;

                Settings.LastStptFilterTheater = FilterSpec.Theater;
                Settings.LastStptFilterTags = FilterSpec.Tags;
                Settings.LastStptFilterIncludeTypes = FilterSpec.IncludeTypes;

                RebuildPointsOfInterest();
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// apply poi click: copy poi information into current coordinate and force source to "position".
        /// </summary>
        private void PoIBtnApply_Click(object sender, RoutedEventArgs args)
        {
            EditCoordInfo.WaypointNumber = 0;
            EditCoordInfo.Name = CurSelectedPoI.Name;
            EditCoordInfo.LatUI = Coord.ConvertFromLatDD(CurSelectedPoI.Latitude, LLFormat.DMDS_P2ZF);
            EditCoordInfo.LonUI = Coord.ConvertFromLonDD(CurSelectedPoI.Longitude, LLFormat.DMDS_P2ZF);
            EditCoordInfo.Alt = CurSelectedPoI.Elevation;
            EditCoordInfo.ClearErrors();

            uiCoordSrcSelectCombo.SelectedIndex = 0;

            SaveEditStateToConfig();
        }

        /// <summary>
        /// capture position click: launch the ui for the dcs coordinate capture and prepare to receive captures
        /// back from dcs.
        /// </summary>
        private async void PosnBtnCapture_Click(object sender, RoutedEventArgs args)
        {
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += PosnBtnCapture_PosnCaptureDataReceived;
            await Utilities.CaptureSingleDialog(Content.XamlRoot, "Position");
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= PosnBtnCapture_PosnCaptureDataReceived;

            UpdateUIFromEditState();
        }

        /// <summary>
        /// data received from the position capture dialog in dcs. update the coordinate information as appropriate
        /// with the captured information.
        /// </summary>
        private void PosnBtnCapture_PosnCaptureDataReceived(WyptCaptureData[] wypts)
        {
            if ((wypts.Length > 0) && !wypts[0].IsTarget)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    EditCoordInfo.WaypointNumber = 0;
                    EditCoordInfo.Name = "DCS Capture";
                    EditCoordInfo.LatUI = Coord.ConvertFromLatDD(wypts[0].Latitude, LLFormat.DMDS_P2ZF);
                    EditCoordInfo.LonUI = Coord.ConvertFromLonDD(wypts[0].Longitude, LLFormat.DMDS_P2ZF);
                    EditCoordInfo.Alt = wypts[0].Elevation.ToString();
                    EditCoordInfo.ClearErrors();
                });
            }
        }

        // ---- station selection -------------------------------------------------------------------------------------

        /// <summary>
        /// weapon previous click: advance to previous station. assume button is disabled to prevent under-indexing.
        /// </summary>
        private void StationBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            uiStationSelectCombo.SelectedIndex = uiStationSelectCombo.SelectedIndex - 1;
        }

        /// <summary>
        /// station next click: advance to next station. assume button is disabled to prevent over-indexing.
        /// </summary>
        private void StationBtnNext_Click(object sender, RoutedEventArgs args)
        {
            uiStationSelectCombo.SelectedIndex = uiStationSelectCombo.SelectedIndex + 1;
        }

        /// <summary>
        /// station select combo selection changed: switch to the selected station. the tag of the sender (a TextBlock)
        /// gives us the station number to select.
        /// </summary>
        private void StationSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if (!IsUIRebuilding && (item != null) && (item.Tag != null))
            {
                // NOTE: assume tag == station here...
                SelectStation(int.Parse((string)item.Tag));
            }
        }

        // ---- weapon selection --------------------------------------------------------------------------------------

        /// <summary>
        /// weapon select combo selection changed: switch to the selected weapon. the tag of the sender (a TextBlock)
        /// gives us the value of the Weapons enum we are switching to.
        /// 
        /// switching from any weapon to none will clear programming (with user approval). switching to any non-slam
        /// weapon will remove all coordinates except target. 
        /// </summary>
        private async void WeaponSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsUIRebuilding && (item != null) && (item.Tag != null))
            {
                Weapons weapon = (Weapons)int.Parse((string)item.Tag);
                if ((weapon != EditWeapon) && (weapon == Weapons.NONE))
                {
                    if (await CoreUnloadWeaponUI() != ContentDialogResult.Primary)
                    {
                        StartUIRebuild();
                        uiWeaponSelectCombo.SelectedIndex = (int)EditWeapon;
                        FinishUIRebuild();
                    }
                }
                else if (weapon != EditWeapon)
                {
                    EditWeapon = weapon;
                    //
                    // NOTE: SaveEditStateToConfig removes Coords[] elements not supported by the weapon.
                    //
                    SaveEditStateToConfig();
                    RebuildCoordSelectMenu();
                }
            }
        }

        // ---- program selection -------------------------------------------------------------------------------------

        /// <summary>
        /// program previous click: advance to previous station program. assume button is disabled to prevent
        /// under-indexing.
        /// </summary>
        private void ProgBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            uiProgSelectCombo.SelectedIndex = uiProgSelectCombo.SelectedIndex - 1;
        }

        /// <summary>
        /// program next click: advance to next station program. assume button is disabled to prevent over-indexing.
        /// </summary>
        private void ProgBtnNext_Click(object sender, RoutedEventArgs args)
        {
            uiProgSelectCombo.SelectedIndex = uiProgSelectCombo.SelectedIndex + 1;
        }

        /// <summary>
        /// program select combo selection changed:
        /// </summary>
        private void ProgSelectCombo_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if (!IsUIRebuilding && (item != null) && (item.Tag != null))
            {
                // NOTE: assume tag == station here...
                SelectProgram(int.Parse((string)item.Tag));
            }
        }

        /// <summary>
        /// program load checkbox clicked: update the loaded program number state.
        /// </summary>
        private void ProgCkbxBoxed_Click(object sender, RoutedEventArgs args)
        {
            CheckBox ckbx = (CheckBox)sender;
            EditBoxedPPNum = (ckbx.IsChecked == true) ? (EditProgIdx + 1) : 0;
            SaveEditStateToConfig();
        }

        // ---- coordinate selection ----------------------------------------------------------------------------------

        /// <summary>
        /// coordinate previous click: advance to previous program coordinate. assume button is disabled to prevent
        /// under-indexing.
        /// </summary>
        private void CoordBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            uiCoordSelectCombo.SelectedIndex = uiCoordSelectCombo.SelectedIndex - 1;
        }

        /// <summary>
        /// coordinate next click: advance to next program coordinate. assume button is disabled to prevent
        /// over-indexing.
        /// </summary>
        private void CoordBtnNext_Click(object sender, RoutedEventArgs args)
        {
            uiCoordSelectCombo.SelectedIndex = uiCoordSelectCombo.SelectedIndex + 1;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void CoordSelectCombo_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsUIRebuilding && (item != null) && (item.Tag != null))
            {
                // NOTE: assume tag == index here...
                SelectCoord(int.Parse((string)item.Tag));
            }
        }

        // ---- coordinate source selection ---------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void CoordSrcSelectCombo_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsUIRebuilding && (item != null) && (item.Tag != null))
            {
                // NOTE: assume tag == index here...
                SelectCoordSource(int.Parse((string)item.Tag));
            }
        }

        // ---- coordinate text focus selection -----------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void CoordText_LostFocus(object sender, RoutedEventArgs args)
        {
            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                SaveEditStateToConfig();
            });
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            FilterSpec = new(Settings.LastStptFilterTheater, Settings.LastStptFilterCampaign,
                             Settings.LastStptFilterTags, Settings.LastStptFilterIncludeTypes);

            base.OnNavigatedTo(args);

            BuildCoordSourceSelectMenu();

            EditStationNum = 2;
            EditProgIdx = 0;
            EditCoordIdx = 0;
            CopyConfigToEditState();

            EditCoordSrcIdx = EditCoordInfo.WaypointNumber;
            uiStationSelectCombo.SelectedIndex = 0;
            ResetComboSelectionState();

            RebuildPointsOfInterest();

            base.OnNavigatedTo(args);
        }
    }
}
