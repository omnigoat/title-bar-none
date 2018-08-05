using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;

namespace Atma.TitleBarNone
{
	public class SettingsWatcher
	{
		readonly bool IsGlobalConfig;

		List<Settings> SettingsSets = new List<Settings>();
		bool IsReloadingNeeded;
		string Fp;
		FileSystemWatcher Watcher;

		static readonly char[] PathSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		public delegate void SettingsClearedDelegate();
		public SettingsClearedDelegate SettingsCleared;

		public SettingsWatcher(bool isGlobalConfig)
		{
			this.IsGlobalConfig = isGlobalConfig;
		}

		public void Clear()
		{
			this.SettingsSets.Clear();
			this.IsReloadingNeeded = true;
			this.StopWatching();
			this.SettingsCleared?.Invoke();
		}

		private void StopWatching()
		{
			if (this.Watcher != null)
			{
				// stop watching
				this.Watcher.EnableRaisingEvents = false;
				this.Watcher.Created -= this.Watcher_Changed;
				this.Watcher.Changed -= this.Watcher_Changed;
				this.Watcher.Renamed -= this.Watcher_Renamed;
				this.Watcher.Deleted -= this.Watcher_Deleted;
				this.Watcher.Dispose();
				this.Watcher = null;
			}
		}

		public void Update(string path)
		{
			if (this.Fp == path)
				return;
			this.Clear();
			this.Fp = path;
		}

		public static Regex CreatePathRegex(string path)
		{
			return new Regex("^" + Regex.Escape(path).
				Replace("\\*", ".*").
				Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
		}



		public bool Update(Settings settings)
		{
			if (IsReloadingNeeded)
			{
				TryReloadSettings();
			}

			foreach (var s in SettingsSets)
			{
				bool matched = false;
				if (!IsGlobalConfig)
				{
					// ignore paths
					matched = true;
				}
				else
				{
					matched = s.Paths.Any(path =>
					{
						bool matches_path = path.IndexOfAny(PathSeparators) == -1
							? path.Equals(s.SolutionFileName, StringComparison.CurrentCultureIgnoreCase)
							: path.Equals(s.SolutionFilePath, StringComparison.CurrentCultureIgnoreCase);
					
						return matches_path || (path.Contains("*") || path.Contains("?")) && CreatePathRegex(path).IsMatch(settings.SolutionFilePath);
					});
				}

				if (matched)
				{
					settings.Merge(s);
					return true;
				}
			}

			return false;
		}

		private void TryReloadSettings()
		{
			this.IsReloadingNeeded = false;
			if (string.IsNullOrEmpty(this.Fp))
				return;

			this.StopWatching();
			try
			{
				this.Watcher = new FileSystemWatcher(Path.GetDirectoryName(this.Fp), Path.GetFileName(this.Fp));
			}
			catch
			{
				// cannot setup watcher because of missing folder
				this.Clear();
				return;
			}
			this.Watcher.Created += this.Watcher_Created;
			this.Watcher.Changed += this.Watcher_Changed;
			this.Watcher.Renamed += this.Watcher_Renamed;
			this.Watcher.Deleted += this.Watcher_Deleted;

			this.LoadSettings();

			this.Watcher.EnableRaisingEvents = true;
		}

		private static string GetAttributeOrChild(XmlElement node, string name)
		{
			var attr = node.GetAttributeNode(name);
			if (attr != null)
				return attr.Value;
			var child = node.GetElementsByTagName(name);
			if (child.Count > 0)
				return child[0].InnerText;
			return null;
		}

		private static void TryUpdateSetting(ref string target, XmlElement node, string name)
		{
			try
			{
				var val = GetAttributeOrChild(node, name);
				if (string.IsNullOrEmpty(val))
					return;
				target = val;
			}
			catch
			{
				// do nothing
			}
		}

		private static void TryUpdateSetting(ref int? target, XmlElement node, string name)
		{
			try
			{
				var val = GetAttributeOrChild(node, name);
				if (string.IsNullOrEmpty(val))
					return;
				target = int.Parse(val, CultureInfo.InvariantCulture);
			}
			catch
			{
				// do nothing
			}
		}

		private static readonly string[] NodePaths = { "TitleBarNonePackage/Settings", "RenameVSWindowTitle/Settings" };
		private void LoadSettings()
		{
			this.SettingsSets = new List<Settings>();

			if (!File.Exists(this.Fp))
			{
				Debug.WriteLine("No settings overrides {0}", this.Fp);
				return;
			}

			var doc = new XmlDocument();
			Debug.WriteLine("Read settings overrides from {0}", this.Fp);
			try
			{
				doc.Load(this.Fp);
			}
			catch (Exception x)
			{
				Debug.WriteLine("Error {0}", x);
			}

			foreach (var np in NodePaths)
			{
				var nodes = doc.SelectNodes(np);
				if (nodes == null) continue;
				foreach (XmlElement node in nodes)
				{
					var settings = new Settings { Paths = new List<string>() };

					// read paths (Path attribute and Path child elements)
					var path = node.GetAttribute(Globals.PathTag);
					if (!string.IsNullOrEmpty(path))
						settings.Paths.Add(path);

					var paths = node.GetElementsByTagName(Globals.PathTag);
					if (paths.Count > 0)
					{
						foreach (XmlElement elem in paths)
						{
							path = elem.InnerText;
							if (!string.IsNullOrEmpty(path))
								settings.Paths.Add(path);
						}
					}

					TryUpdateSetting(ref settings.SolutionName, node, Globals.SolutionNameTag);
					TryUpdateSetting(ref settings.SolutionPattern, node, Globals.SolutionPatternTag);

					this.SettingsSets.Add(settings);
				}
			}
		}

		private void Watcher_Deleted(object sender, FileSystemEventArgs e)
		{
			try
			{
				this.Clear();
				//this.IsReloadingNeeded = false;
			}
			catch
			{
				// do nothing
			}
		}

		private void Watcher_Renamed(object sender, RenamedEventArgs e)
		{
			try
			{
				this.Clear();
			}
			catch
			{
				// do nothing
			}
		}

		private void Watcher_Changed(object sender, FileSystemEventArgs e)
		{
			try
			{
				this.Clear();
			}
			catch
			{
				// do nothing
			}
		}

		private void Watcher_Created(object sender, FileSystemEventArgs e)
		{
			try
			{
				this.Clear();
			}
			catch
			{
				// do nothing
			}
		}
	}
}
