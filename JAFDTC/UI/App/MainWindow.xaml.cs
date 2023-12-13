// ********************************************************************************************************************
//
// MainWindow.xaml.cs -- ui c# for main window
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

using JAFDTC.Utilities;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// main window for jafdtc.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // windoze interfaces & data structs
        //
        // ------------------------------------------------------------------------------------------------------------

        private delegate IntPtr WinProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        internal static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [Flags]
        private enum WindowLongIndexFlags : int
        {
            GWL_WNDPROC = -4,
        }

        private enum WindowMessage : int
        {
            WM_GETMINMAXINFO = 0x0024,
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public ConfigurationListPage ConfigListPage => (ConfigurationListPage)uiAppContentFrame.Content;

        // ---- internal properties

        private static WinProc _newWndProc = null;
        private static IntPtr _oldWndProc = IntPtr.Zero;

        private static int _minWindowWidth = 1000;
        private static int _maxWindowWidth = 1800;
        private static int _minWindowHeight = 700;
        private static int _maxWindowHeight = 1600;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();

            Title = "JAFDTC";

            SystemBackdrop = new DesktopAcrylicBackdrop();
            // SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };
            uiAppTitleBar.Loaded += AppTitleBar_Loaded;
            uiAppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(uiAppTitleBar);

            var hWnd = GetWindowHandleForCurrentWindow(this);

            var scalingFactor = (float)GetDpiForWindow(hWnd) / 96;
            SizeInt32 baseSize;
            baseSize.Height = (int)(700 * scalingFactor);
            baseSize.Width = (int)(1000 * scalingFactor);
            AppWindow.Resize(baseSize);

            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                DisplayArea displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
                if (displayArea != null)
                {
                    // TODO: track last window position and move to there rather than always centering?
                    var CenteredPosition = appWindow.Position;
                    CenteredPosition.X = ((displayArea.WorkArea.Width - appWindow.Size.Width) / 2);
                    CenteredPosition.Y = ((displayArea.WorkArea.Height - appWindow.Size.Height) / 2);
                    appWindow.Move(CenteredPosition);

                    _maxWindowWidth = Math.Max(_maxWindowWidth, displayArea.WorkArea.Width);
                    _maxWindowHeight = Math.Max(_maxWindowHeight, displayArea.WorkArea.Height);
                }
                appWindow.SetIcon(@"Images/JAFDTC_Icon.ico");
            }

            // sets up min/max window sizes using the right magic. code pulled from stackoverflow:
            //
            // https://stackoverflow.com/questions/72825683/wm-getminmaxinfo-in-winui-3-with-c
            //
            _newWndProc = new WinProc(WndProc);
            _oldWndProc = SetWindowLongPtr(hWnd, WindowLongIndexFlags.GWL_WNDPROC, _newWndProc);

            OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter;
            presenter.IsAlwaysOnTop = Settings.IsAlwaysOnTop;
            // presenter.IsResizable = false;
            // presenter.IsMaximizable = false;

            if ((Application.Current as JAFDTC.App).IsAppStartupGood)
            {
                uiAppContentFrame.Navigate(typeof(ConfigurationListPage), null);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // window sizing support
        //
        // ------------------------------------------------------------------------------------------------------------

        // sets up min/max window sizes using the right magic, see
        //
        // https://stackoverflow.com/questions/72825683/wm-getminmaxinfo-in-winui-3-with-c

        private static IntPtr GetWindowHandleForCurrentWindow(object target) =>
            WinRT.Interop.WindowNative.GetWindowHandle(target);

        private static IntPtr WndProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case WindowMessage.WM_GETMINMAXINFO:
                    var dpi = GetDpiForWindow(hWnd);
                    var scalingFactor = (float)dpi / 96;

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.x = (int)(_minWindowWidth * scalingFactor);
                    minMaxInfo.ptMaxTrackSize.x = (int)(_maxWindowWidth * scalingFactor);
                    minMaxInfo.ptMinTrackSize.y = (int)(_minWindowHeight * scalingFactor);
                    minMaxInfo.ptMaxTrackSize.y = (int)(_maxWindowHeight * scalingFactor);

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(_oldWndProc, hWnd, Msg, wParam, lParam);
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, newProc));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // title bar
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public void SetConfigFilterBoxVisibility(Visibility visibility)
        {
            uiAppConfigFilterBox.Visibility = visibility;
            if (ExtendsContentIntoTitleBar == true)
            {
                SetRegionsForCustomTitleBar(visibility != Visibility.Visible);
            }
        }

        // sets up and updates iteractive regions of the title bar, see
        //
        // https://learn.microsoft.com/en-us/windows/apps/develop/title-bar

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                SetRegionsForCustomTitleBar();
            }
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                SetRegionsForCustomTitleBar(uiAppConfigFilterBox.Visibility != Visibility.Visible);
            }
        }

        private void SetRegionsForCustomTitleBar(bool forceFull = false)
        {
            // Specify the interactive regions of the title bar.

            double scaleAdjustment = uiAppTitleBar.XamlRoot.RasterizationScale;

            uiAppTbarRightPadCol.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
            uiAppTbarLeftPadCol.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

            GeneralTransform transform = uiAppConfigFilterBox.TransformToVisual(null);
            Rect bounds = transform.TransformBounds(new Rect(0, 0,
                                                             uiAppConfigFilterBox.ActualWidth,
                                                             uiAppConfigFilterBox.ActualHeight));
            RectInt32 SearchBoxRect = GetRect(bounds, scaleAdjustment);

            var rectArray = (forceFull) ? Array.Empty<RectInt32>() : new RectInt32[] { SearchBoxRect };

            InputNonClientPointerSource nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
            nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
        }

        private static RectInt32 GetRect(Rect bounds, double scale)
        {
            return new RectInt32(
                _X: (int)Math.Round(bounds.X * scale),
                _Y: (int)Math.Round(bounds.Y * scale),
                _Width: (int)Math.Round(bounds.Width * scale),
                _Height: (int)Math.Round(bounds.Height * scale)
            );
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // startup support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// content frame loaded: check for and handle the dcs lua install along with splash now that we have a
        /// xamlroot at which we can target dialogs. hand off the config filter control to teh config list page.
        /// </summary>
        private async void AppContentFrame_Loaded(object sender, RoutedEventArgs args)
        {
            if ((Application.Current as JAFDTC.App).IsAppStartupGood)
            {
                Sploosh(DCSLuaManager.LuaCheck());
                ConfigListPage.ConfigFilterBox = uiAppConfigFilterBox;
            }
            else
            {
                uiAppConfigFilterBox.IsEnabled = false;
                await Utilities.Message1BDialog(Content.XamlRoot, "Bad Joss Captain...",
                                                "Unable to launch JAFDTC due to a fatal error");
            }
        }

        /// <summary>
        /// internal splash sequencing that installs or updates lua and displays the splash screen on new versions.
        /// before calling, use DCSLuaManager.LuaCheck() to determine the current situation with lua.
        /// </summary>
        public async void Sploosh(DCSLuaManagerCheckResult result)
        {
            if (result.ErrorTitle != null)
            {
                await Utilities.Message1BDialog(Content.XamlRoot, result.ErrorTitle, result.ErrorMessage);
                Settings.IsSkipDCSLuaInstall = true;
            }
            else if (!Settings.IsSkipDCSLuaInstall)
            {
                string title = "Install JAFDTC DCS Lua Support?";
                bool didUpdate;
                string successes = "";
                string sucPlural = "";
                string errors = "";
                string errPlural ="";
                foreach (string path in result.InstallPaths)
                {
                    string msg = $"Would you like to add the JAFDTC Lua support to the DCS installation at:\n\n" +
                                 $"      {path}\n\n" +
                                 $"This support is necessary to allow JAFDTC to upload configurations to your jet. " +
                                 $"Please make sure DCS is not running before proceeding. " +
                                 $"You can install Lua support later through the JAFDTC Settings page.";
                    ContentDialogResult choice = await Utilities.Message2BDialog(Content.XamlRoot, title, msg, "Install");
                    if ((choice == ContentDialogResult.Primary) && DCSLuaManager.InstallOrUpdateLua(path, out didUpdate))
                    {
                        sucPlural = (successes.Length > 0) ? "s" : "";
                        successes += $"      {path} (Installed)\n";
                    }
                    else if (choice == ContentDialogResult.Primary)
                    {
                        errPlural = (successes.Length > 0) ? "s" : "";
                        errors += $"      {path}\n";
                    }
                    else
                    {
                        Settings.IsSkipDCSLuaInstall = true;
                    }
                }

                foreach (string path in result.UpdatePaths)
                {
                    bool isSuccess = DCSLuaManager.InstallOrUpdateLua(path, out didUpdate);
                    if (isSuccess && didUpdate)
                    {
                        sucPlural = (successes.Length > 0) ? "s" : "";
                        successes += $"      {path} was Updated\n";
                    }
                    else if (!isSuccess)
                    {
                        errPlural = (successes.Length > 0) ? "s" : "";
                        errors += $"      {path}\n";
                    }
                }

                if (errors.Length > 0)
                {
                    string msg = $"Unable to add or update JAFDTC Lua support in the DCS installation{errPlural} at:\n\n" +
                                 $"{errors}\n" +
                                 $"You can install Lua support later through the JAFDTC Settings page.";
                    await Utilities.Message1BDialog(Content.XamlRoot, "Installation or Update Failed", msg);
                    Settings.IsSkipDCSLuaInstall = false;
                }
                else if (successes.Length > 0)
                {
                    string msg = $"JAFDTC Lua support successfully added to or updated in the DCS installation{sucPlural} at:\n\n" +
                                 $"{successes}\n" +
                                 $"You can uninstall Lua support later through the JAFDTC Settings page.";
                    await Utilities.Message1BDialog(Content.XamlRoot, "Qapla'!", msg);
                }
            }

            if (Settings.IsVersionUpdated)
            {
                await Utilities.Message1BDialog(Content.XamlRoot, "Welcome to JAFDTC!", $"Version v{Settings.VersionJAFDTC}");
            }

            ConfigListPage.RebuildInterfaceState();
        }
    }
}
