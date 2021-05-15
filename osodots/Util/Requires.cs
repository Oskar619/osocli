using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osodots.Util
{
    public static class Requires
    {
        public static void NotNull<T>(T arg, string argName)
        {
            if (string.IsNullOrWhiteSpace(arg?.ToString()))
            {
                throw new ArgumentNullException($"Argument {argName} is null or empty.");
            }
        }
    }
}
