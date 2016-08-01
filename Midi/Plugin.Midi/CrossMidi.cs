using Plugin.Midi.Abstractions;
using System;

namespace Plugin.Midi
{
	/// <summary>
	/// Cross platform Midi implemenations
	/// </summary>
	public class CrossMidi
	{
		static Lazy<IMidi> Implementation = new Lazy<IMidi> (() => CreateMidi (), System.Threading.LazyThreadSafetyMode.PublicationOnly);

		/// <summary>
		/// Current settings to use
		/// </summary>
		public static IMidi Current {
			get {
				var ret = Implementation.Value;
				if (ret == null) {
					throw NotImplementedInReferenceAssembly ();
				}
				return ret;
			}
		}

		static IMidi CreateMidi ()
		{
#if PORTABLE
			return null;
#else
			return new MidiImplementation ();
#endif
		}

		internal static Exception NotImplementedInReferenceAssembly ()
		{
			return new NotImplementedException ("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
		}
	}
}
