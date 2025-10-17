
// ********************************************************************************************************************
//
// MapRouteControl.cs : path geometry for a route in map window
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
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public partial class MapRoutePath : MapPath
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        // don't EVEN think about asking, dude...
        //
        private const double CLIP_HACK_BBMAG = 8192.0;

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty LocationsProperty =
            DependencyPropertyHelper.Register<MapRoutePath, IEnumerable<Location>>(nameof(Locations), null,
                (routePath, oldValue, newValue) => routePath.DataCollectionPropertyChanged(oldValue, newValue)
            );

        public IEnumerable<Location> Locations
        {
            get => (IEnumerable<Location>)GetValue(LocationsProperty);
            set => SetValue(LocationsProperty, value);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapRoutePath()
        {
            Data = new PathGeometry();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // object change notifications
        //
        // ------------------------------------------------------------------------------------------------------------

        private void DataCollectionPropertyChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            if (oldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= DataCollectionChanged;
            if (newValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += DataCollectionChanged;

            UpdateData();
        }

        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateData();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // geometry updates
        //
        // ------------------------------------------------------------------------------------------------------------

        protected override void UpdateData()
        {
            UpdateData(Locations);
        }

        protected void UpdateData(IEnumerable<Location> locations)
        {
            PathFigureCollection figures = ((PathGeometry)Data).Figures;
            figures.Clear();

            if ((ParentMap != null) && (locations != null))
                AddRoutePoints(figures, locations, GetLongitudeOffset(Location ?? locations.FirstOrDefault()));
        }

        /// <summary>
        /// add points corresponding to the locations to the geometry for the route.
        /// </summary>
        private void AddRoutePoints(PathFigureCollection figures, IEnumerable<Location> locations, double lonOffset)
        {
            IEnumerable<Point> points = locations.Select(location => LocationToView(location, lonOffset))
                                                 .Where(point => point.HasValue)
                                                 .Select(point => point.Value);
            if (points.Any())
            {
                Point start = points.First();
                double minX = start.X;
                double maxX = start.X;
                double minY = start.Y;
                double maxY = start.Y;
                foreach (Point point in points.Skip(1))
                {
                    minX = Math.Min(minX, point.X);
                    maxX = Math.Max(maxX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxY = Math.Max(maxY, point.Y);
                }

                if ((maxX >= 0) && (minX <= ParentMap.ActualWidth) && (maxY >= 0) && (minY <= ParentMap.ActualHeight))
                {
                    // HACK WARNING: what in the ever-loving-fuck, batman? so, for reasons uknown to mere mortals,
                    // HACK WARNING: the line just disappears once the magnitude of the min/max bounds gets around
                    // HACK WARNING: 16k. not sure if this is from MapControl or if this is some dusty hold-over
                    // HACK WARNING: from a coordinate system assumption that dates back to windows 1.0?
                    //
                    // HACK WARNING: because i am stupid and have no interest in spending time trying to figure out
                    // HACK WARNING: why what's happening is happening, i'm just going to clip the set of lines
                    // HACK WARNING: so the min/max magnitude drops below the 16k danger zone. ours is not to
                    // HACK WARNING: wonder why, ours is but to do and die...
                    //
                    // check if we have EXTREME(tm) min/max points: if so, rebuild the point list to clip all the
                    // segments of the path to fit within an 8k x 8k canvas. thank dog for cohen-sutherland...
                    //
                    if ((Math.Min(minX, minY) < -CLIP_HACK_BBMAG) || (Math.Max(maxX, maxY) > CLIP_HACK_BBMAG))
                        points = ClipRouteToBox([.. points ], CLIP_HACK_BBMAG);

                    if (points.Any())
                    {
                        PolyLineSegment polyline = new();
                        foreach (Point point in points.Skip(1))
                            polyline.Points.Add(point);

                        PathFigure figure = new() { StartPoint = points.First() };
                        figure.Segments.Add(polyline);

                        figures.Add(figure);
                    }
                }
            }
        }

        // hat tip to https://sighack.com/post/cohen-sutherland-line-clipping-algorithm and my old 3d
        // computer graphics books.

        /// <summary>
        /// return cohen-sutherland encoding for an (x, y) point and given bounding box. bounding box is
        /// assumed to span (-bbMag, -bbMag) to (bbMag, bbMag)
        /// </summary>
        private static int EncodePointRegion(Point p, double bbMag)
        {
            int code = 0;

            if (p.X < -bbMag)                // bit[0] => point left of clip bounding box
                code |= (1 << 0);
            else if (p.X > bbMag)            // bit[1] => point right of clip bounding box
                code |= (1 << 1);

            if (p.Y < -bbMag)                // bit[2] => point below clip bounding box
                code |= (1 << 2);
            else if (p.Y > bbMag)            // bit[3] => point above clip bounding box
                code |= (1 << 3);

            return code;
        }

        /// <summary>
        /// clip the line between p0 and p1 to a bounding box spanning (-bbMag, -bbMag) to (bbMag, bbMag)
        /// using choen-sutherland. returns a tuple with the clipped line, null if the line lies outside
        /// the bounding box.
        /// </summary>
        private static Tuple<Point, Point> ClipLine(Point p0, Point p1, double bbMag)
        {
            bool isAccept = false;
            while (!isAccept)
            {
                int p0Code = EncodePointRegion(p0, bbMag);
                int p1Code = EncodePointRegion(p1, bbMag);

                if ((p0Code == 0x00) && (p1Code == 0x00))
                {
                    isAccept = true;                            // line inside bounding box, accept
                }
                else if ((p0Code & p1Code) != 0x00)
                {
                    break;                                      // line outside bounding box, reject
                }
                else
                {
                    Point pNew = new(0.0, 0.0);

                    int code = (p0Code != 0x00) ? p0Code : p1Code;
                    if ((code & (1 << 0)) != 0x00)                     // selected endpoint is left of bounding box
                        pNew = new Point(-bbMag, ((p1.Y - p0.Y) / (p1.X - p0.X)) * (-bbMag - p0.X) + p0.Y);
                    else if ((code & (1 << 1)) != 0x00)                 // selected endpoint is right of bounding box
                        pNew = new Point(bbMag, ((p1.Y - p0.Y) / (p1.X - p0.X)) * (bbMag - p0.X) + p0.Y);
                    else if ((code & (1 << 3)) != 0)                    // selected endpoint above bounding box
                        pNew = new Point(((p1.X - p0.X) / (p1.Y - p0.Y)) * (bbMag - p0.Y) + p0.X, bbMag);
                    else if ((code & (1 << 2)) != 0)                    // selected endpoint is below bounding box
                        pNew = new Point(((p1.X - p0.X) / (p1.Y - p0.Y)) * (-bbMag - p0.Y) + p0.X, -bbMag);

                    if (code == p0Code)
                        p0 = pNew;
                    else
                        p1 = pNew;
                }
            }

            return (isAccept) ? new Tuple<Point, Point>(p0, p1) : null;
        }

        /// <summary>
        /// use cohen-sutherland to build a list of points for the input route that are totally within the
        /// bounding box defined by (-bbMag, -bbMag) to (bbMag, bbMag). note that this can grow the point
        /// list if verticies have to be "pushed" toward the bounding box.
        /// </summary>
        private static List<Point> ClipRouteToBox(List<Point> points, double bbMag)
        {
            // NOTE: the way we build the point list can lead to the path "cutting the corner" of the bounding
            // NOTE: box. this is not a big deal as such shenanigans should be well off screen for any
            // NOTE: reasonably sized bounding box.

            List<Point> pointsClip = [ ];
            for (int i = 1; i < points.Count; i++)
            {
                Tuple<Point, Point> newPoints = ClipLine(points[i - 1], points[i], bbMag);
                if (newPoints != null)
                {
                    pointsClip.Add(newPoints.Item1);
                    pointsClip.Add(newPoints.Item2);
                }
            }
            return pointsClip;
        }
    }
}
