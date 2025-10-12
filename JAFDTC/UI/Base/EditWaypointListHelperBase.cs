using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.UI.Base
{
    internal abstract class EditWaypointListHelperBase : IEditNavpointListPageHelper
    {
        public abstract string SystemTag { get; }
        public abstract string NavptListTag { get; }
        public abstract AirframeTypes AirframeType { get; }
        public virtual string NavptName => "Waypoint";
        public virtual Type NavptEditorType => typeof(EditNavpointPage);
        public abstract int NavptMaxCount { get; }

        public abstract int NavptCurrentCount(IConfiguration config);
        public virtual int NavptRemainingCount(IConfiguration config) => NavptMaxCount - NavptCurrentCount(config);
        public abstract void AddNavpoint(IConfiguration config);
        public abstract void AppendFromPOIsToConfig(IEnumerable<PointOfInterest> pois, IConfiguration config);
        public abstract void CaptureNavpoints(IConfiguration config, WyptCaptureDataRx.WyptCaptureData[] wypts, int startIndex);
        public abstract void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit);
        public abstract bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config);
        public abstract string ExportNavpoints(IConfiguration config);
        public abstract object NavptEditorArg(Page parentEditor, IConfiguration config, int indexNavpt);
        public abstract INavpointSystemImport NavptSystem(IConfiguration config);
        public abstract bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false);
        public abstract void ResetSystem(IConfiguration config);
        public virtual void SetupUserInterface(IConfiguration config, ListView listView) { }
    }
}
