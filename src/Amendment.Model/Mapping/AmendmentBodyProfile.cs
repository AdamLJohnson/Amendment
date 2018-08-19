using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Model.ViewModel.AmendmentBody;
using AutoMapper;

namespace Amendment.Model.Mapping
{
    public class AmendmentBodyProfile : Profile
    {
        public AmendmentBodyProfile()
        {
            CreateMap<AmendmentBody, AmendmentBodyDetailsViewModel>();
            CreateMap<AmendmentBody, AmendmentBodyEditViewModel>();
            CreateMap<AmendmentBodyEditViewModel, AmendmentBody>();
            CreateMap<AmendmentBodyCreateViewModel, AmendmentBody>();
        }
    }
}
