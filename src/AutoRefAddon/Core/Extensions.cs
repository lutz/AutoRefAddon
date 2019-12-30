using SwissAcademic.Citavi.Shell;
using SwissAcademic.Controls;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AutoRef
{
    internal static class Extensions
    {
        #region Fields

        static readonly BindingFlags fieldBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly BindingFlags privateMethodBindingFlags = BindingFlags.InvokeMethod | BindingFlags.NonPublic;
        static readonly BindingFlags staticEventBindingFlags = fieldBindingFlags | BindingFlags.Static;
        static readonly BindingFlags staticFieldBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        static readonly string regex_pattern = "\\/\\/[ ]*autoref[ ]*\"(.+)\"";

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

        static string GetMacroDirectory(this MacroEditorForm macroEditorForm)
        {
            var fileName = macroEditorForm.GetType().GetField("_fileName", fieldBindingFlags)?.GetValue(macroEditorForm).ToString();
            if (string.IsNullOrEmpty(fileName) || !System.IO.File.Exists(fileName)) return null;
            return System.IO.Path.GetDirectoryName(fileName);
        }

        public static IEnumerable<string> GetReferencedAssemblies(this MacroEditorForm macroEditorForm)
        {
            var macroEditor = macroEditorForm.GetType().GetField("macroEditor", fieldBindingFlags)?.GetValue(macroEditorForm);
            var compilerParameters = macroEditor?.GetType().GetField("_compilerParameters", fieldBindingFlags)?.GetValue(macroEditor) as CompilerParameters;
            return compilerParameters?.ReferencedAssemblies.Cast<string>().ToList();
        }

        public static void AddReferences(this MacroEditorForm macroEditorForm, IEnumerable<string> references)
        {
            if (references.Count() == 0) return;

            var macroEditor = macroEditorForm.GetType().GetField("macroEditor", fieldBindingFlags)?.GetValue(macroEditorForm);
            var compilerParameters = macroEditor?.GetType().GetField("_compilerParameters", fieldBindingFlags)?.GetValue(macroEditor) as CompilerParameters;


            compilerParameters?.ReferencedAssemblies.AddRange(references.ToArray());
            macroEditor?.GetType().GetMethod("ResetDotNetProjectResolver", privateMethodBindingFlags)?.Invoke(macroEditor, new object[] { });
            macroEditor?.GetType().GetMethod("RemoveRestrictedAssembliesFromCompilerParameters", privateMethodBindingFlags)?.Invoke(macroEditor, new object[] { });
        }

        public static void RemoveAutoRefComments(this MacroEditorForm macroEditorForm)
        {
            var codeLines = macroEditorForm.MacroCode.Split(new char[] { '\n' }).ToList();
            codeLines.RemoveAll(line => Regex.IsMatch(line, regex_pattern));
            macroEditorForm.MacroCode = string.Join("\n", codeLines);
        }

        public static IEnumerable<string> ParseAutoRefComments(this MacroEditorForm macroEditorForm)
        {
            return Regex.Matches(macroEditorForm.MacroCode, regex_pattern)
                        .Cast<Match>()
                        .Where(match => match.Groups.Count >= 2)
                        .Select(match => CommentMatchToAssemblyPath(macroEditorForm, match.Groups[1].Value))
                        .Where(path => !string.IsNullOrEmpty(path))
                        .Distinct(StringEqualityComparer.OrdinalIgnoreCase)
                        .ToList();
        }

        static string CommentMatchToAssemblyPath(MacroEditorForm macroEditorForm, string match)
        {
            if (string.IsNullOrEmpty(match)) return null;
            if (System.IO.Path.IsPathRooted(match) && System.IO.File.Exists(match)) return match;
            if (!string.IsNullOrEmpty(macroEditorForm.GetMacroDirectory()) && System.IO.Path.Combine(macroEditorForm.GetMacroDirectory(), match) is string macroPath && System.IO.File.Exists(macroPath)) return macroPath;
            if (System.IO.Path.Combine(Application.StartupPath, match) is string applicationPath && System.IO.File.Exists(applicationPath)) return applicationPath;
            return null;
        }

        public static string AssemblyPathToCommentPart(this string path, MacroEditorForm macroEditorForm)
        {
            if (!System.IO.Path.IsPathRooted(path) || !System.IO.File.Exists(path)) return path;
            if (path.StartsWith(macroEditorForm.GetMacroDirectory(), StringComparison.OrdinalIgnoreCase)) return System.IO.Path.GetFileName(path);
            if (path.StartsWith(Application.StartupPath, StringComparison.OrdinalIgnoreCase)) return System.IO.Path.GetFileName(path);
            return path;
        }

        #endregion
    }
}