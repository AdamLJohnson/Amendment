using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;

namespace Amendment.Model.ViewModel.ScreenView
{
    public class ScreenViewViewModel
    {
        public AmendmentViewViewModel Amendment { get; set; }
        public AmendmentBodyViewViewModel AmendmentBody { get; set; }
        public Language Language { get; set; }
    }
}
