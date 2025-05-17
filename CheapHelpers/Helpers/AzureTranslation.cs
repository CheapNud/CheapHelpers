using System.Collections.Generic;

namespace CheapHelpers
{
	public class DetectedLanguage
	{
		public string Language { get; set; }
		public double Score { get; set; }
	}

	public class AzureTranslation
	{
		public DetectedLanguage DetectedLanguage { get; set; }
		public List<Translation> Translations { get; set; }
	}

	public class Translation
	{
		public string Text { get; set; }
		public string To { get; set; }
	}
}
