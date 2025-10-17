// ********************************************************************************************************************
//
// MapMarkerSquareControl.cs : control for a square map window marker
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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// content control class for a square map marker.
    /// </summary>
    public partial class MapMarkerSquareControl(Brush pathBrush = null, Brush fillBrush = null, Size? size = null)
        : MapMarkerControl(pathBrush, fillBrush, size)
    {
        const double MARKER_W = 16.0;
        const double MARKER_H = 16.0;

        private const double BG_STROKE_EPSILON = -2.0;          // fixes windows fuckery, don't ask...

        /// <summary>
        /// returns default size of the marker.
        /// </summary>
        protected override Size DefaultSize => new(MARKER_W + PAD_X, MARKER_H + PAD_Y);

        /// <summary>
        /// on a size changed event, rebuild the geometries underlying the marker and install them in the
        /// foreground and background paths for the marker.
        /// </summary>
        protected override void OnSizeChanged(object sender, SizeChangedEventArgs evt)
        {
            PathBg.Data = BuildGeometry(0.0);
            PathFg.Data = BuildGeometry(BG_STROKE_EPSILON);
        }

        /// <summary>
        /// rebuild geometry for the marker.
        /// </summary>
        private PathGeometry BuildGeometry(double e)
        {
            PathGeometry geometry = new();

            // HACK: e is a fudge factor to align the rendering for the fg/bg paths. looks like windoze
            // HACK: does not center the stroke along the line between end points of the line?

            double dx = (Size.Width - PAD_X) / 2.0;
            double dy = (Size.Height - PAD_Y) / 2.0;
            double ox = (PAD_X / 2.0) + e - 1.0;
            double oy = (PAD_Y / 2.0) + e - 1.0;

            PathFigure figure = new()
            {
                StartPoint = new Windows.Foundation.Point(ox, oy),
                IsClosed = true,
                IsFilled = true
            };
            figure.Segments.Add(LineTo((2 * dx) + ox, oy));
            figure.Segments.Add(LineTo((2 * dx) + ox, (2 * dy) + oy));
            figure.Segments.Add(LineTo(ox, (2 * dy) + oy));

            geometry.Figures.Add(figure);

            return geometry;
        }
    }
}
