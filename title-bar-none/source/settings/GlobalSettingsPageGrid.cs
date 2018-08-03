using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

//http://stackoverflow.com/questions/24291249/dialogpage-string-array-not-persisted
//http://www.codeproject.com/Articles/351172/CodeStash-a-journey-into-the-dark-side-of-Visual-S

namespace Atma.TitleBarNone
{
	[ClassInterface(ClassInterfaceType.AutoDual)]
	//[CLSCompliant(false), ComVisible(true)]
	public class GlobalSettingsPageGrid : DialogPage
	{
		[Category("Patterns")]
		[DisplayName("No document or solution open")]
		[Description("Default: [ideName]. See 'Supported Tags' section on the left for more guidance.")]
		[DefaultValue(Globals.DefaultPatternIfNothingOpen)]
		[Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string PatternIfNothingOpen { get; set; } = Globals.DefaultPatternIfNothingOpen;

		[Category("Patterns")]
		[DisplayName("Document (no solution) open")]
		[Description("Default: [documentName] - [ideName]. See 'Supported tags' section on the left for more guidance.")]
		[DefaultValue(Globals.DefaultPatternIfDocumentButNoSolutionOpen)]
		[Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[PreviewRequires(PreviewRequiresAttribute.Requirement.Document)]
		public string PatternIfDocumentButNoSolutionOpen { get; set; } = CustomizeVSWindowTitle.DefaultPatternIfDocumentButNoSolutionOpen;

		[Category("Patterns")]
		[DisplayName("Solution in design mode")]
		[Description("Default: [parentPath]\\[solutionName] - [ideName]. See 'Supported tags' section on the left for more guidance.")]
		[DefaultValue("[parentPath]\\[solutionName] - [ideName]")]
		[Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[PreviewRequires(PreviewRequiresAttribute.Requirement.Solution)]
		public string PatternIfDesignMode { get; set; } = "[parentPath]\\[solutionName] - [ideName]";

		[Category("Patterns")]
		[DisplayName("Solution in break mode")]
		[Description("Default: [parentPath]\\[solutionName] (Debugging) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved. See 'Supported tags' section on the left for more guidance.")]
		[DefaultValue("[parentPath]\\[solutionName] (Debugging) - [ideName]")]
		[Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[PreviewRequires(PreviewRequiresAttribute.Requirement.Solution)]
		public string PatternIfBreakMode { get; set; } = "[parentPath]\\[solutionName] (Debugging) - [ideName]";

		[Category("Patterns")]
		[DisplayName("Solution in running mode")]
		[Description("Default: [parentPath]\\[solutionName] (Running) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved. See 'Supported tags' section on the left for more guidance.")]
		[DefaultValue("[parentPath]\\[solutionName] (Running) - [ideName]")]
		[Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[PreviewRequires(PreviewRequiresAttribute.Requirement.Solution)]
		public string PatternIfRunningMode { get; set; } = "[parentPath]\\[solutionName] (Running) - [ideName]";

		[Category("Patterns")]
		[DisplayName("Appended string")]
		[Description("Default: '*'. String to be added at the end of the title to identify that it has been rewritten. If not default and Always rewrite titles, the detection of concurrent instances with the same title may not work.")]
		[DefaultValue(CustomizeVSWindowTitle.DefaultAppendedString)]
		public string AppendedString { get; set; } = CustomizeVSWindowTitle.DefaultAppendedString;

		[Category("Source control")]
		[DisplayName("Git binaries directory")]
		[Description("Default: Empty. Search windows PATH for git if empty.")]
		[Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[FilePicker(true, GitBranchNameResolver.GitExecFn, "Git executable(git.exe)|git.exe|All files(*.*)|*.*", 1)]
		[DefaultValue("")]
		public string GitDirectory { get; set; } = "";

		[Category("Source control")]
		[DisplayName("Hg binaries directory")]
		[Description("Default: Empty. Search windows PATH for hg if empty.")]
		[Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[FilePicker(true, HgBranchNameResolver.HgExecFn, "Hg executable(hg.exe)|hg.exe|All files(*.*)|*.*", 1)]
		[DefaultValue("")]
		public string HgDirectory { get; set; } = "";

		[Category("Source control")]
		[DisplayName("SVN binaries directory")]
		[Description("Default: Empty. Search windows PATH for svn if empty.")]
		[Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[FilePicker(true, SvnResolver.SvnExecFn, "SVN executable(svn.exe)|svn.exe|All files(*.*)|*.*", 1)]
		[DefaultValue("")]
		public string SvnDirectory { get; set; } = "";

		[Category("Source control")]
		[DisplayName("SVN directory separator")]
		[Description("Default: '/'. Specify the character used to separate the SVN directories.")]
		[DefaultValue("/")]
		public string SvnDirectorySeparator { get; set; } = "/";

		[Category("Source control")]
		[DisplayName("Versionr binaries directory")]
		[Description("Default: Empty. Search windows PATH for svn if empty.")]
		[Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[FilePicker(true, VsrResolver.VsrExecFn, "Versionr executable(vsr.exe)|vsr.exe|All files(*.*)|*.*", 1)]
		[DefaultValue("")]
		public string VsrDirectory { get; set; } = "";

		[Category("Source control")]
		[DisplayName("Versionr directory separator")]
		[Description("Default: '/'. Specify the character used to separate the Versionr directories.")]
		[DefaultValue("/")]
		public string VsrDirectorySeparator { get; set; } = "/";

		[Category("Debug")]
		[DisplayName("Enable debug mode")]
		[Description("Default: false. Set to true to activate debug output to Output window.")]
		[DefaultValue(false)]
		public bool EnableDebugMode { get; set; } = false;

		public event EventHandler SettingsChanged;

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			if (e.ApplyBehavior != ApplyKind.Apply)
				return;
			this.SettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}