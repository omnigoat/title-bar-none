using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;

namespace Atma.TitleBarNone
{
	public static class Globals
	{
		public static DTE2 DTE;

		public const string SolutionSettingsOverrideExtension = ".rn.xml";
		public const string PathTag = "Path";
		public const string SolutionNameTag = "solution-name";
		public const string SolutionPatternTag = "solution-pattern";


		public const string DefaultPatternIfNothingOpen = "$ide-name";
		public const string DefaultPatternIfDocumentOpen = "$document-name - $ide-name";
		public const string DefaultPatternIfSolutionOpen = "$solution-name ${$ide-mode }- $ide-name";


		public static readonly Regex IndexRegex = new Regex(@"^:?(?<index>[0-9]+)$", RegexOptions.Compiled);
		public static readonly Regex RangeRegex = new Regex(@"^:(?<startIndex>[0-9]+):(?<endIndex>[0-9]+)$", RegexOptions.Compiled);

#if false
		public static string GetPathForTitle(string[] pathRange)
		{
			if (pathRange.Any())
			{
				if (pathRange.Length >= 2 && pathRange[0].EndsWith(":", StringComparison.Ordinal))
				{
					pathRange = pathRange.ToArray();
					pathRange[0] += Path.DirectorySeparatorChar;
				}
				return Path.Combine(pathRange);
			}
			return string.Empty;
		}

		public static void InvokeOnUIThread(Action action)
		{
			var dispatcher = System.Windows.Application.Current.Dispatcher;
			dispatcher?.Invoke(action);
		}

		public static void BeginInvokeOnUIThread(Action action)
		{
			var dispatcher = System.Windows.Application.Current.Dispatcher;
			dispatcher?.BeginInvoke(action);
		}
#endif

		[DllImport("ole32.dll")]
		static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable pprot);
		[DllImport("ole32.dll")]
		static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

		static readonly Regex m_DTEComObjectNameRegex = new Regex(@"^!VisualStudio\.DTE\.(?<dte_version>\d+\.\d+).*$", RegexOptions.Compiled);

		public struct VSMultiInstanceInfo
		{
			public bool multiple_instances;
			public bool multiple_instances_same_version;
			public int nb_instances_same_solution;
		}

		public static void GetVSMultiInstanceInfo(out VSMultiInstanceInfo vs_instance_info)
		{
			GetVSMultiInstanceInfo(out vs_instance_info, DTE.Version, DTE.Solution);
		}

		public static void GetVSMultiInstanceInfo(out VSMultiInstanceInfo vs_instance_info, string our_dte_version, Solution solution)
		{
			vs_instance_info.multiple_instances = false;
			vs_instance_info.multiple_instances_same_version = false;
			vs_instance_info.nb_instances_same_solution = 0;
			try
			{
				if (VSConstants.S_OK != GetRunningObjectTable(0, out IRunningObjectTable running_object_table))
					return;
				running_object_table.EnumRunning(out IEnumMoniker moniker_enumerator);
				moniker_enumerator.Reset();

				var monikers = new IMoniker[1];
				var num_fetched = IntPtr.Zero;
				int dte_count = 0;
				int dte_count_our_version = 0;
				//Will only return if same privilege as per http://stackoverflow.com/questions/11835617/understanding-the-running-object-table 
				while (VSConstants.S_OK == moniker_enumerator.Next(1, monikers, num_fetched))
				{
					if (VSConstants.S_OK != CreateBindCtx(0, out IBindCtx ctx))
						continue;

					monikers[0].GetDisplayName(ctx, null, out string name);
					if (!name.StartsWith("!VisualStudio.DTE."))
						continue;

					if (VSConstants.S_OK != running_object_table.GetObject(monikers[0], out object com_object))
						continue;

					var dte = com_object as DTE2;
					if (dte != null)
					{
						var s = dte.Solution;
						if (s != null)
						{
							var sn = Path.GetFileNameWithoutExtension(s.FullName);
							if (!string.IsNullOrEmpty(sn) && solution != null && sn == Path.GetFileNameWithoutExtension(solution.FullName))
							{
								vs_instance_info.nb_instances_same_solution++;
							}
						}
						++dte_count;
						var m = m_DTEComObjectNameRegex.Match(name);
						if (m.Success)
						{
							var g = m.Groups["dte_version"];
							if (g.Success && g.Value == our_dte_version)
								++dte_count_our_version;
						}
					}
				}
				vs_instance_info.multiple_instances = dte_count > 1;
				vs_instance_info.multiple_instances_same_version = dte_count_our_version > 1;
			}
			catch
			{
				vs_instance_info.multiple_instances = false;
				vs_instance_info.multiple_instances_same_version = false;
			}
		}
	}
}
