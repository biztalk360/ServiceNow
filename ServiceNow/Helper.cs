using System;
using System.IO;
using System.Reflection;

namespace B360.Notifier.ServiceNowNotification
{
    class Helper
    {
        internal static string GetResourceFileContent(string filename)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string name = assembly.GetName().Name;

            using (Stream stream = assembly.GetManifestResourceStream(name + "." + filename))
            {
                if (stream == null)
                    throw new Exception(
                        string.Format("Cannot read {0} make sure the file exists and it's an embeded resource", filename));

                using (StreamReader sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
