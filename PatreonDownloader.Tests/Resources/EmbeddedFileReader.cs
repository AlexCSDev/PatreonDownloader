using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PatreonDownloader.Tests.Resources
{
    internal static class EmbeddedFileReader
    {
        /// <summary>
        /// Read embedded resource from assembly
        /// </summary>
        /// <typeparam name="TSource">Any class contained in the assembly to read resource from</typeparam>
        /// <param name="embeddedFileName">Name of the resource. Path delimiter: .</param>
        /// <returns>String with contents of embedded resource</returns>
        public static string ReadEmbeddedFile<TSource>(string embeddedFileName) where TSource : class
        {
            var assembly = typeof(TSource).GetTypeInfo().Assembly;
            var resourcesList = assembly.GetManifestResourceNames();
            var resourceName = resourcesList.First(s => s.EndsWith(embeddedFileName, StringComparison.CurrentCultureIgnoreCase));

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Could not load manifest resource stream.");
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
