using System.Runtime.InteropServices;

namespace GetEndscene
{
    internal static class Endscene
    {
        [DllImport("kernel32", EntryPoint = "VirtualAllocEx")]
        private static extern uint VirtualAllocEx(IntPtr hProcess, uint dwAddress, int nSize, uint dwAllocationType, uint dwProtect);

        private static uint endsceneAddr = 0;

        internal static uint Address
        {
            get
            {
                return endsceneAddr;
            }
        }

        // We don't have a static offset to endscene, but we know a function at a static offset that calls it
        private static uint endsceneCallAddr = 0x5A17B6;

        private static uint overwrittenBytesLength = 0x6;

        private static byte[] originalBytes = BmWrapper.memory.ReadBytes(endsceneCallAddr, (int)overwrittenBytesLength);

        private static uint detourPtr;
        private static uint endscenePtr;

        internal static void Inject()
        {
            detourPtr = VirtualAllocEx(BmWrapper.memory.ProcessHandle, 0, 0x256, 0x00001000 | 0x00002000, 0x40);
            endscenePtr = VirtualAllocEx(BmWrapper.memory.ProcessHandle, 0, 0x4, 0x00001000 | 0x00002000, 0x40);

            if (detourPtr != 0 && endscenePtr != 0)
            {
                string[] detourJmp = new string[]
                {
                    // Detour overwrites 6 bytes. Jmp takes 5 bytes so need to append an extra NOP to avoid creating invalid OPcodes
                    "jmp " + (uint)detourPtr,
                    "nop",
                };

                string[] detour = new string[]
                {
                    // x86
                    // push all flags and registers to stack
                    "pushfd",
                    "pushad",
                    // ecx + 0xA8 contains the address we are looking for. Move it to endscenePtr
                    "mov eax, [ecx + 0xA8]",
                    "mov [" + endscenePtr + "], eax",
                    // restore the flags and registers
                    "popad",
                    "popfd",
                    // Original call that is overwritten with detourJmp
                    "call DWORD[ecx + 0xA8]",
                    // Jump back to address after detourJmp
                    "jmp " + (uint)(endsceneCallAddr + overwrittenBytesLength),
                };

                // Inject our detour
                BmWrapper.memory.Asm.Clear();
                foreach (string str in detour)
                {
                    BmWrapper.memory.Asm.AddLine(str);
                }
                BmWrapper.memory.Asm.Inject(detourPtr);

                // Inject our jump to detour from endsceneCallAddr
                BmWrapper.memory.Asm.Clear();
                foreach (string str in detourJmp)
                {
                    BmWrapper.memory.Asm.AddLine(str);
                }
                BmWrapper.memory.Asm.Inject(endsceneCallAddr);

                // Our detour saved the endscene address to endscenePtr
                endsceneAddr = BmWrapper.memory.ReadUInt(endscenePtr);

                // Clean up by restoring the original bytes to the address that we hooked and freeing our allocations
                BmWrapper.memory.WriteBytes(endsceneCallAddr, originalBytes);
                BmWrapper.memory.FreeMemory(endscenePtr);
                BmWrapper.memory.FreeMemory(detourPtr);
            }
        }
    }
}
