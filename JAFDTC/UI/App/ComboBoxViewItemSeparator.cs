// ********************************************************************************************************************
//
// ComboBoxViewItemSeparator.cs -- custom NavigationViewItemSeparator class for separators in ComboBoxSeperated.
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

using Microsoft.UI.Xaml.Controls;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// item separator suitable for use by ComboBoxSeparated. implements ToString() to preserve IsTextSearchEnabled
    /// behavior in the ComboBoxSeparated.
    /// </summary>
    public class ComboBoxViewItemSeparator : NavigationViewItemSeparator
    {
        public override string ToString() => "??";
    }
}
