using LanguageExt;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sampling
{
  internal interface IDesignActivityViewModel
  {
    string NoActivityMessage { get; }
  }

  internal interface INoDesignActivityViewModel : IDesignActivityViewModel
  {
  }

  internal interface ISamplesDesignActivityViewModel : IDesignActivityViewModel
  {
    DataView Samples { get; set; }
  }

  internal interface IOutputsDesignActivityViewModel : IDesignActivityViewModel
  {
    Arr<string> ElementNames { get; set; }
    string SelectedElementName { get; set; }
    PlotModel Outputs { get; set; }
  }
}
