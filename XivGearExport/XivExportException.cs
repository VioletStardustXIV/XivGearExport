using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XivGearExport
{
    public class XivExportException : Exception
    {
        public XivExportException(string? message = null) : base(message)
        {
        }
    }
}
