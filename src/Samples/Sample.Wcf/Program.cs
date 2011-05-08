using System;
using Gate.Wcf;

namespace Sample.Wcf
{
    internal class Program
    {
        static readonly Uri BaseUri = new Uri("http://localhost:1234/");

        static void Main(string[] args)
        {
            //todo: look at args[] for uri and named startup

            using (GateWcfService.Create(BaseUri))
            {
                Console.WriteLine("Service is now running on: {0}", BaseUri);
                Console.ReadLine();
            }
        }
    }
}