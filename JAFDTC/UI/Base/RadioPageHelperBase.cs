using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.UI.Base
{
    internal abstract class RadioPageHelperBase
    {
        public virtual string RadioAux1Title(int radio) => null;

        public virtual string RadioAux2Title(int radio) => null;
        
        public virtual string RadioAux3Title(int radio) => null;
        
        public virtual string RadioAux4Title(int radio) => null;

        public virtual bool RadioCanProgramModulation(int radio) => false;
        
        public virtual List<TextBlock> RadioModulationItems(int radio, string freq) => null;
    }
}
