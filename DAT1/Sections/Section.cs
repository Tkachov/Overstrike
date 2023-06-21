namespace DAT1.Sections {
	public abstract class Section
	{
		abstract public byte[] Save();
	}

	public class UnknownSection: Section
	{
		public byte[] Raw;

		public UnknownSection(byte[] bytes) {
			Raw = bytes;
		}

		override public byte[] Save() { return Raw; }
	}
}
