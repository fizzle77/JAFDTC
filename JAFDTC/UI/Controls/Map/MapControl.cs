// ********************************************************************************************************************
//
// WorldMapControl.cs : map control
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

using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using MapControl;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

using static JAFDTC.UI.Controls.Map.MapMarkerControl;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// world map control is a control that displays a map upon which routes and markers can be overlaid and edited.
    /// the map only works with lat/lon, edits for other parameters, such as altitude, are assumed to be handled
    /// elsewhere.
    /// </summary>
    public partial class MapControl : MapBase, IMapControlVerbHandler
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// information on a route rendered by a route path control including the route's navpoint locations. used
        /// internally by the control to associate ui elements with data.
        /// </summary>
        private sealed class Route(ObservableCollection<Location> locations)
        {
            public ObservableCollection<Location> Locations { get; set; } = locations;
        }

        // ================================================================================================================

        /// <summary>
        /// information on a route path including the foreground/background path the control renders. used internally
        /// by the map control to associate ui elements with data.
        /// </summary>
        private sealed class RoutePathControlInfo()
        {
            public MapItemsControl ControlFg { get; set; } = null;
            public MapItemsControl ControlBg { get; set; } = null;
            //
            // extra level of hierarchy here (Paths is a list of one element: a list of Locations) is due to the way
            // xaml-map-control implements MapItemsControl as a ListBox. ListBox expects an ItemSource (Paths in our
            // case) which provides items that the ui elements can pull from via bindings to build screen content.
            //
            // NOTE: both MapItemsControl (background and foreground) use this as their data source.
            //
            // TODO: does this need a MapItemsControl?
            //
            public List<Route> Paths { get; set; } = [ ];
        }

        // ================================================================================================================

        /// <summary>
        /// information on the controls and points along a route. this includes the foreground and background route paths,
        /// the positive and negative edit handles, and the marker points. used internally by the control to associate ui
        /// elements with data.
        /// </summary>
        private sealed class RouteInfo
        {
            public RoutePathControlInfo Path { get; set; } = new();
            public MapMarkerControl EditHandlePos { get; set; } = null;
            public MapMarkerControl EditHandleNeg { get; set; } = null;
            public List<MapMarkerControl> Points { get; set; } = [ ];
        }

        // ================================================================================================================

        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        // number of different route colors, needs to be in sync with resources in .xaml that instantiates control.
        //
        private const int NUM_ROUTE_COLORS = 4;

        // delta (in decimal degrees) tp offset the edit handle for a route endpoint.
        //
        private const double EDIT_HANDLE_DELTA = 2.0 / 60.0;

        // movement threshold (squared) that is considered "moving" when dragging edit handles.
        //
        private const double DIST2_MOVE_THRESHOLD = (7.0 * 7.0);

        // defines state of map or marker drags for managing ui behaviors.
        //
        private enum DragStateEnum
        {
            IDLE = 0,                   // idle
            READY_MARKER = 1,           // pointer has been pressed (but not released) on a marker
            ACTIVE_MARKER = 2,          // actively dragging the previously-selected marker
            READY_MAP = 3,              // pointer has been pressed (but not released) on the map
            ACTIVE_MAP = 4              // actively dragging the map
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- dependency properties

        public static readonly DependencyProperty MouseWheelZoomDeltaProperty =
            DependencyPropertyHelper.Register<MapControl, double>(nameof(MouseWheelZoomDelta), 0.25);

        public static readonly DependencyProperty MouseWheelZoomAnimatedProperty =
            DependencyPropertyHelper.Register<MapControl, bool>(nameof(MouseWheelZoomAnimated), true);

        // ---- public properties

        public MapMarkerInfo.MarkerTypeMask EditMask { get; set; }

        public int MaxRouteLength { get; set; }

        public double MouseWheelZoomDelta
        {
            get => (double)GetValue(MouseWheelZoomDeltaProperty);
            set => SetValue(MouseWheelZoomDeltaProperty, value);
        }

        public bool MouseWheelZoomAnimated
        {
            get => (bool)GetValue(MouseWheelZoomAnimatedProperty);
            set => SetValue(MouseWheelZoomAnimatedProperty, value);
        }

        // ---- computed properties

        public bool CanEditSelectedMarker => CanEditMarker(_selectedMarker);

        public MapMarkerInfo SelectedMarkerInfo => (_selectedMarker != null) ? new(_selectedMarker) : null;

        // ---- private properties

        private MapMarkerControl _selectedMarker;

        private VirtualKeyModifiers _lastPressKeyModifiers;

        private DragStateEnum _dragState = DragStateEnum.IDLE;
        private bool _isNewMarker = false;

        private readonly Dictionary<string, RouteInfo> _routes = [ ];
        private readonly Dictionary<string, MapMarkerControl> _marks = [ ];

        private readonly Dictionary<string, string> _routeBrushMap = [ ];

        private readonly Dictionary<MapMarkerInfo.MarkerType, int> _mapMarkerZ = new()
        {
            [MapMarkerInfo.MarkerType.DCS_CORE] = 10,
            [MapMarkerInfo.MarkerType.BULLSEYE] = 11,
            [MapMarkerInfo.MarkerType.USER] = 12,
            [MapMarkerInfo.MarkerType.CAMPAIGN] = 13,
            [MapMarkerInfo.MarkerType.IMPORT_GEN] = 14,
            [MapMarkerInfo.MarkerType.IMPORT_S2A] = 15,
            //
            // z of 20 reserved for route paths.
            //
            [MapMarkerInfo.MarkerType.NAVPT_HANDLE] = 21,
            [MapMarkerInfo.MarkerType.NAVPT] = 22,
        };

#if TODO_IMPLEMENT
        private readonly Dictionary<object, List<MapMarkerInfo>> _imports = [ ];
#endif

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapControl()
        {
            ManipulationMode = ManipulationModes.Scale
                             | ManipulationModes.TranslateX
                             | ManipulationModes.TranslateY
                             | ManipulationModes.TranslateInertia;

            PointerWheelChanged += OnPointerWheelChanged;
#if TODO_IMPLEMENT
            PointerEntered += OnPointerEntered;
            PointerExited += OnPointerExited;
#endif
            PointerMoved += OnPointerMoved;
            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            DoubleTapped += OnDoubleTapped;
            ManipulationDelta += OnManipulationDelta;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // control setup
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the latitude and longitude extents for all known markers.
        /// </summary>

        public void GetMarkerExtents(out double minLat, out double maxLat, out double minLon, out double maxLon, double dP = 0.0)
        {
            minLat = 90.0;
            maxLat = -90.0;
            minLon = 180.0;
            maxLon = -180.0;
            foreach (var child in Children)
            {
                MapMarkerControl marker = child as MapMarkerControl;
                if (marker != null)
                {
                    CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string _, out int _);
                    if (type != MapMarkerInfo.MarkerType.NAVPT_HANDLE)
                    {
                        minLat = Math.Min(minLat, marker.Location.Latitude - dP);
                        maxLat = Math.Max(maxLat, marker.Location.Latitude + dP);
                        minLon = Math.Min(minLon, marker.Location.Longitude - dP);
                        maxLon = Math.Max(maxLon, marker.Location.Longitude + dP);
                    }
                }
            }
        }

        /// <summary>
        /// returns the latitude and longitude extents for all known markers as a BoundingBox.
        /// </summary>
        public BoundingBox GetMarkerBoundingBox(double dP = 0.0)
        {
            GetMarkerExtents(out double minLat, out double maxLat, out double minLon, out double maxLon, dP);
            return new(minLat, minLon, maxLat, maxLon);
        }

        /// <summary>
        /// configure the control from the data source by copying over the marks and routes from the data source to
        /// the control's internal state. this implicitly clears any existing marks or routes.
        /// </summary>
        public void SetupMapContent(Dictionary<string, List<INavpointInfo>> routes,
                                    Dictionary<string, PointOfInterest> marks)
        {
            _marks.Clear();
            foreach (KeyValuePair<string, PointOfInterest> kvp in marks)
                AddMark(kvp.Key, kvp.Value);

            _routes.Clear();
            foreach (KeyValuePair<string, List<INavpointInfo>> kvp in routes)
                AddRoute(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// add a mark to the control by creating a new maker and adding it to the list of known marks.
        /// </summary>
        private void AddMark(string tagStr, PointOfInterest poi)
        {
            Location location = new(double.Parse(poi.Latitude), double.Parse(poi.Longitude));
            _marks[tagStr] = MarkerFactory((MapMarkerInfo.MarkerType)poi.Type, tagStr, -1, location);
        }

        /// <summary>
        /// add a route to the control by creating markers for each navigation point along the route as well as
        /// the lines that connect points on the route.
        /// </summary>
        private void AddRoute(string tag, List<INavpointInfo> npts)
        {
            _routeBrushMap[tag] = $"{_routes.Count % NUM_ROUTE_COLORS}";

            RouteInfo route = new();

            ObservableCollection<Location> locations = [];
            for (int i = 0; i < npts.Count; i++)
                locations.Add(new Location(double.Parse(npts[i].Lat), double.Parse(npts[i].Lon)));

            route.Path.Paths.Add(new(locations));
            route.Path.ControlBg = BuildRoutePathControl(route.Path, tag, false);
            route.Path.ControlFg = BuildRoutePathControl(route.Path, tag, true);
            route.Path.ControlBg.Visibility = Visibility.Collapsed;

            route.EditHandleNeg = MarkerFactory(MapMarkerInfo.MarkerType.NAVPT_HANDLE, tag, -1);
            route.EditHandlePos = MarkerFactory(MapMarkerInfo.MarkerType.NAVPT_HANDLE, tag, -1);

            for (int i = 0; i < locations.Count; i++)
                route.Points.Add(MarkerFactory(MapMarkerInfo.MarkerType.NAVPT, tag, i + 1, locations[i]));

            _routes[tag] = route;
        }

        /// <summary>
        /// helper function to build out a route control. adds the locations to a new path managed by the controller,
        /// creating/configuring the underlying ui control if necessary.
        /// </summary>
        private MapItemsControl BuildRoutePathControl(RoutePathControlInfo info, string tag, bool isForeground)
        {
            MapItemsControl control = new()
            {
                Tag = tag
            };
            Canvas.SetZIndex(control, _mapMarkerZ[MapMarkerInfo.MarkerType.NAVPT] - 1);
            Children.Add(control);
            //
            // TODO: since the route paths use MapItemsControl (a ListBox), they need a data template. no way to
            // TODO: build those programatically without basically parsing xaml here. this implies that colors
            // TODO: are (for now) setup and specified in .xaml.
            //
            string rsrcName = "RouteLineBgTemplate";
            if (isForeground)
            {
                string rdOnlyStr = (!CanEdit(MapMarkerInfo.MarkerType.NAVPT)) ? "_RO" : "";
                rsrcName = $"RouteLineFg_{_routeBrushMap[tag]}{rdOnlyStr}_Template";
            }
            if (Resources.TryGetValue(rsrcName, out object template))
                control.ItemTemplate = template as DataTemplate;
            control.ItemsSource = info.Paths;
            return control;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // marker utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return true if the data source allows edits on markers of the given type.
        /// </summary>
        private bool CanEdit(MapMarkerInfo.MarkerType type)
            => (type != MapMarkerInfo.MarkerType.UNKNOWN) &&
               EditMask.HasFlag((MapMarkerInfo.MarkerTypeMask)(1 << (int)type));

        private bool CanEditMarker(MapMarkerControl marker)
        {
            CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string _, out int _);
            return CanEdit(type);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private MapMarkerControl MarkerFactory(MapMarkerInfo.MarkerType type, string tagStr, int tagInt,
                                               Location loc = null)
        {
            loc ??= new Location(0.0, 0.0);

            string indexStr = _routeBrushMap.GetValueOrDefault(tagStr, "");
            string rdOnlyStr = (!CanEdit(type)) ? "_RO" : "";
            string typeStr = (type != MapMarkerInfo.MarkerType.NAVPT_HANDLE) ? $"{type}"
                                                                             : $"{MapMarkerInfo.MarkerType.NAVPT}";
            Brush brush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            if (Resources.TryGetValue($"MapMarker_{typeStr}{indexStr}{rdOnlyStr}_Brush", out object value))
                brush = value as Brush;

            MapMarkerControl marker = type switch
            {
                MapMarkerInfo.MarkerType.NAVPT
                    => new MapMarkerDiamondControl(brush, brush, new Size(24.0, 24.0)),
                MapMarkerInfo.MarkerType.NAVPT_HANDLE
                    => new MapMarkerCircleControl(brush, brush, new Size(18.0, 18.0))
                    {
                        Visibility = Visibility.Collapsed
                    },
                MapMarkerInfo.MarkerType.DCS_CORE
                    => new MapMarkerSquareControl(brush, brush, new Size(18.0, 18.0)),
                MapMarkerInfo.MarkerType.USER
                or MapMarkerInfo.MarkerType.CAMPAIGN
                or MapMarkerInfo.MarkerType.IMPORT_GEN
                or MapMarkerInfo.MarkerType.IMPORT_S2A
                or MapMarkerInfo.MarkerType.BULLSEYE
                    => new MapMarkerCircleControl(brush, brush, new Size(20.0, 20.0)),
                _ => new MapMarkerCircleControl(),
            };
            marker.Location = loc;
            marker.Tag = TagForMarkerOfKind(type, tagStr, tagInt);
            Canvas.SetZIndex(marker, _mapMarkerZ[type]);
            Children.Add(marker);
            return marker;
        }

        /// <summary>
        /// break or create a tag object from a MapMarkerControl into it's constituent pieces. marker tags (navpoints
        /// and handles) are a tuple of the form: { [type], [integer], [string] }.
        ///
        /// navpoints        [type]      MapMarkerInfo.MarkerType.NAVPT
        ///                  [string]    route tag for this._routes
        ///                  [integer]   number of the navpoint in the path list (so, 1-based index)
        ///
        /// edit handles     [type]      MapMarkerInfo.MarkerType.NAVPT_HANDLE
        ///                  [string]    route tag for this.Routes
        ///                  [integer]   position in the navpoint list where a new point should be inserted (0 implies
        ///                              before first point)
        ///
        /// all others       [type]      MapMarkerInfo.MarkerType.[others]
        ///                  [string]    marker tag for this._marks
        ///                  [integer]   -1
        ///
        /// when tag is null, returns type of UNKNOWN, tagInt of -1, and tagStr of null.
        /// </summary>
        private static void CrackMarkerTag(MapMarkerControl marker,
                                           out MapMarkerInfo.MarkerType type, out string tagStr, out int tagInt)
        {
            if ((marker == null) || (marker.Tag as Tuple<MapMarkerInfo.MarkerType, string, int> == null))
            {
                type = MapMarkerInfo.MarkerType.UNKNOWN;
                tagStr = null;
                tagInt = -1;
            }
            else
            {
                Tuple<MapMarkerInfo.MarkerType, string, int> tuple = marker.Tag as Tuple<MapMarkerInfo.MarkerType, string, int>;
                type = tuple.Item1;
                tagStr = tuple.Item2;
                tagInt = tuple.Item3;
            }
        }

        /// <summary>
        /// return tag used to identify MapMarkerControl, tags are tuples of the form: { [type], [integer], [string] }.
        /// </summary>
        private static Tuple<MapMarkerInfo.MarkerType, string, int> TagForMarkerOfKind(MapMarkerInfo.MarkerType type,
                                                                                       string tagStr, int tagInt)
            => new(type, tagStr, tagInt);

        /// <summary>
        /// return the first MapMarkerControl that is a parent of the starting source framework element from the view
        /// hierarhcy, null if none is found.
        /// </summary>
        private static MapMarkerControl IsSourceMarker(object source)
        {
            for (object elem = source;
                 (elem != null) && (elem is FrameworkElement) && (elem is not MapControl);
                 elem = (elem as FrameworkElement).Parent)
            {
                if (elem is MapMarkerControl)
                    return elem as MapMarkerControl;
            }
            return null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // route marker management
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// drag an editable map marker to a new location. this updates edit handles for navpoints as well.
        /// </summary>
        private void MoveMapMarker(MapMarkerControl marker, Location newLocation)
        {
            CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string str, out int number);
            if (CanEdit(type) && (type == MapMarkerInfo.MarkerType.NAVPT))
            {
                _isNewMarker = false;

                RouteInfo routeInfo = _routes[str];
                marker.Location = newLocation;

                HideEditHandle(routeInfo.EditHandlePos);
                HideEditHandle(routeInfo.EditHandleNeg);

                routeInfo.Points[number - 1].Location = newLocation;

                Debug.Assert(routeInfo.Path.Paths.Count == 1);

                routeInfo.Path.Paths[0].Locations.RemoveAt(number - 1);
                routeInfo.Path.Paths[0].Locations.Insert(number - 1, newLocation);
            }
            else if (CanEdit(type) && (type == MapMarkerInfo.MarkerType.NAVPT_HANDLE))
            {
                _isNewMarker = true;

                RouteInfo routeInfo = _routes[str];
                HideEditHandle(routeInfo.EditHandlePos);
                HideEditHandle(routeInfo.EditHandleNeg);

                _selectedMarker = AddMarkerToRoute(str, number, newLocation);
            }
            else if (CanEdit(type))
            {
                _marks[str].Location = newLocation;
            }
        }

        /// <summary>
        /// add a new marker to a route at the given index in the route. creates the marker ui object and updates
        /// the route to include the new point. returns the MapMarkerInfo for the new marker.
        /// </summary>
        private MapMarkerControl AddMarkerToRoute(string route, int number, Location newLocation)
        {
            RouteInfo routeInfo = _routes[route];

            MapMarkerControl marker = MarkerFactory(MapMarkerInfo.MarkerType.NAVPT, route, 0, newLocation);
            marker.VisualState = VisualStateMask.SHOW_BG;

            routeInfo.Points.Insert(number - 1, marker);
            for (int i = 0; i < routeInfo.Points.Count; i++)
                routeInfo.Points[i].Tag = TagForMarkerOfKind(MapMarkerInfo.MarkerType.NAVPT, route, i + 1);

            routeInfo.Path.Paths[0].Locations.Insert(number - 1, newLocation);

            return marker;
        }

        /// <summary>
        /// return the location for the route edit handle between points p0 (the selected navpoint on the
        /// route) and an adjacent point p1. there are three cases: (1) within a small distance left/right
        /// of p0 if there is only one point, (2) midway between p0 and p1 if both points are in the point
        /// list, and (3) a small distance beyond p0 along the line from the adjacent point if p0 is an
        /// endpoint of the route.
        /// </summary>
        private static Location LocateHandle(List<MapMarkerControl> points, int indexP0, int indexP1)
        {
            double dLat = 0.0;
            double dLon = (indexP1 > indexP0) ? EDIT_HANDLE_DELTA : -EDIT_HANDLE_DELTA;

            Location p0Loc = points[indexP0].Location;
            if (points.Count > 1)
            {
                if (((indexP0 >= 0) && (indexP0 < points.Count)) &&
                    ((indexP1 >= 0) && (indexP1 < points.Count)))
                {
                    Location p1Loc = points[indexP1].Location;
                    dLat = (p1Loc.Latitude - p0Loc.Latitude) / 2.0;
                    dLon = (p1Loc.Longitude - p0Loc.Longitude) / 2.0;
                }
                else
                {
                    Location p1Loc = (indexP0 == 0) ? points[1].Location : points[^2].Location;
                    double len = Math.Sqrt(Math.Pow(p1Loc.Latitude - p0Loc.Latitude, 2) +
                                           Math.Pow(p1Loc.Longitude - p0Loc.Longitude, 2));
                    dLat = ((p0Loc.Latitude - p1Loc.Latitude) / len) * EDIT_HANDLE_DELTA;
                    dLon = ((p0Loc.Longitude - p1Loc.Longitude) / len) * EDIT_HANDLE_DELTA;
                }
            }
            return new(p0Loc.Latitude + dLat, p0Loc.Longitude + dLon);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void SelectMarker(MapMarkerControl marker)
        {
            CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string tagStr, out int tagInt);
            bool isRoute = (type == MapMarkerInfo.MarkerType.NAVPT);
            bool isEditHandle = (type == MapMarkerInfo.MarkerType.NAVPT_HANDLE);
            double dtHandle = (_dragState == DragStateEnum.IDLE) ? 0.10 : 0.30;
            RouteInfo route = _routes.GetValueOrDefault(tagStr, null);

            if (isRoute || isEditHandle)
            {
                foreach (MapMarkerControl routeMarker in route.Points)
                    routeMarker.VisualState = VisualStateMask.SHOW_BG | VisualStateMask.SHOW_FILL;

                route.Path.ControlBg.Visibility = Visibility.Visible;
                route.Path.ControlFg.Visibility = Visibility.Visible;
            }

            if (isRoute && !isEditHandle)
            {
                marker.VisualState &= ~VisualStateMask.SHOW_FILL;

                if (CanEdit(MapMarkerInfo.MarkerType.NAVPT) &&
                    (route.Points.Count < MaxRouteLength) &&
                    (_dragState != DragStateEnum.ACTIVE_MARKER))
                {
                    Utilities.DispatchAfterDelay(marker.DispatcherQueue, dtHandle, false,
                        (sender, evt) =>
                        {
                            // NOTE: remember, intParam from navpoint and handle control tags is navpoint number, *not*
                            // NOTE: an array index...

                            ShowEditHandleAtLocation(route.EditHandleNeg,
                                                     TagForMarkerOfKind(MapMarkerInfo.MarkerType.NAVPT_HANDLE, tagStr, tagInt),
                                                     LocateHandle(route.Points, tagInt - 1, tagInt - 2));

                            ShowEditHandleAtLocation(route.EditHandlePos,
                                                     TagForMarkerOfKind(MapMarkerInfo.MarkerType.NAVPT_HANDLE, tagStr, tagInt + 1),
                                                     LocateHandle(route.Points, tagInt - 1, tagInt));
                        });
                }
            }
            if (isEditHandle)
                ShowEditHandleAtLocation(marker);
            else if (!isRoute)
                marker.VisualState = VisualStateMask.SHOW_BG;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void UnselectMarker(MapMarkerControl marker)
        {
            CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string tagStr, out int _);
            bool isRoute = (type == MapMarkerInfo.MarkerType.NAVPT);
            bool isEditHandle = (type == MapMarkerInfo.MarkerType.NAVPT_HANDLE);
            RouteInfo route = _routes.GetValueOrDefault(tagStr, null);

            if (isRoute || isEditHandle)
                foreach (MapMarkerControl routeMarker in route.Points)
                    routeMarker.VisualState = VisualStateMask.SHOW_FILL;

            if (isRoute && !isEditHandle)
            {
                route.Path.ControlFg.Visibility = Visibility.Visible;
                route.Path.ControlBg.Visibility = Visibility.Collapsed;
                HideEditHandle(route.EditHandleNeg);
                HideEditHandle(route.EditHandlePos);
            }
            if (!isEditHandle)
                marker.VisualState = VisualStateMask.SHOW_FILL;
        }

        /// <summary>
        /// shows an edit handle via its visibility. optionally update the tag and/or location of the handle prior
        /// to showing.
        /// </summary>
        private static void ShowEditHandleAtLocation(MapMarkerControl marker,
                                                     Tuple<MapMarkerInfo.MarkerType, string, int> tag = null,
                                                     Location location = null)
        {
            marker.Tag = tag ?? marker.Tag;
            marker.Location = location ?? marker.Location;
            marker.VisualState = VisualStateMask.SHOW_FILL | VisualStateMask.SHOW_BG;
            marker.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// hides an edit handle via its visibility.
        /// </summary>
        private static void HideEditHandle(MapMarkerControl marker)
        {
            marker.VisualState = VisualStateMask.SHOW_FILL | VisualStateMask.SHOW_BG;
            marker.Visibility = Visibility.Collapsed;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IWorldMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "MapControl";

        public IMapControlVerbMirror VerbMirror { get; set; }

        /// <summary>
        /// handle a change to the selected marker from another source of map actions.
        /// </summary>
        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"MC:MarkerSelect {info.Type}, {info.TagStr} {info.TagInt}");
            if (_selectedMarker != null)
                UnselectMarker(_selectedMarker);
            _selectedMarker = null;

            if ((info.TagStr != null) && (_routes.TryGetValue(info.TagStr, out RouteInfo routeInfo)))
            {
                _selectedMarker = routeInfo.Points[info.TagInt - 1];
                SelectMarker(_selectedMarker);
            }
            else if ((info.TagStr != null) && (_marks.TryGetValue(info.TagStr, out MapMarkerControl marker)))
            {
                _selectedMarker = marker;
                SelectMarker(_selectedMarker);
            }
        }

        /// <summary>
        /// map does not have anything to do on opens originating from other senders.
        /// </summary>
        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info) { }

        /// <summary>
        /// handle the change of a marker location.
        /// </summary>
        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"MC:VerbMarkerMoved {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            MoveMapMarker(_routes[info.TagStr].Points[info.TagInt - 1], new(double.Parse(info.Lat), double.Parse(info.Lon)));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"MC:VerbMarkerAdded {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            RouteInfo routeInfo = _routes[info.TagStr];
            HideEditHandle(routeInfo.EditHandlePos);
            HideEditHandle(routeInfo.EditHandleNeg);

// TODO: what about other marker types?
            AddMarkerToRoute(info.TagStr, info.TagInt, new(double.Parse(info.Lat), double.Parse(info.Lon)));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"MC:VerbMarkerDeleted {info.Type}, {info.TagStr}, {info.TagInt}");
            if ((info.TagStr != null) && (_routes.TryGetValue(info.TagStr, out RouteInfo routeInfo)))
            {
                CrackMarkerTag(_selectedMarker, out MapMarkerInfo.MarkerType type, out string tagStr, out int _);
                if (tagStr == info.TagStr)
                    _selectedMarker = null;
                MapMarkerControl marker = routeInfo.Points[info.TagInt - 1];
                Children.Remove(marker);

                routeInfo.Points.RemoveAt(info.TagInt - 1);
                for (int i = 0; i < routeInfo.Points.Count; i++)
                    routeInfo.Points[i].Tag = TagForMarkerOfKind(info.Type, info.TagStr, i + 1);
                routeInfo.Path.Paths[0].Locations.RemoveAt(info.TagInt - 1);
            }
            else if ((info.TagStr != null) && (_marks.TryGetValue(info.TagStr, out MapMarkerControl marker)))
            {
                CrackMarkerTag(_selectedMarker, out MapMarkerInfo.MarkerType _, out string tagStr, out int _);
                if (tagStr == info.TagStr)
                    _selectedMarker = null;
                Children.Remove(marker);
                _marks.Remove(info.TagStr);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // event handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on mouse wheel, zoom to integer multiple of MouseWheelZoomDelta when the event was raised by a mouse wheel
        /// or by a large movement on a touch pad or other high resolution device.
        /// </summary>
        private void OnMouseWheel(Windows.Foundation.Point position, double delta)
        {
            var zoomLevel = TargetZoomLevel + MouseWheelZoomDelta * delta;
            var animated = false;

            if ((_dragState != DragStateEnum.READY_MARKER) || (_dragState != DragStateEnum.ACTIVE_MARKER))
            {
                if ((delta <= -1d) || (delta >= 1d))
                {
                    zoomLevel = MouseWheelZoomDelta * Math.Round(zoomLevel / MouseWheelZoomDelta);
                    animated = MouseWheelZoomAnimated;
                }
                ZoomMap(position, zoomLevel, animated);
            }
        }

        /// <summary>
        /// on pointer wheel changes, convert the event to a mouse wheel event and pass off to OnMouseWheel().
        /// </summary>
        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs evt)
        {
            if ((_dragState != DragStateEnum.READY_MARKER) || (_dragState != DragStateEnum.ACTIVE_MARKER))
            {
                Microsoft.UI.Input.PointerPoint point = evt.GetCurrentPoint(this);
                OnMouseWheel(point.Position, point.Properties.MouseWheelDelta / 120d);
            }
        }

        /// <summary>
        /// on pointer pressed, select a marker and get ready to drag it (if press is in a marker) or clear
        /// the selection and get ready to drag the map (if press outside a marker). only left mouse button
        /// is tracked. returns to idle if no match. marks event as handled.
        /// </summary>
        protected void OnPointerPressed(object sender, PointerRoutedEventArgs evt)
        {
            _isNewMarker = false;
            _lastPressKeyModifiers = evt.KeyModifiers;

            MapMarkerControl marker = IsSourceMarker(evt.OriginalSource);
            if (evt.GetCurrentPoint(this).Properties.IsLeftButtonPressed && (marker != null))
            {
                if (_selectedMarker != marker)
                {
                    if (_selectedMarker != null)
                        UnselectMarker(_selectedMarker);
                    SelectMarker(marker);
                    _selectedMarker = marker;
                    VerbMirror?.MirrorVerbMarkerSelected(this, new(_selectedMarker));
                }
                _dragState = DragStateEnum.READY_MARKER;
            }
            else if (evt.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _dragState = DragStateEnum.READY_MAP;
            }
            else
            {
                _dragState = DragStateEnum.IDLE;
            }

            evt.Handled = true;
        }

        /// <summary>
        /// on pointer release, TODO
        /// </summary>
        protected void OnPointerReleased(object sender, PointerRoutedEventArgs evt)
        {
            if (_dragState == DragStateEnum.ACTIVE_MARKER)
            {
                _dragState = DragStateEnum.IDLE;                // force SelectMarker() to restore handles...
                SelectMarker(_selectedMarker);
            }
            else if (_dragState == DragStateEnum.READY_MAP)
            {
                if (_selectedMarker != null)
                    UnselectMarker(_selectedMarker);
                _selectedMarker = null;
                VerbMirror?.MirrorVerbMarkerSelected(this, new());
            }

            _dragState = DragStateEnum.IDLE;
            _isNewMarker = false;

            evt.Handled = true;
        }

        /// <summary>
        /// on double-tap, TODO
        /// </summary>
        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs evt)
        {
            MapMarkerControl marker = IsSourceMarker(evt.OriginalSource);
            if (marker != null)
                VerbMirror?.MirrorVerbMarkerOpened(this, new(marker));
            else if (_lastPressKeyModifiers == VirtualKeyModifiers.None)
                Center = ViewToLocation(evt.GetPosition(this));
            else if (_lastPressKeyModifiers == VirtualKeyModifiers.Shift)
                ZoomToBounds(GetMarkerBoundingBox(2.0));
            _dragState = DragStateEnum.IDLE;
        }

        /// <summary>
        /// on pointer move, enable manipulation of the map if the left button is pressed and there are no modifier
        /// keys pressed.
        /// </summary>
        private void OnPointerMoved(object sender, PointerRoutedEventArgs evt)
        {
            if (evt.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                if (_dragState == DragStateEnum.READY_MAP)
                {
                    _dragState = (evt.KeyModifiers == VirtualKeyModifiers.None) ? DragStateEnum.ACTIVE_MAP
                                                                                : DragStateEnum.IDLE;
                }
                else if (_dragState == DragStateEnum.ACTIVE_MARKER)
                {
                    MoveMapMarker(_selectedMarker, ViewToLocation(evt.GetCurrentPoint(this).Position));
                    if (_isNewMarker)
                        VerbMirror?.MirrorVerbMarkerAdded(this, new(_selectedMarker));
                    else
                        VerbMirror?.MirrorVerbMarkerMoved(this, new(_selectedMarker));
                    _isNewMarker = false;
                }
            }
            else
            {
                _dragState = DragStateEnum.IDLE;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs evt)
        {
            if (_dragState == DragStateEnum.READY_MARKER)
            {
                CrackMarkerTag(_selectedMarker, out MapMarkerInfo.MarkerType type, out string _, out int _);
                double dist2 = Math.Pow(evt.Cumulative.Translation.X, 2.0) + Math.Pow(evt.Cumulative.Translation.Y, 2.0);
                if ((dist2 > DIST2_MOVE_THRESHOLD) && CanEdit(type))
                {
                    _dragState = DragStateEnum.ACTIVE_MARKER;
                    if (type == MapMarkerInfo.MarkerType.NAVPT_HANDLE)
                        _isNewMarker = true;
                }
            }
            else if (_dragState == DragStateEnum.ACTIVE_MAP)
            {
// TODO: tweak translation to keep map on theater?
                TranslateMap(evt.Delta.Translation);
            }
        }

#if TODO_IMPLEMENT
        /// <summary>
        /// TODO: document
        /// </summary>
        private void OnPointerEntered(object sender, PointerRoutedEventArgs evt)
        {
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void OnPointerExited(object sender, PointerRoutedEventArgs evt)
        {
        }
#endif
    }
}
