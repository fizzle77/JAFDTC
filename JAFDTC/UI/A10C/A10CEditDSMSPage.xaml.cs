using JAFDTC.UI.App;
using JAFDTC.Models.A10C.DSMS;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using JAFDTC.Models.A10C;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class A10CEditDSMSPage : Page
    {
        private ConfigEditorPageNavArgs _navArgs;
        private A10CConfiguration _config;

        public static ConfigEditorPageInfo PageInfo
            => new(DSMSSystem.SystemTag, "DSMS", "DSMS", Glyphs.DSMS, typeof(A10CEditDSMSPage));

        public A10CEditDSMSPage()
        {
            InitializeComponent();
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (ReferenceEquals(args.SelectedItem, uiMunitionTab))
                DSMSContentFrame.Navigate(typeof(A10CEditDSMSMunitionSettingsPage), _navArgs);
            else if (ReferenceEquals(args.SelectedItem, uiProfileTab))
                DSMSContentFrame.Navigate(typeof(A10CEditDSMSProfileOrderPage), _navArgs);
            else
                throw new ApplicationException("Unexpected NavigationViewItem type");
        }

        private void PageBtnLink_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {

        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _config = (A10CConfiguration)_navArgs.Config;
            base.OnNavigatedTo(args);
        }

    }
}
