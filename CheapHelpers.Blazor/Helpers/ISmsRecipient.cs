using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheapHelpers.Blazor.Helpers
{
    // Supporting interfaces and classes
    public interface ISmsRecipient
    {
        string PhoneNumber { get; }
        string DisplayName { get; }
    }
}
