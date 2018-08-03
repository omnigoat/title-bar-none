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
	[PackageRegistration(UseManagedResourcesOnly = true), InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string), ProvideMenuResource("Menus.ctmenu", 1)]
	//[Guid(GuidList.guidCustomizeVSWindowTitle2PkgString)]
	//[ProvideOptionPage(typeof(GlobalSettingsPageGrid), "Customize VS Window Title", "Global rules", 0, 0, true)]
	//[ProvideOptionPage(typeof(SettingsOverridesPageGrid), "Customize VS Window Title", "Solution-specific overrides", 51, 500, true)]
	//[ProvideOptionPage(typeof(SupportedTagsGrid), "Customize VS Window Title", "Supported tags", 101, 1000, true)]
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
		}

		public readonly string[] SupportedTags;

		private void OnIdeEvent(Window gotfocus, Window lostfocus)
		{
			this.OnIdeEvent();
		}

		private void OnIdeEvent(Document document)
		{
			this.OnIdeEvent();
		}

		private void OnIdeEvent(Window window)
		{
			this.OnIdeEvent();
		}

		private void OnIdeEvent(dbgEventReason reason)
		{
			this.OnIdeEvent();
		}

		private void OnIdeEvent(dbgEventReason reason, ref dbgExecutionAction executionaction)
		{
			this.OnIdeEvent();
		}

		private void OnIdeEvent(Process newProc, Program newProg, EnvDTE.Thread newThread, StackFrame newStkFrame)
		{
			this.OnIdeEvent();
		}

		private void OnIdeEvent()
		{
			if (this.UiSettings.EnableDebugMode)
			{
				WriteOutput("Debugger context changed. Updating title.");
			}
			this.UpdateWindowTitleAsync(this, EventArgs.Empty);
		}

		private void OnIdeSolutionEvent(string oldname)
		{
			this.ClearCachedSettings();
			this.OnIdeEvent();
		}

		// clear settings cache and update
		private void OnIdeSolutionEvent()
		{
			this.ClearCachedSettings();
			this.OnIdeEvent();
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


		private GlobalSettingsPageGrid _UiSettings;

		internal GlobalSettingsPageGrid UiSettings
		{
			get
			{
				if (this._UiSettings == null)
				{
					this._UiSettings = this.GetDialogPage(typeof(GlobalSettingsPageGrid)) as GlobalSettingsPageGrid;  // as is faster than cast
					this._UiSettings.SettingsChanged += (s, e) => this.OnIdeSolutionEvent();
				}
				return this._UiSettings;
			}
		}

		private SettingsOverridesPageGrid _UiSettingsOverridesOptions;

		internal SettingsOverridesPageGrid UiSettingsOverridesOptions
		{
			get
			{
				if (this._UiSettingsOverridesOptions == null)
				{
					this._UiSettingsOverridesOptions = this.GetDialogPage(typeof(SettingsOverridesPageGrid)) as SettingsOverridesPageGrid;  // as is faster than cast
					this._UiSettingsOverridesOptions.SettingsChanged += (s, e) => this.OnIdeSolutionEvent();
				}
				return this._UiSettingsOverridesOptions;
			}
		}

		private string GetIDEName(string str)
		{
			try
			{
				var m = new Regex(@"^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UiSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
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
					if (this.UiSettings.EnableDebugMode)
					{
						WriteOutput("IDE name (" + Globals.DTE.Name + ") not found: " + str + ".");
					}
					return null;
				}
			}
			catch (Exception ex)
			{
				if (this.UiSettings.EnableDebugMode)
				{
					WriteOutput("GetIDEName Exception: " + str + ". Details: " + ex);
				}
				return null;
			}
			return "";
		}

		private string GetVSSolutionName(string str)
		{
			try
			{
				var m = new Regex(@"^(.*)\\(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UiSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
				if (m.Success && m.Groups.Count >= 4)
				{
					var name = m.Groups[2].Captures[0].Value;
					var state = this.GetVSState(str);
					return name.Substring(0, name.Length - (string.IsNullOrEmpty(state) ? 0 : state.Length + 3));
				}
				m = new Regex("^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UiSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
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
				if (this.UiSettings.EnableDebugMode)
				{
					WriteOutput("VSName not found: " + str + ".");
				}
				return null;
			}
			catch (Exception ex)
			{
				if (this.UiSettings.EnableDebugMode)
				{
					WriteOutput("GetVSName Exception: " + str + (". Details: " + ex));
				}
				return null;
			}
		}

		private string GetVSState(string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return null;
			try
			{
				var m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UiSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
				if (!m.Success)
				{
					m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
				}
				if (m.Success && m.Groups.Count >= 3)
				{
					return m.Groups[1].Captures[0].Value;
				}
				if (this.UiSettings.EnableDebugMode)
				{
					WriteOutput("VSState not found: " + str + ".");
				}
				return null;
			}
			catch (Exception ex)
			{
				if (this.UiSettings.EnableDebugMode)
				{
					WriteOutput("GetVSState Exception: " + str + (". Details: " + ex));
				}
				return null;
			}
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
						if (this.UiSettings.EnableDebugMode)
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
				if (this.UiSettings.AlwaysRewriteTitles)
				{
					useDefaultPattern = false;
				}
				else
				{
					Globals.VSMultiInstanceInfo info;
					Globals.GetVSMultiInstanceInfo(out info);
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
								var currentInstanceName = Path.GetFileNameWithoutExtension(Globals.DTE.Solution.FullName);
								if (string.IsNullOrEmpty(currentInstanceName) || (from vsInstance in vsInstances
																				  where vsInstance.Id != VsProcessIdResolver.VsProcessId.Value
																				  select this.GetVSSolutionName(vsInstance.MainWindowTitle)).Any(vsInstanceName => vsInstanceName != null && currentInstanceName == vsInstanceName))
								{
									useDefaultPattern = false;
								}
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
				}
				var solution = Globals.DTE.Solution;
				var solutionFp = solution?.FullName;

				var settings = this.GetSettings(solutionFp);

				var pattern = this.GetPattern(solutionFp, useDefaultPattern, settings);
				this.ChangeWindowTitle(this.GetNewTitle(solution, pattern, settings));
			}
			catch (Exception ex)
			{
				try
				{
					if (this.UiSettings.EnableDebugMode)
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
			if (this.UiSettings.EnableDebugMode)
			{
				WriteOutput("Clearing cached settings...");
			}

			this.SolutionSettingsWatcher.Clear();
			this.GlobalSettingsWatcher.Clear();
			this.CachedSettings = null;
			if (this.UiSettings.EnableDebugMode)
			{
				WriteOutput("Clearing cached settings... Completed.");
			}
		}

		private void OnSettingsCleared()
		{
			this.CachedSettings = null; // force reload
		}

		private SettingsSet CachedSettings;
		internal SettingsSet GetSettings(string solutionFp)
		{
			this.GlobalSettingsWatcher.Update(this.UiSettingsOverridesOptions.GlobalSolutionSettingsOverridesFp);
			this.SolutionSettingsWatcher.Update(string.IsNullOrEmpty(solutionFp) ? null : solutionFp + Globals.SolutionSettingsOverrideExtension);

			// config already loaded, use cache
			if (this.CachedSettings != null && this.CachedSettings.SolutionFilePath == solutionFp)
			{
				return this.CachedSettings;
			}

			// init values from settings
			var settings = new SettingsSet
			{
				ClosestParentDepth = this.UiSettings.ClosestParentDepth,
				FarthestParentDepth = this.UiSettings.FarthestParentDepth,
				AppendedString = this.UiSettings.AppendedString,
				PatternIfBreakMode = this.UiSettings.PatternIfBreakMode,
				PatternIfDesignMode = this.UiSettings.PatternIfDesignMode,
				PatternIfRunningMode = this.UiSettings.PatternIfRunningMode,
			};

			if (!string.IsNullOrEmpty(solutionFp))
			{
				settings.SolutionFilePath = solutionFp;
				settings.SolutionFileName = Path.GetFileName(solutionFp);
				settings.SolutionName = Path.GetFileNameWithoutExtension(solutionFp);

				if (!this.UiSettingsOverridesOptions.AllowSolutionSettingsOverrides)
				{
					// Do nothing
				}
				else if (this.GlobalSettingsWatcher.Update(settings))
				{
					// Do nothing
				}
				else if (this.SolutionSettingsWatcher.Update(settings))
				{
					// Do nothing
				}
			}
			this.CachedSettings = settings;
			return settings;
		}

		private string GetPattern(string solutionFp, bool useDefault, SettingsSet settingsOverride)
		{
			var Settings = this.UiSettings;
			if (string.IsNullOrEmpty(solutionFp))
			{
				var document = Globals.DTE.ActiveDocument;
				var window = Globals.DTE.ActiveWindow;
				if (string.IsNullOrEmpty(document?.FullName) && string.IsNullOrEmpty(window?.Caption))
				{
					return useDefault ? DefaultPatternIfNothingOpen : Settings.PatternIfNothingOpen;
				}
				return useDefault ? DefaultPatternIfDocumentButNoSolutionOpen : Settings.PatternIfDocumentButNoSolutionOpen;
			}
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
		}

		readonly Regex TagRegex = new Regex(@"\[([^\[\]]+)\]", RegexOptions.Multiline | RegexOptions.Compiled);

		internal string GetNewTitle(Solution solution, string pattern, SettingsSet cfg)
		{
			var info = AvailableInfo.GetCurrent(ideName: this.IDEName, solution: solution, cfg: cfg, globalSettings: this.UiSettings);
			if (info == null) return this.IDEName;

			pattern = this.TagRegex.Replace(pattern, match =>
			{
				try
				{
					var tag = match.Groups[1].Value;
					try
					{
						if (this.SimpleTagResolvers.TryGetValue(tag, out ISimpleTagResolver resolver))
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
						if (this.UiSettings.EnableDebugMode)
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
			var appendedString = cfg?.AppendedString ?? this.UiSettings.AppendedString;
			return pattern + " " + appendedString;
		}

		private void ChangeWindowTitle(string title)
		{
			try
			{
				Globals.BeginInvokeOnUIThread(() =>
				{
					try
					{
						System.Windows.Application.Current.MainWindow.Title = Globals.DTE.MainWindow.Caption;
						if (System.Windows.Application.Current.MainWindow.Title != title)
						{
							System.Windows.Application.Current.MainWindow.Title = title;
						}
						//foreach (var windowObj in System.Windows.Application.Current.Windows) {
						//    var window = windowObj as Microsoft.VisualStudio.PlatformUI.Shell.Controls.FloatingWindow;
						//    if (window != null) {
						//        window.Title = Globals.DTE.MainWindow.Caption;
						//        if (window.Title != title) {
						//            window.Title = title;
						//        }
						//    }
						//}
					}
					catch (Exception)
					{
						// ignored
					}
				});
			}
			catch (Exception ex)
			{
				if (this.UiSettings.EnableDebugMode)
				{
					WriteOutput("ChangeWindowTitle failed: " + ex);
				}
			}
		}

		public static void WriteOutput(string str, params object[] args)
		{
			try
			{
				Globals.InvokeOnUIThread(() =>
				{
					var outWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
					var generalPaneGuid = VSConstants.OutputWindowPaneGuid.DebugPane_guid;
					// P.S. There's also the VSConstants.GUID_OutWindowDebugPane available.
					if (outWindow != null)
					{
						IVsOutputWindowPane generalPane;
						outWindow.GetPane(ref generalPaneGuid, out generalPane);
						generalPane.OutputString("CustomizeVSWindowTitle: " + string.Format(str, args) + "\r\n");
						generalPane.Activate();
					}
				});
			}
			catch
			{
				// ignored
			}
		}
	}

	public class AvailableInfo
	{
		private AvailableInfo() { }

		public string IdeName { get; private set; }
		public Solution Solution { get; private set; }
		//public GlobalSettingsPageGrid GlobalSettings { get; private set; }
		public SettingsSet Cfg { get; private set; }
		public Document ActiveDocument { get; private set; }
		public Window ActiveWindow { get; private set; }
		public string DocumentName { get; private set; }
		public string Path { get; private set; }
		public string[] PathParts { get; private set; }
		public string[] DocumentPathParts { get; private set; }
		public string DocumentPath { get; private set; }
		public string WindowName { get; private set; }
		public string ElevationSuffix { get; private set; }

		public static AvailableInfo GetCurrent(string ideName, Solution solution, SettingsSet cfg, GlobalSettingsPageGrid globalSettings)
		{
			var info = new AvailableInfo
			{
				IdeName = ideName,
				Solution = solution,
				GlobalSettings = globalSettings,
				Cfg = cfg,
				ElevationSuffix = TitleBarNone.CurrentPackage.ElevationSuffix
			};
			try
			{
				info.ActiveDocument = Globals.DTE.ActiveDocument;
			}
			catch
			{
				// Do nothing
			}
			try
			{
				info.ActiveWindow = Globals.DTE.ActiveWindow;
			}
			catch
			{
				// Do nothing
			}
			var solutionFp = info.Solution?.FullName;
			if (info.ActiveDocument == null && string.IsNullOrEmpty(solutionFp))
			{
				if (info.ActiveWindow == null || info.ActiveWindow.Caption == Globals.DTE.MainWindow.Caption)
				{
					return null;
				}
			}
			info.DocumentName = DocumentHelper.GetActiveDocumentNameOrEmpty(info.ActiveDocument);
			info.DocumentPath = DocumentHelper.GetActiveDocumentPathOrEmpty(info.ActiveDocument);
			info.WindowName = DocumentHelper.GetActiveWindowNameOrEmpty(info.ActiveWindow);

			if (!string.IsNullOrEmpty(solutionFp))
			{
				info.Path = solutionFp;
			}
			else
			{
				info.Path = info.DocumentPath;
			}

			info.PathParts = SplitPath(info.Path);
			if (!string.IsNullOrEmpty(info.Path))
			{
				info.PathParts[0] = System.IO.Path.GetPathRoot(info.Path).Replace("\\", "");
			}

			info.DocumentPathParts = SplitPath(info.DocumentPath);
			if (!string.IsNullOrEmpty(info.DocumentPath))
			{
				info.DocumentPathParts[0] = System.IO.Path.GetPathRoot(info.DocumentPath).Replace("\\", "");
			}
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