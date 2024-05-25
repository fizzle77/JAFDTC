// ********************************************************************************************************************
//
// Utilities.cs : general user interface utility functions
//
// Copyright(C) 2023-2024 ilominar/raven
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

using JAFDTC.Models;
using JAFDTC.UI.App;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using System.Reflection;
using JAFDTC.Models.A10C.HMCS;

namespace JAFDTC.UI
{
    /// <summary>
    /// General (somewhat) helpful user interface utilities.
    /// </summary>
    internal class Utilities
    {
        /// TODO: DEPRECATE
        private static readonly Regex regexInts = new("^[\\-]{0,1}[0-9]*$");
        private static readonly Regex regexTwoNegs = new("[^\\-]*[\\-][^\\-]*[\\-].*");

        // ------------------------------------------------------------------------------------------------------------
        //
        // basic generic dialogs
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a ContentDialog dialog with an single button ready for presentation via await.
        /// </summary>
        public static IAsyncOperation<ContentDialogResult> Message1BDialog(XamlRoot root, string title,
                                                                           string message, string button = "OK")
        {
            return new ContentDialog()
            {
                XamlRoot = root,
                Title = title,
                Content = message,
                PrimaryButtonText = button,
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync(ContentDialogPlacement.Popup);
        }

        /// <summary>
        /// return a ContentDialog dialog with two buttons ready for presentation via await.
        /// </summary>
        public static IAsyncOperation<ContentDialogResult> Message2BDialog(XamlRoot root, string title,
                                                                           string message, string btnPrimary = "OK",
                                                                           string btnClose = "Cancel")
        {
            return new ContentDialog()
            {
                XamlRoot = root,
                Title = title,
                Content = message,
                PrimaryButtonText = btnPrimary,
                CloseButtonText = btnClose,
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync(ContentDialogPlacement.Popup);
        }

        /// <summary>
        /// return a ContentDialog dialog with three buttons ready for presentation via await.
        /// </summary>
        public static IAsyncOperation<ContentDialogResult> Message3BDialog(XamlRoot root, string title,
                                                                           string message, string btnPrimary = "OK",
                                                                           string btnSecondary = "",
                                                                           string btnClose = "Cancel")
        {
            return new ContentDialog()
            {
                XamlRoot = root,
                Title = title,
                Content = message,
                PrimaryButtonText = btnPrimary,
                SecondaryButtonText = btnSecondary,
                CloseButtonText = btnClose,
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync(ContentDialogPlacement.Popup);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // common dialogs
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a ContentDialog with two buttons to request whether a dcs capture should append or update
        /// navigation points. the primary button is "create", close is "update. the what parameter should be
        /// capitalized and singular.
        /// </summary>
        public static IAsyncOperation<ContentDialogResult> CaptureActionDialog(XamlRoot root, string what)
        {
            return Message2BDialog(
                root,
                $"Capture {what}s from DCS",
                $"Would you like to create new {what.ToLower()}s from the coordinates captured from DCS and append " +
                $"them to the end of the {what.ToLower()} list or update coordinates in existing {what.ToLower()}s " +
                $"starting from the current selection (adding new {what.ToLower()}s as necessary)?",
                $"Create & Append",
                $"Update");
        }

        /// <summary>
        /// return a ContentDialog dialog with one button set up to tell the user a capture of a single navigation
        /// point is in flight. the what parameter should be capitalized and singular.
        /// </summary>
        public static IAsyncOperation<ContentDialogResult> CaptureSingleDialog(XamlRoot root, string what)
        {
            return Message1BDialog(
                root,
                $"Capturing {what} from DCS",
                $"The coordinates of this {what.ToLower()} will be set from the first captured location.\n\n" +
                $"In DCS, type [CTRL]-[SHIFT]-J on the F10 map to show the coordinate capture dialog, move " +
                $"the F10 map until the crosshair is over the desired point, click “Add”, and then click “Send”. " +
                $"Typing [CTRL]-[SHIFT]-J again will dismiss the capture dialog.\n\n" +
                $"Click “Done” below when finished capturing.",
                "Done");
        }

        /// <summary>
        /// return a ContentDialog dialog with one button set up to tell the user a capture of multiple navigation
        /// points is in flight. the what parameter should be capitalized and singular.
        /// </summary>
        public static IAsyncOperation<ContentDialogResult> CaptureMultipleDialog(XamlRoot root, string what)
        {
            return Message1BDialog(
                root,
                $"Capturing {what}s from DCS",
                $"The coordinates of the effected {what.ToLower()}s will be set from captured locations.\n\n" +
                $"In DCS, type [CTRL]-[SHIFT]-J on the F10 map to show the coordinate capture dialog, move " +
                $"the F10 map until the crosshair is over the desired point, click “Add”, repeat this process " +
                $"for other {what}s and then click “Send”. Typing [CTRL]-[SHIFT]-J again will dismiss the " +
                $"capture dialog.\n\n" +
                $"Click “Done” below when finished capturing.",
                "Done");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// search the visual tree for a child control of a particular type with a given tag. returns the control
        /// or null if not found.
        /// 
        /// hat tip to:
        /// https://stackoverflow.com/questions/38110972/how-to-find-a-control-with-a-specific-name-in-an-xaml-ui-with-c-sharp-code
        /// </summary>
        public static T FindControl<T>(UIElement parent, Type targetType, object tag) where T : FrameworkElement
        {
            T result = null;
            if ((parent != null) && (parent.GetType() == targetType) && tag.Equals(((T)parent).Tag))
            {
                result = parent as T;
            }
            else if (parent != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    UIElement child = VisualTreeHelper.GetChild(parent, i) as UIElement;
                    if (FindControl<T>(child, targetType, tag) != null)
                    {
                        return FindControl<T>(child, targetType, tag);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Find all child controls of the specified type.
        /// </summary>
        public static void FindChildren<T>(List<T> results, DependencyObject startNode)
          where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(startNode);
            for (int i = 0; i < count; i++)
            {
                DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
                if (current is T)
                {
                    T asType = (T)current;
                    results.Add(asType);
                }
                FindChildren<T>(results, current);
            }
        }
        
        /// <summary>
        /// truncate a string at the nearest word boundary to the indicated length. the string is suffixed with "...".
        /// </summary>
        public static string TruncateAtWord(string input, int length)
        {
            if ((input == null) || (input.Length < length))
            {
                return input;
            }

            int iNextSpace = input.LastIndexOf(" ", length);
            return string.Format("{0}...", input[..((iNextSpace > 0) ? iNextSpace : length)].Trim());
        }

        /// <summary>
        /// build out the names list and name to uid map for the configurations that can be linked to from the
        /// configuration with the given id. a configuration may not link to itself or a configuration that
        /// creates a loop (e.g., for config a, b is an invalid target if b links to a). on return the name
        /// list and uid map are updated appropriately.
        /// </summary>
        public static void BuildSystemLinkLists(Dictionary<string, IConfiguration> uidToConfigMap,
                                                string myUID, string systemTag,
                                                List<string> names, Dictionary<string, string> nametoUIDMap)
        {
            nametoUIDMap.Clear();
            names.Clear();
            foreach (KeyValuePair<string, IConfiguration> kvp in uidToConfigMap)
            {
                IConfiguration config = kvp.Value;
                if (config.UID != myUID)
                {
                    while (config.SystemLinkedTo(systemTag) != null)
                    {
                        string linkUID = config.SystemLinkedTo(systemTag);
                        config = (uidToConfigMap.ContainsKey(linkUID)) ? uidToConfigMap[linkUID] : null;
                        if ((config == null) || (config.UID == myUID))
                        {
                            config = null;
                            break;
                        }
                    }
                    if (config != null)
                    {
                        nametoUIDMap[kvp.Value.Name] = kvp.Key;
                        names.Add(kvp.Value.Name);
                    }
                }
            }
            names.Sort();
        }

        /// TODO: DEPRECATE
        /// <summary>
        /// on changes to an integer parameter field in a TextBox (via typing or paste), remove non-integer
        /// characters so the field contains an integer.
        /// </summary>
        public static void TextBoxIntValue_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            var curPosition = sender.SelectionStart;
            if (!regexInts.IsMatch(sender.Text))
            {
                for (var match = Regex.Match(sender.Text, @"[^0-9\\-]");
                     match.Success;
                     match = Regex.Match(sender.Text, @"[^0-9\\-]"))
                {
                    sender.Text = sender.Text.Remove(match.Index, 1);
                    curPosition--;
                    sender.Select(curPosition, 0);
                }
            }
            for (var match = Regex.Match(sender.Text, @"[\\-]");
                 match.Success && regexTwoNegs.IsMatch(sender.Text);
                 match = Regex.Match(sender.Text, @"[\\-]"))
            {
                sender.Text = sender.Text.Remove(match.Index, 1);
                curPosition--;
                sender.Select(curPosition, 0);
            }
        }

        public static Visibility HiddenIfDefault(ISystem profileSettings) => profileSettings.IsDefault switch
        {
            true => Visibility.Collapsed,
            false => Visibility.Visible
        };

        /// <summary>
        /// set the enable state of a control, allowing the control to maintain focus.
        /// 
        /// HACK: winui appears to move focus to the next control when a control is disabled while it has focus.
        /// HACK: temporarily allow the control to have focus while disabled as we change its IsEnabled state.
        /// </summary>
        public static void SetEnableState(Control cntrl, bool isEnabled)
        {
            cntrl.AllowFocusWhenDisabled = true;
            cntrl.IsEnabled = isEnabled;
            cntrl.AllowFocusWhenDisabled = false;
        }

        public static void SetEnableState(bool isEnabled, params Control[] cntrls)
        {
            foreach (Control cntrl in cntrls) 
                SetEnableState(cntrl, isEnabled);
        }

        public static void SetCheckEnabledAndState(CheckBox check, bool isEnabled, bool isChecked)
        {
            SetEnableState(check, isEnabled);
            check.IsChecked = isChecked;
        }

        public static void SetComboEnabledAndSelection(ComboBox combo, bool isNotLinked, bool isEnabled, int selectedIndex)
        {
            if (!isEnabled)
            {
                SetEnableState(combo, false); // always disable if set to disable
                combo.SelectedItem = null; // always blank if disabled
            }
            else
            {
                SetEnableState(combo, isNotLinked); // otherwise disable if linked
                combo.SelectedIndex = selectedIndex; // show setting if enabled or linked
            }
        }

        public static void SetTextBoxEnabledAndText(TextBox tb, bool isNotLinked, bool isEnabled, string text, string disabledText = null)
        {
            if (!isEnabled)
            {
                SetEnableState(tb, false); // always disable if set to disable
                tb.Text = disabledText; // always the disabledText if disabled
            }
            else
            {
                SetEnableState(tb, isNotLinked); // otherwise disable if linked
                tb.Text = text; // show setting if enabled or linked
            }
        }

        /// <summary>
        /// rebuild the "link"/"unlink" page buttons based on the state of the link. if the button is set up to
        /// unlink, button title is "unlink" with text containing the name of the linked configuration. if button is
        /// set up to link, button title is "link" with text containing "".
        /// </summary>
        public static void RebuildLinkControls(IConfiguration config, string systemTag,
                                               Dictionary<string, IConfiguration> uidToConfigMap,
                                               TextBlock uiPageBtnTxtLink, TextBlock uiPageTxtLink)
        {
            string linkedUID = config.SystemLinkedTo(systemTag);
            if (string.IsNullOrEmpty(linkedUID) || !uidToConfigMap.ContainsKey(linkedUID))
            {
                uiPageBtnTxtLink.Text = "Link To...";
                uiPageTxtLink.Text = "";
                config.UnlinkSystem(systemTag);
            }
            else
            {
                uiPageBtnTxtLink.Text = "Unlink From";
                uiPageTxtLink.Text = TruncateAtWord(uidToConfigMap[linkedUID].Name, 46);
            }
        }

        /// <summary>
        /// click on a page "link"/"unlink" button. when in state to link, put up the "select a configuration to
        /// link" dialog to select a source configuration. returns the name of the selected configuration, or
        /// null if the dialog was canceled or the button was in a state to unlink.
        /// </summary>
        public static async Task<string> PageBtnLink_Click(XamlRoot root, IConfiguration config,
                                                           string systemTag, List<string> configNameList)
        {
            if (string.IsNullOrEmpty(config.SystemLinkedTo(systemTag)))
            {
                GetListDialog dialog = new(configNameList, "Configuration")
                {
                    XamlRoot = root,
                    Title = "Select a Configuration to Link",
                    PrimaryButtonText = "Link",
                    CloseButtonText = "Cancel",
                };
                return (await dialog.ShowAsync() == ContentDialogResult.Primary) ? dialog.SelectedItem : "";
            }
            return null;
        }
    }
}
