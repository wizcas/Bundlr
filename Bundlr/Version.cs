using System;
using System.IO;

namespace Bundlr
{
	public struct Version
	{
		public byte major, minor, revision;

		public int Size
		{
			get{ return 3; }
		}

		public Version(byte major, byte minor, byte revision)
		{
			this.major = major;
			this.minor = minor;
			this.revision = revision;
		}

		public override string ToString ()
		{
			return string.Format ("v{0}.{1}.{2}", major, minor, revision);
		}

		public void Serialize(Stream s)
		{
			s.WriteByte (major);
			s.WriteByte (minor);
			s.WriteByte (revision);
		}

		public static Version Deserialize(Stream s)
		{
			var maj = (byte)s.ReadByte ();
			var min = (byte)s.ReadByte ();
			var rev = (byte)s.ReadByte ();
			return new Version (maj, min, rev);
		}
	}
}

