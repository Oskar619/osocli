using System;
using System.Threading.Tasks;
using osocli;
using System.CommandLine.Parsing;
using System.CommandLine;

namespace osodots
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            var commandRoot = new RootCommandController("OsoDOTS CLI used to analyze DOTS projects and get metadata from the files.");
            commandRoot.Init();
            int result;
            try
            {
                result = await commandRoot.InvokeAsync(args);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }

            return result;
        }
    }
}
