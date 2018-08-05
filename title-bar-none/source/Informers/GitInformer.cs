using System.IO;
using System.Text;
using EnvDTE;

namespace Atma.TitleBarNone.Informers
{
	public class GitInformer : Informer
	{
		public GitInformer()
		{
			AddTag("git-branch", ResolveBranch);
		}

		public string ResolveBranch(AvailableInfo info)
		{

			//UpdateGitExecFp(info.GlobalSettings.GitDirectory); // there is likely a better way to adjust the git path
			//return GetGitBranchNameOrEmpty(info.Solution);
			return "lulz";
		}

		public static string GetGitBranchNameOrEmpty(Solution solution)
		{
			var sn = solution?.FullName;
			if (string.IsNullOrEmpty(sn)) return string.Empty;
			var working_dir = new FileInfo(sn).DirectoryName;
			return IsGitRepository(working_dir) ? GetBranch(working_dir) ?? string.Empty : string.Empty;
		}

		public static string GetBranch(string working_dir)
		{
			using (var process = new System.Diagnostics.Process
			{
				StartInfo = {
					FileName = ExecutableFilepath,
					//As per: http://git-blame.blogspot.sg/2013/06/checking-current-branch-programatically.html. Or: "rev-parse --abbrev-ref HEAD"
					Arguments = "symbolic-ref --short -q HEAD",
					UseShellExecute = false,
					StandardOutputEncoding = Encoding.UTF8,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					WorkingDirectory = working_dir
				}
			})
			{
				process.Start();
				var branch = process.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');
				process.WaitForExit();
				return branch;
			}
		}

		public const string ExecutableFilename = "git.exe";
		private static string ExecutableFilepath = ExecutableFilename;

		public static void UpdateGitExecFp(string git_directory)
		{
			if (string.IsNullOrEmpty(git_directory))
				ExecutableFilepath = ExecutableFilename;
			else
				ExecutableFilepath = Path.Combine(git_directory, ExecutableFilename);
		}

		public static bool IsGitRepository(string working_dir)
		{
			using (var process = new System.Diagnostics.Process
			{
				StartInfo = {
					FileName = ExecutableFilepath,
					Arguments = "rev-parse --is-inside-work-tree",
					UseShellExecute = false,
					StandardOutputEncoding = Encoding.UTF8,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					WorkingDirectory = working_dir
				}
			})
			{
				process.Start();
				var res = process.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');
				process.WaitForExit();
				return res == "true";
			}
		}
	}
}