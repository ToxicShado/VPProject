using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Helpers
{
    public static class FileOperations
    {

        public static List<Battery> LoadData(string folderPath = "../BatteryData")
        {
            var data = new List<Battery>();

            if (Directory.Exists(folderPath))
            {

            }

            return data;
        }
    }
}
