using System.Runtime.InteropServices;

namespace MarioPC;

sealed class WaveAudio : IDisposable
{
    [StructLayout(LayoutKind.Sequential)] struct WaveFormat {public ushort Tag,Channels;public uint Rate,BytesPerSecond;public ushort BlockAlign,Bits,Extra;}
    [StructLayout(LayoutKind.Sequential)] struct Header {public IntPtr Data;public uint Length,Recorded;public nuint User;public uint Flags,Loops;public IntPtr Next;public nuint Reserved;}
    [DllImport("winmm.dll")]static extern int waveOutOpen(out IntPtr h,uint device,ref WaveFormat format,IntPtr callback,IntPtr instance,uint flags);
    [DllImport("winmm.dll")]static extern int waveOutPrepareHeader(IntPtr h,IntPtr p,uint size);
    [DllImport("winmm.dll")]static extern int waveOutUnprepareHeader(IntPtr h,IntPtr p,uint size);
    [DllImport("winmm.dll")]static extern int waveOutWrite(IntPtr h,IntPtr p,uint size);
    [DllImport("winmm.dll")]static extern int waveOutReset(IntPtr h);
    [DllImport("winmm.dll")]static extern int waveOutClose(IntPtr h);
    readonly IntPtr handle;
    readonly List<(IntPtr header,IntPtr buffer)> buffers=[];
    readonly uint headerSize=(uint)Marshal.SizeOf<Header>();
    public WaveAudio()
    {
        var f=new WaveFormat{Tag=1,Channels=2,Rate=32768,BytesPerSecond=131072,BlockAlign=4,Bits=16};
        if(waveOutOpen(out handle,uint.MaxValue,ref f,IntPtr.Zero,IntPtr.Zero,0)!=0)throw new InvalidOperationException("waveOut");
        for(int i=0;i<8;i++)
        {
            var b=Marshal.AllocHGlobal(8192);var h=Marshal.AllocHGlobal((int)headerSize);
            Marshal.StructureToPtr(new Header{Data=b,Length=8192},h,false);
            waveOutPrepareHeader(handle,h,headerSize);buffers.Add((h,b));
        }
    }
    public unsafe void Queue(IntPtr data,int frames,int volume)
    {
        if(frames<=0 || frames>2048)return;
        foreach(var (h,b) in buffers)
        {
            var header=Marshal.PtrToStructure<Header>(h);
            if((header.Flags&16)!=0)continue; // WHDR_INQUEUE
            short* src=(short*)data;short* dst=(short*)b;
            for(int n=0;n<frames*2;n++)dst[n]=(short)(src[n]*Math.Clamp(volume,0,100)/100);
            header.Length=(uint)frames*4;Marshal.StructureToPtr(header,h,false);waveOutWrite(handle,h,headerSize);return;
        }
    }
    public void Reset()=>waveOutReset(handle);
    public void Dispose()
    {waveOutReset(handle);foreach(var(h,b)in buffers){waveOutUnprepareHeader(handle,h,headerSize);Marshal.FreeHGlobal(h);Marshal.FreeHGlobal(b);}waveOutClose(handle);}
}
