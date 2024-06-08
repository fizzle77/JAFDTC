// ********************************************************************************************************************
//
// A10CEditTGPPage.xaml.cs : ui c# for warthog tgp page
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

using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.TGP;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 TGP editor.
    /// </summary>
    public sealed partial class A10CEditTGPPage : SystemEditorPageBase
    {
        private const string SYSTEM_NAME = "TGP";

        protected override SystemBase SystemConfig => ((A10CConfiguration)Config).TGP;
        protected override string SystemTag => TGPSystem.SystemTag;
        protected override string SystemName => SYSTEM_NAME;

        public static ConfigEditorPageInfo PageInfo
            => new(TGPSystem.SystemTag, SYSTEM_NAME, SYSTEM_NAME, Glyphs.TGP, typeof(A10CEditTGPPage));

        public A10CEditTGPPage()
        {
            InitializeComponent();
            InitializeBase(new TGPSystem(), uiTextLaserCode, uiCtlLinkResetBtns);
        }
    }
}
