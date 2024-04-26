namespace SubtitleEditor.Pages.SectionDef
{
	public class EditableClip
	{
		public string Source { get; set; }
		protected EditableClip() { }
	}
	public class AudioClip : EditableClip
	{
		public AudioClip() { }
	}
	public class VideoClip : EditableClip
	{
		public VideoClip() { }
	}
	public class PhotoClip : EditableClip
	{
		public PhotoClip() { }
	}
}
