using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheapHelpers.Blazor.Helpers
{
    public class AccountTabDefinition
    {
        public string Title { get; set; } = string.Empty;
        public RenderFragment Content { get; set; } = default!;
        public string? Policy { get; set; }
        public int Order { get; set; } = 0;
    }
}
