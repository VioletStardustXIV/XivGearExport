using System;

namespace XivGearExport
{
    public class XivExportException : Exception
    {
        public XivExportException(string? message = null) : base(message)
        {
        }
    }
}
