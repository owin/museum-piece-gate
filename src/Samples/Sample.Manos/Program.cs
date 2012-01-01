using System;
using Gate.Builder;
using Gate.Hosts.Manos;

namespace Sample.Manos
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Server.Create(AppBuilder.BuildConfiguration(), 8090))
            {
                Console.WriteLine("Running on port 8090. Press enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
