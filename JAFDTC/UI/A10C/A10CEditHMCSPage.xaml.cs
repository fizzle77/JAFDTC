using JAFDTC.Models.A10C.DSMS;
using JAFDTC.UI.App;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class A10CEditHMCSPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(DSMSSystem.SystemTag, "HMCS", "HMCS", Glyphs.HMCS, typeof(A10CEditHMCSPage));

        public A10CEditHMCSPage()
        {
            this.InitializeComponent();
        }

    }
}
