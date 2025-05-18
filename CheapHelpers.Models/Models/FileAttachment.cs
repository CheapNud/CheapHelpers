namespace CheapHelpers.Models
{
	public abstract class FileAttachment : IEntityId
	{
		public int Id { get; set; }
		public string FileName { get; set; }
		public bool Visible { get; set; } = true;
        public int DisplayIndex { get; set; }

		//TODO: Timestamps
	}
}