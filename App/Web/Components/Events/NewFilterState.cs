using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Web.Components.Events
{
    public record NewFilterState
{
    public string Search { get; init; } = "";
    public List<string> Categories { get; set; } = [];
    public List<string> Places { get; init; } = [];
    public string Date { get; init; } = "";
}

}