using System.ComponentModel.DataAnnotations;
using Amendment.Model.Infrastructure;

namespace Amendment.Model.DataModel
{
    public class Language : IReadOnlyTable
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string LanguageName { get; set; }
        public string LanguageCode { get; set; }
    }
}