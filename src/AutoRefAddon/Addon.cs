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
        #region Constants

        public static string COMMENT_FORMAT = "// autoref \"{0}\"\r";

        #endregion

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
            if (e.Key.Equals("Save", StringComparison.OrdinalIgnoreCase) || e.Key.Equals("SaveAs", StringComparison.OrdinalIgnoreCase))
            {
                var assemblies = macroEditorForm.GetReferencedAssemblies().Except(_defaultAssemblies, StringEqualityComparer.OrdinalIgnoreCase).ToList();

                macroEditorForm.AddAutoRefComments(assemblies);
            }
        }

        void OnAfterPerformingCommand(MacroEditorForm macroEditorForm, BeforePerformingCommandEventArgs e)
        {
            switch (e.Key)
            {
                case "Save":
                case "SaveAs":
                    {
                        macroEditorForm.RemoveAutoRefComments();
                    }
                    break;
                case "Open":
                    {
                        var references = macroEditorForm.ParseAutoRefComments();
                        macroEditorForm.AddReferences(references);
                        macroEditorForm.RemoveAutoRefComments();
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
            foreach (var macroEditorForm in args.Forms.OfType<MacroEditorForm>())
            {
                ChangedToolClickHandler(macroEditorForm);

                if (_defaultAssemblies == null) _defaultAssemblies = macroEditorForm.GetReferencedAssemblies();
            }
        }

        #endregion
    }
}