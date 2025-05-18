using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheapHelpers.Models
{
    /// <summary>
    /// Move DisplayIndex recalculations te here or atleast use the interface
    /// </summary>
    internal interface IDisplayIndex
    {
        public int DisplayIndex { get; set; }
    }
}
