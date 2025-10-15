// ********************************************************************************************************************
//
// Utilities.cs : general user interface utility functions
//
// Copyright(C) 2023-2025 ilominar/raven
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
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace JAFDTC.UI
{
    /// <summary>
    /// General (somewhat) helpful user interface utilities.
    /// </summary>
    internal class Utilities
    {
        // TODO: DEPRECATE
        private static readonly Regex regexInts = new("^[\\-]{0,1}[0-9]*$");
        // TODO: DEPRECATE
        private static readonly Regex regexTwoNegs = new("[^\\-]*[\\-][^\\-]*[\\-].*");

        // ------------------------------------------------------------------------------------------------------------
        //
        // dispatch queue support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// schedule a lambda on a dispatch queue to fire one or more times. 
        /// </summary>
        public static void DispatchAfterDelay(Microsoft.UI.Dispatching.DispatcherQueue queue, double seconds, bool repeats,
                                              TypedEventHandler<Microsoft.UI.Dispatching.DispatcherQueueTimer, object> lambda)
        {
            Microsoft.UI.Dispatching.DispatcherQueueTimer timer = queue.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(seconds);
            timer.IsRepeating = repeats;
            timer.Tick += lambda;
            timer.Start();
        }

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
        // combobox support
        //
        // ------------------------------------------------------------------------------------------------------------

        public static TextBlock TextComboBoxItem(string text, string tagItem)
            => new() { Text = text, Tag = tagItem };

        /// <summary>
        /// returns an instance of Grid for use as a ComboBoxItem. the item consists of a FontIcon bullet (Tag of
        /// "BadgeIcon") with a TextBlock text element (Tag of "ItemText") to its right. parameters set the text and
        /// tag of the Grid.
        /// </summary>
        public static Grid BulletComboBoxItem(string text, string tagItem)
        {
            // OMFG, i so *HATE* WinUI for making even the simple stuff non-intuitive. there seems to be ways to do
            // everything but they are opaque and mysterious and the obvious path never seems to completely work.
            //
            // this sh!t is due to issues getting DataTemplate on ComboBox to show both in the closed as well as
            // open states. there's stackoverflow goodness on a fix, but instructions are incomplete and beyond my
            // meager knowledge. and let's not even get started on the asymmetries in C# vs XAML.
            //
            // so instead we get this: making the xaml reader slurp up some xaml as a hack.
            //
            // can we build jafdtc in UIKit? please?
            //
            string xaml = $"<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"" +
                          $"      Tag=\"{tagItem}\">" +
                          $"  <Grid.ColumnDefinitions>" +
                          $"    <ColumnDefinition Width=\"20\"/>" +
                          $"    <ColumnDefinition Width=\"Auto\"/>" +
                          $"  </Grid.ColumnDefinitions>" +
                          $"  <FontIcon Grid.Column=\"0\"" +
                          $"            VerticalAlignment=\"Center\"" +
                          $"            Tag=\"BadgeIcon\"" +
                          $"            Foreground=\"{{ThemeResource SystemAccentColor}}\"" +
                          $"            FontFamily=\"Segoe Fluent Icons\"" +
                          $"            Glyph=\"\xE915\"/>" +
                          $"  <TextBlock Grid.Column=\"1\"" +
                          $"             Margin=\"8,0,0,0\"" +
                          $"             VerticalAlignment=\"Center\"" +
                          $"             Tag=\"ItemText\"" +
                          $"             Text = \"{text}\"/>" +
                          $"</Grid>";
            return (Grid)XamlReader.Load(xaml);
        }

        /// <summary>
        /// handle visiblity of the bullets in a ComboBox that uses bullet items built by BulletComboBoxItem() or
        /// items that are simillarly structured and tagged. the bullets are set according to the return value of
        /// fnIsBulletAtIndexVisible(i) : true => visible, where i is the index of the item in the menu.
        /// </summary>
        public static void SetBulletsInBulletComboBox(ComboBox combo, Func<int, bool> fnIsBulletAtIndexVisible)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                FontIcon icon = FindControl<FontIcon>((Grid)combo.Items[i], typeof(FontIcon), "BadgeIcon");
                icon.Visibility = (fnIsBulletAtIndexVisible(i) ? Visibility.Visible : Visibility.Collapsed);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // keyboard helpers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns true if the requested modifier key is down, false otherwise.
        /// </summary>
        public static bool IsModifierKeyDown(CoreVirtualKeyStates leftKeyState, CoreVirtualKeyStates rightKeyState)
        {
            // Odd that this is so convoluted, but checking for (Down | Locked) does appear necessary to reliably
            // detect key state.
            // Doc source: https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-accelerators#override-default-keyboard-behavior

            if (leftKeyState == CoreVirtualKeyStates.Down || leftKeyState == (CoreVirtualKeyStates.Down |
                                                                              CoreVirtualKeyStates.Locked))
                return true;
            if (rightKeyState == CoreVirtualKeyStates.Down || rightKeyState == (CoreVirtualKeyStates.Down |
                                                                                CoreVirtualKeyStates.Locked))
                return true;
            return false;
        }

        /// <summary>
        /// return the current shift and control modifier state.
        /// </summary>
        public static void GetModifierKeyStates(out bool isShiftDown, out bool isCtrlDown)
        {
            CoreVirtualKeyStates leftKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftShift);
            CoreVirtualKeyStates rightKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightShift);
            isShiftDown = IsModifierKeyDown(leftKeyState, rightKeyState);

            leftKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftControl);
            rightKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightControl);
            isCtrlDown = IsModifierKeyDown(leftKeyState, rightKeyState);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// search the visual tree of a parent for a child control of a particular type with a given tag. returns the
        /// control or null if not found.
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
                        return FindControl<T>(child, targetType, tag);
                }
            }
            return result;
        }

        /// <summary>
        /// Find all descendant controls of the specified type in the visual tree.
        /// </summary>
        public static void FindDescendantControls<T>(List<T> results, DependencyObject startNode)
          where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(startNode);
            for (int i = 0; i < count; i++)
            {
                DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
                if (current is T asType)
                    results.Add(asType);
                FindDescendantControls<T>(results, current);
            }
        }
        
        /// <summary>
        /// truncate a string at the nearest word boundary to the indicated length. the string is suffixed with "...".
        /// </summary>
        public static string TruncateAtWord(string input, int length)
        {
            if ((input == null) || (input.Length < length))
                return input;

            int iNextSpace = input.LastIndexOf(' ', length);
            return string.Format("{0}...", input[..((iNextSpace > 0) ? iNextSpace : length)].Trim());
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

        /// <summary>
        /// return visibility state based on whether or not an ISystem is default or not. returns visible if the
        /// system is default, collapsed otherwise.
        /// </summary>
        public static Visibility HiddenIfDefault(ISystem system)
            => ((system == null) || system.IsDefault) ? Visibility.Collapsed : Visibility.Visible;

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

        // TODO: DEPRECATE
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

        public static void SetComboEnabledAndSelection(ComboBox combo, bool isNotLinked, bool isEnabled,
                                                       int selectedIndex)
        {
            if (!isEnabled)
            {
                SetEnableState(combo, false);               // always disable if set to disable
                combo.SelectedItem = null;                  // always blank if disabled
            }
            else
            {
                SetEnableState(combo, isNotLinked);         // otherwise disable if linked
                if (combo.SelectedIndex != selectedIndex)
                    combo.SelectedIndex = selectedIndex;    // show setting if enabled or linked (if selection changed)
            }
        }

        public static void SetTextBoxEnabledAndText(TextBox tb, bool isNotLinked, bool isEnabled, string text,
                                                    string disabledText = null)
        {
            if (!isEnabled)
            {
                SetEnableState(tb, false);                  // always disable if set to disable
                tb.Text = disabledText;                     // always the disabledText if disabled
            }
            else
            {
                SetEnableState(tb, isNotLinked);            // otherwise disable if linked
                tb.Text = text;                             // show setting if enabled or linked
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // link/unlink button support
        //
        // ------------------------------------------------------------------------------------------------------------

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
                        config = (uidToConfigMap.TryGetValue(linkUID, out IConfiguration value)) ? value : null;
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
            if (string.IsNullOrEmpty(linkedUID) || !uidToConfigMap.TryGetValue(linkedUID, out IConfiguration value))
            {
                uiPageBtnTxtLink.Text = "Link To...";
                uiPageTxtLink.Text = "";
                config.UnlinkSystem(systemTag);
            }
            else
            {
                uiPageBtnTxtLink.Text = "Unlink From";
                uiPageTxtLink.Text = TruncateAtWord(value.Name, 46);
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
