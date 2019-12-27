using Infragistics.Win.UltraWinToolbars;
using SwissAcademic.Citavi.Shell;
using SwissAcademic.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AutoRef
{
    public class Addon : CitaviAddOn
    {
        #region Fields

        IEnumerable<string> _defaultAssemblies;

        #endregion

        #region Constructors

        public Addon() => Application.OpenForms.AddListChangedEventHandler(Forms_Added, ListChangedType.Added);

        #endregion

        #region Properties
        public override AddOnHostingForm HostingForm => AddOnHostingForm.None;

        #endregion

        #region Methods

        void ChangedToolClickHandler(MacroEditorForm form)
        {
            var toolbarsManager = form.GetToolbarsManager();
            var registredDelegates = toolbarsManager?.RemoveEventHandlersFromEvent("ToolClick");
            ToolClickEventHandler clickEventHandler = (sender, args) =>
            {
                var e = new BeforePerformingCommandEventArgs(form, args.Tool.Key, args.ListToolItem?.Key, null);
                OnBeforePerformingCommand(form, e);
                if (!e.Handled)
                {
                    foreach (var del in registredDelegates)
                    {
                        del.DynamicInvoke(sender, args);
                    }

                    OnAfterPerformingCommand(form, e);
                }
            };

            toolbarsManager?.AddEventHandlerForEvent("ToolClick", clickEventHandler);
        }

        void OnBeforePerformingCommand(MacroEditorForm macroEditorForm, BeforePerformingCommandEventArgs e)
        {
            if (e.Key.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                var codeLines = macroEditorForm.GetReferencedAssemblies()
                                                       .Where(assembly => _defaultAssemblies.FirstOrDefault(a => a.Equals(assembly, StringComparison.OrdinalIgnoreCase)) == null)
                                                       .Select(ase => $"// #include \"{ase}\"\r")
                                                       .ToList();

                codeLines.AddRange(macroEditorForm.MacroCode.Split(new char[] { '\n' }).ToList());
                macroEditorForm.MacroCode = string.Join("\n", codeLines);
            }
        }
        
        void OnAfterPerformingCommand(MacroEditorForm macroEditorForm, BeforePerformingCommandEventArgs e)
        {
            switch (e.Key)
            {
                case "Save":
                    {
                        macroEditorForm.RemoveIncludes();
                    }
                    break;
                case "Open":
                    {
                        var includes = macroEditorForm.ParseIncludes();
                        macroEditorForm.ReferenceIncludes(includes);
                        macroEditorForm.RemoveIncludes();
                    }
                    break;
                default:
                    break;
            }

        }


        #endregion

        #region EventHandler

        void Forms_Added(object sender, ListChangedEventArgs args)
        {
            foreach (var form in args.Forms)
            {
                if (form is MacroEditorForm macroEditorForm)
                {
                    ChangedToolClickHandler(macroEditorForm);

                    if (_defaultAssemblies == null) _defaultAssemblies = macroEditorForm.GetReferencedAssemblies();
                }
            }
        }

        #endregion
    }
}