using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.ViewModel.Amendment;
using AutoMapper;

namespace Amendment.Model.Mapping
{
    public class AmendmentProfile : Profile
    {
        public AmendmentProfile()
        {
            CreateMap<DataModel.Amendment, AmendmentDetailsViewModel>();
            CreateMap<DataModel.Amendment, AmendmentEditViewModel>();
            CreateMap<AmendmentEditViewModel, DataModel.Amendment>()
                .ForMember(dest => dest.AmendmentBodies, opt => opt.Ignore());
            CreateMap<AmendmentCreateViewModel, DataModel.Amendment>();
        }
    }
}
