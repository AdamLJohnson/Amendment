using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;

namespace Amendment.Model.ViewModel.ScreenView
{
    public class HomeScreenViewViewModel
    {
        public AmendmentViewViewModel Amendment { get; set; }
        public List<AmendmentBodyViewViewModel> AmendmentBodies { get; set; }
        public List<Language> Languages { get; set; }
    }
}
