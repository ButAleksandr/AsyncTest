using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace AsynTest
{
    static class OptionContainer
    {
        static OptionContainer()
        {
            TestFileName = ConfigurationSettings.AppSettings.Get("TestFileName");
            TestFilePath = ConfigurationSettings.AppSettings.Get("TestFilePath");
        }

        public static string TestFileName { get; }

        public static string TestFilePath { get; }

        public static string FullTestFilePath {
            get
            {
                return TestFilePath + TestFileName;
            }
        }
    }
}
