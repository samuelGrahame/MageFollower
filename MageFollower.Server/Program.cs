using System;

namespace MageFollower.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            string server = "";
            if (args != null && args.Length > 0)
            {
                server = args[0];
            }
            MageFollower.Program.StartServer(server);
        }
    }
}
