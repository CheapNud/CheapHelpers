using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheapHelpers.Models
{
    public interface IEntityName : IEntityCode
    {
        public string Name { get; set; }
    }
}
