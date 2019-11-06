using Splat;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace RVisUI.Controls
{
  public class ActiveViewTemplateSelector : DataTemplateSelector
  {
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (_isInDesignMode)
      {
        return GetTemplateForViewModel("SelectSimulation");
        //return GetTemplateForViewModel("SimulationHome");
      }

      if (null == item) return null;
      var typeName = item.GetType().Name;
      var activeViewName = typeName.Replace("ViewModel", string.Empty);

      return GetTemplateForViewModel(activeViewName);
    }

    private DataTemplate GetTemplateForViewModel(string workflowViewName)
    {
      var ns = $"{nameof(RVisUI)}.{nameof(Controls)}.{nameof(Views)}";
      var viewTypeName = $"{ns}.{workflowViewName}View";
      if (!_templates.ContainsKey(viewTypeName))
      {
        var workflowViewType = Assembly.GetExecutingAssembly().GetType(viewTypeName);
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(workflowViewType);
        template.VisualTree = factory;
        _templates.Add(viewTypeName, template);
      }
      return _templates[viewTypeName];
    }

    private static readonly bool _isInDesignMode = ModeDetector.InDesignMode();
    private IDictionary<string, DataTemplate> _templates = new SortedDictionary<string, DataTemplate>();
  }
}
