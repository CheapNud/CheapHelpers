namespace CheapHelpers.Models.Dtos.Translation
{
    public class AzureTranslation
    {
        public DetectedLanguage DetectedLanguage { get; set; }
        public List<Translation> Translations { get; set; }
    }
}
