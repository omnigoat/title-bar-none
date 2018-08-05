using System;
using System.Collections.Generic;
using System.Linq;

namespace Atma.TitleBarNone.Informers
{
	public interface IInformer
	{
		IEnumerable<string> TagNames { get; }

		bool RewriteTag(out string result, AvailableInfo info, string tag);
	}

	public abstract class Informer : IInformer
	{
		protected Informer() { }
		protected Informer(IDictionary<string, Func<AvailableInfo, string>> tags)
		{
			m_TagsToFunctions = tags;
		}

		protected Informer(string tag_name, Func<AvailableInfo, string> callback)
			: this(new Dictionary<string, Func<AvailableInfo, string>> { { tag_name, callback } })
		{}

		protected void AddTag(string tag_name, Func<AvailableInfo, string> callback)
		{
			m_TagsToFunctions.Add(tag_name, callback);
		}

		public IEnumerable<string> TagNames => m_TagsToFunctions.Keys;

		public bool RewriteTag(out string result, AvailableInfo info, string tag)
		{
			if (m_TagsToFunctions.TryGetValue(tag, out var f))
			{
				result = f.Invoke(info);
				return true;
			}
			else
			{
				result = null;
				return false;
			}
		}

		private IDictionary<string, Func<AvailableInfo, string>> m_TagsToFunctions;
	}
}
