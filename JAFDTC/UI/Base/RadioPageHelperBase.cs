// ********************************************************************************************************************
//
// RadioPageHelperBase.cs : base class for EditRadioPage helper classes
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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

using JAFDTC.Utilities;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// base class for a class implementing IEditRadioPageHelper to cover common method implementations for a subset
    /// of the IEditRadioHelper interface.
    /// </summary>
    internal abstract class RadioPageHelperBase
    {
        public virtual string RadioAux1Title(int radio) => null;

        public virtual string RadioAux2Title(int radio) => null;
        
        public virtual string RadioAux3Title(int radio) => null;

        public virtual bool RadioCanMonitorGuard(int radio) => false;

        public virtual int RadioMaxPresets(int radio) => 1;

        public virtual bool RadioCanProgramModulation(int radio) => false;
        
        public virtual List<TextBlock> RadioModulationItems(int radio, string freq) => null;

        public virtual bool ValidatePreset(int radio, string preset, bool isNoEValid = true)
            => BindableObject.IsIntegerFieldValid(preset, 1, RadioMaxPresets(radio), isNoEValid);

        public virtual bool ValidateFrequency(int radio, string freq, bool isNoEValid = true) => false;

        public virtual string ValidateDefaultTuning(int radio, string value, bool isNoEValid = true)
        {
            if (ValidateFrequency(radio, value, isNoEValid))
            {
                return FixupFrequency(radio, value);
            }
            else if (ValidatePreset(radio, value, isNoEValid))
            {
                return value;
            }
            return null;
        }

        public virtual string FixupPreset(int radio, string preset)
            => BindableObject.FixupIntegerField(preset);

        public virtual string FixupFrequency(int radio, string freq)
            => BindableObject.FixupDecimalField(freq, "F3");
    }
}
