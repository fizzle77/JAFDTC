// ********************************************************************************************************************
//
// IMapControlHelper.cs : interfaces for a map window control helpers
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

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// defines the interface for an object that can provide details on markers.
    /// </summary>
    public interface IMapControlMarkerExplainer
    {
        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayName(MapMarkerInfo info);

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayElevation(MapMarkerInfo info, string units = "");
    }

    /// <summary>
    /// defines the interface for a mirror object that is responsible for distributing calls to map "verbs" to
    /// various interested objects. this allows for coordination on edits.
    /// </summary>
    public interface IMapControlVerbMirror
    {
        /// <summary>
        /// registers a map control verb observer with the mirror.
        /// </summary>
        public void RegisterMapControlVerbObserver(IMapControlVerbHandler observer);

        /// <summary>
        /// mirror marker selected across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info);

        /// <summary>
        /// mirror marker opened across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info);

        /// <summary>
        /// mirror marker moved across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info);

        /// <summary>
        /// mirror marker added across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info);

        /// <summary>
        /// mirror marker deleted across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info);
    }

    /// <summary>
    /// defines the interface for a the verbs a mirror object can perform. when making changes, one object
    /// (the sender) initiates a change and makes appropriate state adustments. the sender then invokes the
    /// appropriate verb method (Marker[verb]) on the IMapControlVerbMirror object. this then invokes
    /// ("mirrors") the verb method on each observer registered via RegisterMapControlVerbObserver() that
    /// is not the sender so that those observers can appropriate changes to its internal state to match
    /// the actions of the sender. this ensures internal state is consistent across all objects that can
    /// edit map control state.
    /// </summary>
    public interface IMapControlVerbHandler
    {
        /// <summary>
        /// provides the mirror object (implements IMapControlVerMirror) to use for mirroring.
        /// </summary>
        public IMapControlVerbMirror VerbMirror { get; set; }

        /// <summary>
        /// returns a unique tag for this verb handler.
        /// </summary>
        public string VerbHandlerTag { get; }

        /// <summary>
        /// sender object drives a change to make the indicated marker selected (an UNKNOWN marker type
        /// indicates the selection is being cleared), replicate this change in the handler object.
        /// </summary>
        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info);

        /// <summary>
        /// sender object has opened the indicated marker for detailed editing, replicate this change
        /// in the handler object.
        /// </summary>
        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info);

        /// <summary>
        /// sender object has moved the indicated marker, replicate this change in the handler object.
        /// </summary>
        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info);

        /// <summary>
        /// sender object has added a new marker, replicate this change in the handler object.
        /// </summary>
        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info);

        /// <summary>
        /// sender object has deleted indicated marker, replicate this change in the handler object.
        /// </summary>
        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info);
    }
}
