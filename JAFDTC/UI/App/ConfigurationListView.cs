// ********************************************************************************************************************
//
// ConfigurationListView.cs -- custom ListView class for the configuration list
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

using JAFDTC.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using Windows.UI;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// custom ListView class for the configuration list. we use TextBlocks with icon strings to identify updated
    /// systems. a second TextBox with icon strings overlays the icons to provide a badge. becuase segoe fluent
    /// icons doesn't have a full-width empty glyph, when we have no badge, we have to use full-width non-blank
    /// glyph that we hide by using a transparent TextHighlighter. as TextHighlighters is not a bindable property
    /// (near as i can tell), we hook the Loaded event for the ItemViewItem and PrepareContainerForItemOverride()
    /// to set up the highlighters based on content.
    /// 
    /// HACK: this is a total hack that stems from limited winui experience. for example, could use a custom control
    /// HACK: for the icons rather than TextBlocks, but that would require figuring out custom controls. The
    /// HACK: customization necessary is straight-forward in iOS, have to figure there is some easier way to do this
    /// HACK: that i'm simply unaware of. c'est la vie.
    /// </summary>
    public class ConfigurationListView : ListView
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly string _badgeTag = "uiCfgListViewItemBadges";

        // ------------------------------------------------------------------------------------------------------------
        //
        // support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// build the TextHighlighter state to show the badges. badges are taken either from the text block
        /// text or the configuration badges state.
        /// </summary>
        private static void SetupTblockHighlightsToBadge(TextBlock txtBlock, IConfiguration config = null)
        {
            if (txtBlock != null)
            {
                string badges = (config != null) ? config.UpdatesIconBadgesUI : txtBlock.Text;
                txtBlock.TextHighlighters.Clear();
                for (int index = 0; index < badges.Length; index++)
                {
                    if (badges[index] == JAFDTC.UI.Glyphs.Badge[0])
                    {
                        TextHighlighter highlighter = new()
                        {
                            Background = new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)),   // Transparent
                            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xB8, 0x86, 0x0B))    // DarkGoldenrod
                        };
                        TextRange txtRange = new()
                        {
                            StartIndex = index,
                            Length = 1
                        };
                        highlighter.Ranges.Add(txtRange);
                        txtBlock.TextHighlighters.Add(highlighter);
                    }
                }
            }
        }

        /// <summary>
        /// ListViewItem loaded: update the text highlighters in the text block that holds badges (tagged with
        /// _badgeTag) to show the badges.
        /// </summary>
        private void CfgListViewItem_Loaded(object sender, RoutedEventArgs args)
        {
            ListViewItem listViewItem = (ListViewItem)sender;
            TextBlock txtBlock = Utilities.FindControl<TextBlock>(listViewItem, typeof(TextBlock), _badgeTag) as TextBlock;
            SetupTblockHighlightsToBadge(txtBlock);
        }

        /// <summary>
        /// prepare the item container for an item by updating the text highlighters in the text block that holds
        /// badges (tagged with _badgeTag) to show the badges. install the CfgListViewItem_Loaded handler for the
        /// loaded event in the item container.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            ListViewItem listViewItem = (ListViewItem)element;
            listViewItem.Loaded += CfgListViewItem_Loaded;
            TextBlock txtBlock = Utilities.FindControl<TextBlock>(listViewItem, typeof(TextBlock), _badgeTag) as TextBlock;
            SetupTblockHighlightsToBadge(txtBlock, (IConfiguration)item);
        }
    }
}
