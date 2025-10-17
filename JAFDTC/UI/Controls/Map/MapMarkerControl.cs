// ********************************************************************************************************************
//
// MapMarkerControl.cs : abstract base class for a map window marker control
//
// Copyright(C) 2025 ilominar/raven
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
//
// adapted from code from https://github.com/ClemensFischer/XAML-Map-Control
//
// MIT License
//
// Copyright(c) 2025 Clemens Fischer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without
// limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
// ********************************************************************************************************************

using MapControl;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public abstract partial class MapMarkerControl : MapContentControl
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        protected const double FG_STROKE_THICKNESS = 4.0;
        protected const double BG_STROKE_THICKNESS = 8.0;

        protected const double PAD_X = BG_STROKE_THICKNESS + 4.0;
        protected const double PAD_Y = BG_STROKE_THICKNESS + 4.0;

        protected readonly Brush _brushBaseBg = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        protected readonly Brush _brushNone = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        // control state definitions
        //
        [Flags]
        public enum VisualStateMask
        {
            SHOW_NONE = 0,
            SHOW_BG = 0x0001,
            SHOW_FILL = 0x002
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        private VisualStateMask _visualState;
        public VisualStateMask VisualState
        {
            get => _visualState;
            set
            {
                if (_visualState != value)
                {
                    _visualState = value;
                    StyleControl(_visualState);
                }
            }
        }

        // ---- protected properties

        protected Size Size { get; private set; }

        protected Brush PathBrush { get; private set; }
        protected Brush FillBrush { get; private set; }

        // ---- read-only properties

        protected readonly Path PathBg;
        protected readonly Path PathFg;

        private readonly Border _border = new();

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapMarkerControl() : this(null, null) { }

        public MapMarkerControl(Brush pathBrush = null, Brush fillBrush = null, Size? size = null)
        {
            Size = size ?? DefaultSize;
            PathBrush = pathBrush ?? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            FillBrush = fillBrush ?? _brushNone;

            PathFg = CreatePath(PathBrush, FillBrush, FG_STROKE_THICKNESS);
            PathBg = CreatePath(_brushNone, _brushNone, BG_STROKE_THICKNESS);

            _border.Margin = new Thickness(0, 0, 0, 0);

            VisualState = VisualStateMask.SHOW_NONE;

            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            DefaultStyleKey = typeof(MapMarkerControl);
            IsHitTestVisible = true;

            Grid grid = new()
            {
                Width = Size.Width,
                Height = Size.Height,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            grid.Children.Add(PathBg);
            grid.Children.Add(PathFg);
            grid.Children.Add(_border);
            Content = grid;

            SizeChanged += OnSizeChanged;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // abstract methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns default size of marker. derived classes must override.
        /// </summary>
        protected abstract Size DefaultSize { get; }

        /// <summary>
        /// size changed event handler for marker. this method should rebuild the geometry in _pathFg and _pathBg for
        /// the control based on the current size. derived classes must override.
        /// </summary>
        protected abstract void OnSizeChanged(object sender, SizeChangedEventArgs evt);

        // ------------------------------------------------------------------------------------------------------------
        //
        // helper functions for derived classes
        //
        // ------------------------------------------------------------------------------------------------------------

        protected static LineSegment LineTo(double x, double y) => new() { Point = new Windows.Foundation.Point(x, y) };

        protected static ArcSegment ArcTo(double x, double y, double r)
            => new()
                {
                    Point = new Windows.Foundation.Point(x, y),
                    Size = new Windows.Foundation.Size(r, r),
                    SweepDirection = SweepDirection.Clockwise
                };

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        private static Path CreatePath(Brush stroke, Brush fill, double thickness)
            => new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Stretch = Stretch.None,
                Stroke = stroke,
                Fill = fill,
                StrokeThickness = thickness
            };

        /// <summary>
        /// TODO: document
        /// </summary>
        public void StyleControl(VisualStateMask state)
        {
            PathBg.Fill = (state.HasFlag(VisualStateMask.SHOW_BG)) ? _brushBaseBg : _brushNone;
            PathBg.Stroke = (state.HasFlag(VisualStateMask.SHOW_BG)) ? _brushBaseBg : _brushNone;

            PathFg.Fill = (state.HasFlag(VisualStateMask.SHOW_FILL)) ? FillBrush : _brushNone;
        }
    }
}
