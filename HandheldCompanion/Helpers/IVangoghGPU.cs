using System;
using System.Collections.Generic;

namespace HandheldCompanion.Helpers
{
    public interface IVangoghGPU
    {
        Dictionary<string, uint> All { get; }
        uint FastTDP { get; set; }
        uint FClock { get; }
        uint GfxClock { get; }
        uint HardMinGfxClock { set; }
        uint IfVersion { get; }
        uint MaxCPUClock { set; }
        uint MinCPUClock { set; }
        uint SlowTDP { get; set; }
        VangoghGPU.Features SmuFeatures { get; }
        uint SMUVersion { get; }
        uint SoftMaxGfxClock { set; }
        uint SoftMinGfxClock { set; }

        VangoghGPU.DetectionStatus Detect();
        void Dispose();
        VangoghGPU? Open();
        VangoghGPU? Open(Tuple<string, ulong, ulong, uint[]> device);
    }
}