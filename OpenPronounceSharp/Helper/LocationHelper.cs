using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OpenPronounceSharp.Helper
{
    public class LocationHelper
    {
        public static string Location => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string CACHE_DIR => Path.Combine(Location, "Cache");
        public static string TEMP_DIR => Path.Combine(Location, "Temp");

        static LocationHelper()
        {
            if (!Directory.Exists(CACHE_DIR))
            {
                Directory.CreateDirectory(CACHE_DIR);
            }

            if (!Directory.Exists(TEMP_DIR))
            {
                Directory.CreateDirectory(TEMP_DIR);
            }
        }
    }
}
