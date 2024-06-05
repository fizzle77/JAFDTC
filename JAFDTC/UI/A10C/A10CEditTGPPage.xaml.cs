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
using JAFDTC.Models.A10C.TGP;
using JAFDTC.UI.App;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 TGP editor.
    /// </summary>
    public sealed partial class A10CEditTGPPage : A10CPageBase
    {
        private const string SYSTEM_NAME = "TGP";

        public override SystemBase SystemConfig => _config.TGP;

        public static ConfigEditorPageInfo PageInfo
            => new(TGPSystem.SystemTag, SYSTEM_NAME, SYSTEM_NAME, Glyphs.TGP, typeof(A10CEditTGPPage));

        public A10CEditTGPPage() : base(SYSTEM_NAME, TGPSystem.SystemTag)
        {
            InitializeComponent();
            InitializeBase(new TGPSystem(), uiTextLaserCode, uiCtlLinkResetBtns);
        }
    }
}
