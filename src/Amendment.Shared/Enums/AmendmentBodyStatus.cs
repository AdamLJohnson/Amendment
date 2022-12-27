using System.ComponentModel.DataAnnotations;

namespace Amendment.Shared.Enums
{
    public enum AmendmentBodyStatus
    {
        New = 0,
        Draft = 1,
        [Display(Name = "Under Review")]
        UnderReview = 2,
        Ready = 3
    }
}