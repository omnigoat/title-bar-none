using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;

// The PackageRegistration attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class
// is a package.
//
// The InstalledProductRegistration attribute is used to register the information needed to show this package
// in the Help/About dialog of Visual Studio.

namespace Atma.TitleBarNone
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	//[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string), ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(Guids.PackageGuid)]
	//[ProvideOptionPage(typeof(GlobalSettingsPageGrid), "Title Bar None", "Settings", 101, 1000, true)]
	//[ProvideOptionPage(typeof(SettingsOverridesPageGrid), "Customize VS Window Title", "Solution-specific overrides", 51, 500, true)]
	//[ProvideOptionPage(typeof(SupportedTagsGrid), "Customize VS Window Title", "Supported tags", 101, 1000, true)]
	[ComVisible(true)]
	public sealed class TitleBarNonePackage : Package
	{
		public string IDEName { get; private set; }
		public string ElevationSuffix { get; private set; }

		public static TitleBarNonePackage CurrentPackage;

		private System.Windows.Forms.Timer ResetTitleTimer;

		/// <summary>
		/// Default constructor of the package.
		/// Inside this method you can place any initialization code that does not require
		/// any Visual Studio service because at this point the package object is created but
		/// not sited yet inside Visual Studio environment. The place to do all the other
		/// initialization is the Initialize method.
		/// </summary>
		public TitleBarNonePackage()
		{
			CurrentPackage = this;

			Globals.DTE = (DTE2)GetGlobalService(typeof(DTE));
			Globals.DTE.Events.DebuggerEvents.OnEnterBreakMode += OnIdeEvent;
			Globals.DTE.Events.DebuggerEvents.OnEnterRunMode += this.OnIdeEvent;
			Globals.DTE.Events.DebuggerEvents.OnEnterDesignMode += this.OnIdeEvent;
			Globals.DTE.Events.DebuggerEvents.OnContextChanged += this.OnIdeEvent;
			Globals.DTE.Events.SolutionEvents.AfterClosing += this.OnIdeSolutionEvent;
			Globals.DTE.Events.SolutionEvents.Opened += this.OnIdeSolutionEvent;
			Globals.DTE.Events.SolutionEvents.Renamed += this.OnIdeSolutionEvent;
			Globals.DTE.Events.WindowEvents.WindowCreated += this.OnIdeEvent;
			Globals.DTE.Events.WindowEvents.WindowClosing += this.OnIdeEvent;
			Globals.DTE.Events.WindowEvents.WindowActivated += this.OnIdeEvent;
			Globals.DTE.Events.DocumentEvents.DocumentOpened += this.OnIdeEvent;
			Globals.DTE.Events.DocumentEvents.DocumentClosing += this.OnIdeEvent;

			// use reflection to popuplate all the informers we have
			SupportedTags = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
				.Where(type => type.IsClass && type.Namespace == "Atma.TitleBarNone.Resolvers"
					&& !type.IsAbstract
					&& type.GetMember("TagNames").Count() != 0)
				.Cast<Informers.Informer>()
				.SelectMany(x => x.TagNames)
				.ToList();
		}

		public readonly List<string> SupportedTags;

		private void OnIdeEvent(Window gotfocus, Window lostfocus)
		{
			OnIdeEvent();
		}

		private void OnIdeEvent(Document document)
		{
			OnIdeEvent();
		}

		private void OnIdeEvent(Window window)
		{
			OnIdeEvent();
		}

		private void OnIdeEvent(dbgEventReason reason)
		{
			OnIdeEvent();
		}

		private void OnIdeEvent(dbgEventReason reason, ref dbgExecutionAction executionaction)
		{
			OnIdeEvent();
		}

		private void OnIdeEvent(Process newProc, Program newProg, EnvDTE.Thread newThread, StackFrame newStkFrame)
		{
			OnIdeEvent();
		}

		private void OnIdeEvent()
		{
			if (UISettings.EnableDebugMode)
				WriteOutput("Debugger context changed. Updating title.");
			UpdateWindowTitleAsync(this, EventArgs.Empty);
		}

		private void OnIdeSolutionEvent(string oldname)
		{
			ClearCachedSettings();
			OnIdeEvent();
		}

		// clear settings cache and update
		private void OnIdeSolutionEvent()
		{
			ClearCachedSettings();
			OnIdeEvent();
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initilaization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			CurrentPackage = this;

			GlobalSettingsWatcher.SettingsCleared = OnSettingsCleared;
			SolutionSettingsWatcher.SettingsCleared = OnSettingsCleared;

			// update the title every 5 seconds for debugging
			this.ResetTitleTimer = new System.Windows.Forms.Timer { Interval = 5000 };
			this.ResetTitleTimer.Tick += this.UpdateWindowTitleAsync;
			this.ResetTitleTimer.Start();
		}


		protected override void Dispose(bool disposing)
		{
			this.ResetTitleTimer.Dispose();
			base.Dispose(disposing: disposing);
		}

		#endregion


		private GlobalSettingsPageGrid m_UISettings;

		internal GlobalSettingsPageGrid UISettings
		{
			get
			{
				if (this.m_UISettings == null)
				{
					this.m_UISettings = this.GetDialogPage(typeof(GlobalSettingsPageGrid)) as GlobalSettingsPageGrid;  // as is faster than cast
					this.m_UISettings.SettingsChanged += (s, e) => this.OnIdeSolutionEvent();
				}
				return this.m_UISettings;
			}
		}

		private string GetIDEName(string str)
		{
			try
			{
				var m = new Regex(@"^(.*) - (" + Globals.DTE.Name + ".*) $", RegexOptions.RightToLeft).Match(str);
				if (!m.Success)
				{
					m = new Regex(@"^(.*) - (" + Globals.DTE.Name + @".* \(.+\)) \(.+\)$", RegexOptions.RightToLeft).Match(str);
				}
				if (!m.Success)
				{
					m = new Regex(@"^(.*) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
				}
				if (!m.Success)
				{
					m = new Regex(@"^(" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
				}
				if (m.Success && m.Groups.Count >= 2)
				{
					if (m.Groups.Count >= 3)
					{
						return m.Groups[2].Captures[0].Value;
					}
					if (m.Groups.Count >= 2)
					{
						return m.Groups[1].Captures[0].Value;
					}
				}
				else
				{
					if (this.UISettings.EnableDebugMode)
					{
						WriteOutput("IDE name (" + Globals.DTE.Name + ") not found: " + str + ".");
					}
					return null;
				}
			}
			catch (Exception ex)
			{
				if (this.UISettings.EnableDebugMode)
				{
					WriteOutput("GetIDEName Exception: " + str + ". Details: " + ex);
				}
				return null;
			}
			return "";
		}

		private string GetVSSolutionName(string str)
		{
			return "Microsoft Visual Studio";
#if false
			try
			{
				var m = new Regex(@"^(.*)\\(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UISettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
				if (m.Success && m.Groups.Count >= 4)
				{
					var name = m.Groups[2].Captures[0].Value;
					var state = this.GetVSState(str);
					return name.Substring(0, name.Length - (string.IsNullOrEmpty(state) ? 0 : state.Length + 3));
				}
				m = new Regex("^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UISettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
				if (m.Success && m.Groups.Count >= 3)
				{
					var name = m.Groups[1].Captures[0].Value;
					var state = this.GetVSState(str);
					return name.Substring(0, name.Length - (string.IsNullOrEmpty(state) ? 0 : state.Length + 3));
				}
				m = new Regex("^(.*) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
				if (m.Success && m.Groups.Count >= 3)
				{
					var name = m.Groups[1].Captures[0].Value;
					var state = this.GetVSState(str);
					return name.Substring(0, name.Length - (string.IsNullOrEmpty(state) ? 0 : state.Length + 3));
				}
				if (this.UISettings.EnableDebugMode)
				{
					WriteOutput("VSName not found: " + str + ".");
				}
				return null;
			}
			catch (Exception ex)
			{
				if (this.UISettings.EnableDebugMode)
				{
					WriteOutput("GetVSName Exception: " + str + (". Details: " + ex));
				}
				return null;
			}
#endif
		}

		private string GetVSState(string str)
		{
			return "WHAT";
#if false
			if (string.IsNullOrWhiteSpace(str)) return null;
			try
			{
				var m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UISettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
				if (!m.Success)
				{
					m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
				}
				if (m.Success && m.Groups.Count >= 3)
				{
					return m.Groups[1].Captures[0].Value;
				}
				if (this.UISettings.EnableDebugMode)
				{
					WriteOutput("VSState not found: " + str + ".");
				}
				return null;
			}
			catch (Exception ex)
			{
				if (this.UISettings.EnableDebugMode)
				{
					WriteOutput("GetVSState Exception: " + str + (". Details: " + ex));
				}
				return null;
			}
#endif
		}

		private void UpdateWindowTitleAsync(object state, EventArgs e)
		{
			if (this.IDEName == null && Globals.DTE.MainWindow != null)
			{
				this.IDEName = this.GetIDEName(Globals.DTE.MainWindow.Caption);
				if (!string.IsNullOrWhiteSpace(this.IDEName))
				{
					try
					{
						var m = new Regex(@".*( \(.+\)).*$", RegexOptions.RightToLeft).Match(this.IDEName);
						if (m.Success)
						{
							this.ElevationSuffix = m.Groups[1].Captures[0].Value;
						}
					}
					catch (Exception ex)
					{
						if (this.UISettings.EnableDebugMode)
						{
							WriteOutput("UpdateWindowTitleAsync Exception: " + this.IDEName + (". Details: " + ex));
						}
					}
				}
			}
			if (this.IDEName == null)
			{
				return;
			}
			System.Threading.Tasks.Task.Factory.StartNew(this.UpdateWindowTitle);
		}

		private readonly object UpdateWindowTitleLock = new object();

		private void UpdateWindowTitle()
		{
			if (!Monitor.TryEnter(this.UpdateWindowTitleLock))
			{
				return;
			}
			try
			{
				var useDefaultPattern = true;

				Globals.GetVSMultiInstanceInfo(out Globals.VSMultiInstanceInfo info);
				if (info.nb_instances_same_solution >= 2)
				{
					useDefaultPattern = false;
				}
				else
				{
					var vsInstances = System.Diagnostics.Process.GetProcessesByName("devenv");
					try
					{
						if (vsInstances.Length >= 2)
						{
							//Check if multiple instances of devenv have identical original names. If so, then rewrite the title of current instance (normally the extension will run on each instance so no need to rewrite them as well). Otherwise do not rewrite the title.
							//The best would be to get the EnvDTE.DTE object of the other instances, and compare the solution or project names directly instead of relying on window titles (which may be hacked by third party software as well). But using moniker it will only work if they are launched with the same privilege.
							//var currentInstanceName = Path.GetFileNameWithoutExtension(Globals.DTE.Solution.FullName);
							//if (string.IsNullOrEmpty(currentInstanceName) || (from vsInstance in vsInstances
							//													where vsInstance.Id != VsProcessIdResolver.VsProcessId.Value
							//													select this.GetVSSolutionName(vsInstance.MainWindowTitle)).Any(vsInstanceName => vsInstanceName != null && currentInstanceName == vsInstanceName))
							//{
							//	useDefaultPattern = false;
							//}
						}
					}
					finally
					{
						foreach (var p in vsInstances)
						{
							p.Dispose();
						}
					}
				}

				var solution = Globals.DTE.Solution;
				var solution_filepath = solution?.FullName;

				var settings = this.GetSettings(solution_filepath);

				var pattern = GetPattern(solution_filepath, useDefaultPattern, settings);
				this.ChangeWindowTitle(GetNewTitle(solution, pattern, settings));
			}
			catch (Exception ex)
			{
				try
				{
					if (this.UISettings.EnableDebugMode)
					{
						WriteOutput("UpdateWindowTitle exception: " + ex);
					}
				}
				catch
				{
					// ignored
				}
			}
			finally
			{
				Monitor.Exit(this.UpdateWindowTitleLock);
			}
		}

		readonly SettingsWatcher SolutionSettingsWatcher = new SettingsWatcher(false);
		readonly SettingsWatcher GlobalSettingsWatcher = new SettingsWatcher(true);

		private void ClearCachedSettings()
		{
			if (this.UISettings.EnableDebugMode)
			{
				WriteOutput("Clearing cached settings...");
			}

			this.SolutionSettingsWatcher.Clear();
			this.GlobalSettingsWatcher.Clear();
			this.CachedSettings = null;
			if (this.UISettings.EnableDebugMode)
			{
				WriteOutput("Clearing cached settings... Completed.");
			}
		}

		private void OnSettingsCleared()
		{
			this.CachedSettings = null; // force reload
		}

		private Settings CachedSettings;
		internal Settings GetSettings(string solution_filepath)
		{
			if (CachedSettings != null && CachedSettings.SolutionFilePath == solution_filepath)
				return CachedSettings;

			// init values from settings
			var settings = new Settings
			{
				SolutionPattern = UISettings.PatternIfSolutionOpen
			};

			if (!string.IsNullOrEmpty(solution_filepath))
			{
				settings.SolutionFilePath = solution_filepath;
				settings.SolutionFileName = Path.GetFileName(solution_filepath);
				settings.SolutionName = Path.GetFileNameWithoutExtension(solution_filepath);

				if (!GlobalSettingsWatcher.Update(settings))
					SolutionSettingsWatcher.Update(settings);
			}

			CachedSettings = settings;
			return settings;
		}

		private string GetPattern(string solution_filepath, bool useDefault, Settings settingsOverride)
		{
			var Settings = UISettings;
			if (string.IsNullOrEmpty(solution_filepath))
			{
				if (string.IsNullOrEmpty(Globals.DTE.ActiveDocument?.FullName) && string.IsNullOrEmpty(Globals.DTE.ActiveWindow?.Caption))
					return useDefault ? Globals.DefaultPatternIfNothingOpen : Settings.PatternIfNothingOpen;
				return useDefault ? Globals.DefaultPatternIfDocumentOpen : Settings.PatternIfDocumentButNoSolutionOpen;
			}

			return "LMAO";

#if false
			string designModePattern = null;
			string breakModePattern = null;
			string runningModePattern = null;
			if (!useDefault)
			{
				designModePattern = settingsOverride?.PatternIfDesignMode ?? Settings.PatternIfDesignMode;
				breakModePattern = settingsOverride?.PatternIfBreakMode ?? Settings.PatternIfBreakMode;
				runningModePattern = settingsOverride?.PatternIfRunningMode ?? Settings.PatternIfRunningMode;
			}

			if (Globals.DTE.Debugger == null || Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode)
			{
				return designModePattern ?? DefaultPatternIfDesignMode;
			}
			if (Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode)
			{
				return breakModePattern ?? DefaultPatternIfBreakMode;
			}
			if (Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgRunMode)
			{
				return runningModePattern ?? DefaultPatternIfRunningMode;
			}
			throw new Exception("No matching state found");
#endif
		}

		readonly Regex TagRegex = new Regex(@"\[([^\[\]]+)\]", RegexOptions.Multiline | RegexOptions.Compiled);

		internal string GetNewTitle(Solution solution, string pattern, Settings cfg)
		{
			var info = AvailableInfo.GetCurrent(ideName: this.IDEName, solution: solution, cfg: cfg, globalSettings: this.UISettings);
			if (info == null) return this.IDEName;

#if false
			pattern = this.TagRegex.Replace(pattern, match =>
			{
				try
				{
					var tag = match.Groups[1].Value;
					try
					{
						if (SimpleTagResolvers.TryGetValue(tag, out ISimpleTagResolver resolver))
						{
							return resolver.Resolve(info: info);
						}
						foreach (var tagResolver in this.TagResolvers)
						{
							if (tagResolver.TryResolve(tag: tag, info: info, s: out string value))
							{
								return value;
							}
						}
						return match.Value;
					}
					catch (Exception ex)
					{
						if (this.UISettings.EnableDebugMode)
						{
							WriteOutput("ReplaceTag (" + tag + ") failed: " + ex);
						}
						throw;
					}
				}
				catch
				{
					return "";
				}
			});
#endif

			return pattern;
		}

		private void ChangeWindowTitle(string title)
		{
			try
			{
				TitleBarNonePackage.BeginInvokeOnUIThread(() =>
				{
					try
					{
						System.Windows.Application.Current.MainWindow.Title = Globals.DTE.MainWindow.Caption;
						if (System.Windows.Application.Current.MainWindow.Title != title)
							System.Windows.Application.Current.MainWindow.Title = title;
					}
					catch (Exception)
					{
					}
				});
			}
			catch (Exception ex)
			{
				if (UISettings.EnableDebugMode)
				{
					WriteOutput("ChangeWindowTitle failed: " + ex);
				}
			}
		}

		public static void WriteOutput(string str, params object[] args)
		{
			try
			{
				InvokeOnUIThread(() =>
				{
					var outWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
					var generalPaneGuid = VSConstants.OutputWindowPaneGuid.DebugPane_guid;
					// P.S. There's also the VSConstants.GUID_OutWindowDebugPane available.
					if (outWindow != null)
					{
						IVsOutputWindowPane generalPane;
						outWindow.GetPane(ref generalPaneGuid, out generalPane);
						generalPane.OutputString("TitleBarNonePackage: " + string.Format(str, args) + "\r\n");
						generalPane.Activate();
					}
				});
			}
			catch
			{
				// ignored
			}
		}

		private static void InvokeOnUIThread(Action action)
		{
			var dispatcher = System.Windows.Application.Current.Dispatcher;
			dispatcher?.Invoke(action);
		}

		private static void BeginInvokeOnUIThread(Action action)
		{
			var dispatcher = System.Windows.Application.Current.Dispatcher;
			dispatcher?.BeginInvoke(action);
		}

	}

	internal static class DocumentHelper
	{
		public static string GetActiveDocumentProjectNameOrEmpty(Document activeDocument)
		{
			return activeDocument?.ProjectItem?.ContainingProject?.Name ?? string.Empty;
		}

		public static string GetActiveDocumentProjectFileNameOrEmpty(Document activeDocument)
		{
			var fn = activeDocument?.ProjectItem?.ContainingProject?.FullName;
			return fn != null ? Path.GetFileName(fn) : string.Empty;
		}

		public static string GetActiveDocumentNameOrEmpty(Document activeDocument)
		{
			return activeDocument != null ? Path.GetFileName(activeDocument.FullName) : string.Empty;
		}

		public static string GetActiveWindowNameOrEmpty(Window activeWindow)
		{
			if (activeWindow != null && activeWindow.Caption != Globals.DTE.MainWindow.Caption)
				return activeWindow.Caption ?? string.Empty;
			return string.Empty;
		}

		public static string GetActiveDocumentPathOrEmpty(Document activeDocument)
		{
			return activeDocument != null ? activeDocument.FullName : string.Empty;
		}
	}

	public class AvailableInfo
	{
		private AvailableInfo()
		{
			DocumentName = DocumentHelper.GetActiveDocumentNameOrEmpty(ActiveDocument);
			DocumentPath = DocumentHelper.GetActiveDocumentPathOrEmpty(ActiveDocument);
			WindowName = DocumentHelper.GetActiveWindowNameOrEmpty(ActiveWindow);

			Path = string.IsNullOrEmpty(Solution?.FullName) ? DocumentPath : Solution.FullName;

			PathParts = SplitPath(Path);
			if (!string.IsNullOrEmpty(Path))
				PathParts[0] = System.IO.Path.GetPathRoot(Path).Replace("\\", "");

			DocumentPathParts = SplitPath(DocumentPath);
			if (!string.IsNullOrEmpty(DocumentPath))
				DocumentPathParts[0] = System.IO.Path.GetPathRoot(DocumentPath).Replace("\\", "");
		}

		public string IdeName { get; private set; }
		public Solution Solution { get; private set; }
		public GlobalSettingsPageGrid GlobalSettings { get; private set; }
		public Settings Cfg { get; private set; }
		public Document ActiveDocument { get; private set; }
		public Window ActiveWindow { get; private set; }
		public string DocumentName { get; private set; }
		public string Path { get; private set; }
		public string[] PathParts { get; private set; }
		public string[] DocumentPathParts { get; private set; }
		public string DocumentPath { get; private set; }
		public string WindowName { get; private set; }
		public string ElevationSuffix { get; private set; }

		private bool IsInvalidThings()
		{
			return ActiveDocument == null && string.IsNullOrEmpty(Solution?.FullName)
				&& (ActiveWindow == null || ActiveWindow.Caption == Globals.DTE.MainWindow.Caption);
		}

		public static AvailableInfo GetCurrent(string ideName, Solution solution, Settings cfg, GlobalSettingsPageGrid globalSettings)
		{
			var info = new AvailableInfo
			{
				IdeName = ideName,
				Solution = solution,
				GlobalSettings = globalSettings,
				Cfg = cfg,
				ActiveDocument = Globals.DTE.ActiveDocument,
				ActiveWindow = Globals.DTE.ActiveWindow
			};

			if (info.IsInvalidThings())
				return null;

			return info;
		}

		private static string[] SplitPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return new string[0];
			}

			var root = System.IO.Path.GetPathRoot(path);
			var parts = new List<string>();
			if (!string.IsNullOrEmpty(root))
			{
				parts.Add(root);
			}
			parts.AddRange(path.Substring(root.Length).Split(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));
			return parts.ToArray();
		}

		public static string GetParentPath(string[] pathParts, int closestParentDepth, int farthestParentDepth)
		{
			if (closestParentDepth > farthestParentDepth)
			{
				// swap if provided in wrong order
				var t = closestParentDepth;
				closestParentDepth = farthestParentDepth;
				farthestParentDepth = t;
			}
			pathParts = pathParts.Reverse().Skip(closestParentDepth)
								 .Take(farthestParentDepth - closestParentDepth + 1)
								 .Reverse()
								 .ToArray();
			if (pathParts.Length >= 2 && pathParts[0].EndsWith(":", StringComparison.Ordinal))
			{
				pathParts[0] += System.IO.Path.DirectorySeparatorChar;
			}
			return System.IO.Path.Combine(pathParts);
		}

		
	}
}