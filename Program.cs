using System.Diagnostics;

namespace GetEndscene.Interact
{
    class GetEndscene
    {
        private static Process getProcesses()
        {
            uint gameVersionOffset = 0x00837C04;

            Process[] procs = Process.GetProcessesByName("WoW");
            foreach (Process p in procs)
            {
                BmWrapper.memory.OpenProcessAndThread(p.Id);
                string version = BmWrapper.memory.ReadASCIIString(gameVersionOffset, 6);
                Console.WriteLine("game_version: " + version);
                if (version == "1.12.1")
                {
                    Console.WriteLine("BaseAddress: " + Process.GetProcessById(p.Id).Modules[0].BaseAddress);
                    return p;
                }
            }
            return Process.GetProcessById(0);
        }


        static void Main(string[] args)
        {
            // Simple tool to retrieve address of endscene for WoW client version 1.12.1.
            // Endscene itself doesn't have a static offset, but there exists a function at a static offset that calls it
            // This tool creates a hook at that function's address, jumps to our code to grab the address of the endscene, then returns to the original location
            // Uses Blackmagic (https://github.com/acidburn974/Blackmagic) for memory reading and writing
            // Very old implementation but the same idea works for a variety of different games
            Process p = getProcesses();
            if (p.Id == 0) { Console.WriteLine("1.12.1 WoW client is not open"); return; }

            Endscene.Inject();
            Console.WriteLine("Endscene at: " + Endscene.Address.ToString("X"));

        }
    }
}