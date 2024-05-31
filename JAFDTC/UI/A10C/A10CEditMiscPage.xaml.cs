// ********************************************************************************************************************
//
// A10CEditMiscPage.xaml.cs : ui c# for warthog miscellaneous page
//
// Copyright(C) 2024 fizzle
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

using JAFDTC.UI.App;
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.Misc;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 Miscellaneous editor.
    /// </summary>
    public sealed partial class A10CEditMiscPage : A10CPageBase
    {
        private const string SYSTEM_NAME = "Miscellaneous";

        public override A10CSystemBase SystemConfig => _config.Misc;

        public static ConfigEditorPageInfo PageInfo
            => new(MiscSystem.SystemTag, SYSTEM_NAME, SYSTEM_NAME, Glyphs.MISC, typeof(A10CEditMiscPage));

        public A10CEditMiscPage() : base(SYSTEM_NAME, MiscSystem.SystemTag)
        {
            InitializeComponent();
            InitializeBase(new MiscSystem(), uiTextTACANChannel, uiCtlLinkResetBtns);
        }
    }
}
