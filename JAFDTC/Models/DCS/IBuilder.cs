using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// interface for a command builder object that generates a stream of clickable-cockpit commands to change
    /// avionics state in dcs for a system.
    /// </summary>
    public interface IBuilder
    {
        /// <summary>
        /// build the command stream appropriate for the object and add it to internal object state.
        /// </summary>
        public void Build();
    }
}
