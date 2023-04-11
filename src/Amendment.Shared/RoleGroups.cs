using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared
{
    public static class RoleGroups
    {
        public const string Admin = "System Administrator";
        public const string ScreenController = "Screen Controller";
        public const string AmendmentEditor = "Amendment Editor";
        public const string Translator = "Translator";

        public const string AdminScreenController = $"{Admin}, {ScreenController}";
        public const string AdminAmendEditor = $"{Admin}, {AmendmentEditor}";
        public const string AdminTranslatorAmendEditor = $"{Admin}, {Translator}, {AmendmentEditor}";
    }
}
