using SwissAcademic.Citavi.Shell;
using SwissAcademic.Controls;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoRef
{
    internal static class Extensions
    {
        #region Fields

        static readonly BindingFlags fieldBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly BindingFlags staticEventBindingFlags = fieldBindingFlags | BindingFlags.Static;
        static readonly BindingFlags staticFieldBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        #endregion

        #region Methods

        public static ToolbarsManager GetToolbarsManager<T>(this T form) where T : FormBase => form.GetType().GetField("toolbarsManager", fieldBindingFlags)?.GetValue(form) as ToolbarsManager;

        public static IReadOnlyList<Delegate> RemoveEventHandlersFromEvent(this ToolbarsManager toolbarsManager, string eventName)
        {

            var eventsPropertyInfo = toolbarsManager
                                    .GetType()
                                    .GetProperties(staticEventBindingFlags)
                                    .Where(p => p.Name.Equals("Events", StringComparison.OrdinalIgnoreCase) && p.PropertyType.Equals(typeof(Infragistics.Shared.EventHandlerDictionary)))
                                    .FirstOrDefault();

            var eventHandlerList = eventsPropertyInfo?
                                   .GetValue(toolbarsManager, new object[] { }) as Infragistics.Shared.EventHandlerDictionary;

            var eventFieldInfo = typeof(ToolbarsManager)
                                  .BaseType
                                  .GetFields(staticFieldBindingFlags)
                                  .FirstOrDefault(fi => fi.Name.Equals("Event_" + eventName, StringComparison.OrdinalIgnoreCase));

            var eventKey = eventFieldInfo.GetValue(toolbarsManager);

            var currentEventHandler = eventHandlerList[eventKey] as Delegate;
            Delegate[] currentRegistredHandlers = currentEventHandler.GetInvocationList();
            foreach (var item in currentRegistredHandlers)
            {
                toolbarsManager.GetType().GetEvent(eventName).RemoveEventHandler(toolbarsManager, item);
            }

            return currentRegistredHandlers.ToList().AsReadOnly();
        }

        public static void AddEventHandlerForEvent(this ToolbarsManager toolbarsManager, string eventName, Delegate @delegate) => toolbarsManager.GetType().GetEvent(eventName).AddEventHandler(toolbarsManager, @delegate);


        public static IEnumerable<string> GetReferencedAssemblies(this MacroEditorForm macroEditorForm)
        {
            var macroEditor = macroEditorForm.GetType().GetField("macroEditor", fieldBindingFlags)?.GetValue(macroEditorForm);
            var compilerParameters = macroEditor?.GetType().GetField("_compilerParameters", fieldBindingFlags)?.GetValue(macroEditor) as CompilerParameters;
            return compilerParameters?.ReferencedAssemblies.Cast<string>().ToList();
        }

        public static void ReferenceIncludes(this MacroEditorForm macroEditorForm, IEnumerable<string> includes)
        {
            if (includes.Count() == 0) return;

            var macroEditor = macroEditorForm.GetType().GetField("macroEditor", fieldBindingFlags)?.GetValue(macroEditorForm);
            var compilerParameters = macroEditor?.GetType().GetField("_compilerParameters", fieldBindingFlags)?.GetValue(macroEditor) as CompilerParameters;


            compilerParameters?.ReferencedAssemblies.AddRange(includes.ToArray());
            macroEditor?.GetType().GetMethod("ResetDotNetProjectResolver", BindingFlags.NonPublic | BindingFlags.InvokeMethod)?.Invoke(macroEditor, new object[] { });
            macroEditor?.GetType().GetMethod("RemoveRestrictedAssembliesFromCompilerParameters", BindingFlags.NonPublic | BindingFlags.InvokeMethod)?.Invoke(macroEditor, new object[] { });
        }

        public static void RemoveIncludes(this MacroEditorForm macroEditorForm)
        {
            var codeLines = macroEditorForm.MacroCode.Split(new char[] { '\n' }).ToList();
            codeLines.RemoveAll(line => line.StartsWith("// #include"));
            macroEditorForm.MacroCode = string.Join("\n", codeLines);
        }

        public static IEnumerable<string> ParseIncludes(this MacroEditorForm macroEditorForm) => macroEditorForm.MacroCode.Split(new char[] { '\n' }).Where(line => line.StartsWith("// #include")).Select(incl => incl.Substring(incl.IndexOf("\"")).Trim().Trim('\"')).ToList();

        #endregion
    }
}