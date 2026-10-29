using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MarioPC;

// Minimal libretro frontend. The original DS program runs in melonDS, not as native game code.
public sealed class RetroCore : IDisposable
{
    const string Lib="melonds_libretro.dll";
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] delegate bool EnvironmentCallback(uint cmd,IntPtr data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void VideoCallback(IntPtr data,uint w,uint h,nuint pitch);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void AudioCallback(short left,short right);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate nuint AudioBatchCallback(IntPtr data,nuint frames);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void PollCallback();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate short InputCallback(uint port,uint device,uint index,uint id);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_set_environment(EnvironmentCallback cb);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_set_video_refresh(VideoCallback cb);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_set_audio_sample(AudioCallback cb);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_set_audio_sample_batch(AudioBatchCallback cb);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_set_input_poll(PollCallback cb);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_set_input_state(InputCallback cb);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_init();
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_deinit();
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool retro_load_game(ref GameInfo info);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_unload_game();
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern void retro_run();
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr retro_get_memory_data(uint id);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] static extern nuint retro_serialize_size();
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool retro_serialize(IntPtr data,nuint size);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool retro_unserialize(IntPtr data,nuint size);
    [StructLayout(LayoutKind.Sequential)] struct GameInfo {public IntPtr path,data;public nuint size;public IntPtr meta;}
    readonly EnvironmentCallback env;
    readonly VideoCallback video;
    readonly AudioCallback audio;
    readonly AudioBatchCallback batch;
    readonly PollCallback poll;
    readonly InputCallback input;
    readonly Dictionary<string,IntPtr> strings=[];
    readonly string root;
    readonly InputState state;
    readonly WaveAudio? output;
    readonly IntPtr gameInfoPointer;
    IntPtr rom;
    bool loaded,disposed;
    public Bitmap Frame {get;}=new(256,384,PixelFormat.Format32bppRgb);
    public long Frames {get;private set;}
    public long AudioFrames {get;private set;}
    public long TouchFrames {get;private set;}
    public int Scene => loaded ? Marshal.ReadInt16(retro_get_memory_data(2),0x3bd34) : -1;
    public bool BottomHasContent
    {
        get
        {
            int lit=0;
            for(int y=208;y<384;y+=32)for(int x=16;x<256;x+=32)
            {var c=Frame.GetPixel(x,y);if(c.R+c.G+c.B>60)lit++;}
            return lit>=4;
        }
    }
    public int Volume {get;set;}=75;
    public ushort LastJoypad {get;private set;}
    public string? Warning {get;private set;}
    public RetroCore(string path,string dataRoot,InputState state,bool sound=true)
    {
        root=dataRoot;this.state=state;Directory.CreateDirectory(root);
        if(sound){try{output=new WaveAudio();}catch{Warning="Áudio indisponível; verifique seu dispositivo de saída.";}}
        env=Environment;video=Video;audio=(_,_)=>{};batch=AudioBatch;poll=()=>{};input=Input;
        var bytes=RomImport.Read(path);rom=Marshal.AllocHGlobal(bytes.Length);Marshal.Copy(bytes,0,rom,bytes.Length);
        var info=new GameInfo {path=Str(path),data=rom,size=(nuint)bytes.Length};
        // Upstream keeps this pointer for retro_reset; retain both struct and ROM for the core's lifetime.
        gameInfoPointer=Marshal.AllocHGlobal(Marshal.SizeOf<GameInfo>());Marshal.StructureToPtr(info,gameInfoPointer,false);
        retro_set_environment(env);retro_set_video_refresh(video);retro_set_audio_sample(audio);retro_set_audio_sample_batch(batch);
        retro_set_input_poll(poll);retro_set_input_state(input);retro_init();
        // P/Invoke ref storage is not retained by us; load uses unmanaged storage below.
        if(!LoadAt(gameInfoPointer)) { Dispose();throw new InvalidOperationException("O núcleo não conseguiu carregar a ROM."); }
        loaded=true;
    }
    [DllImport(Lib,EntryPoint="retro_load_game",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool LoadAt(IntPtr info);
    IntPtr Str(string s)
    {if(!strings.TryGetValue(s,out var p)){p=Marshal.StringToCoTaskMemUTF8(s);strings.Add(s,p);}return p;}
    bool Environment(uint command,IntPtr p)
    {
        uint cmd=command & 0xffff;
        switch(cmd)
        {
            case 9: case 31: Marshal.WriteIntPtr(p,Str(root));return true;
            case 10:return Marshal.ReadInt32(p)==1; // XRGB8888
            case 15:
                var key=Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(p))??"";
                string? value=key switch {
                    "melonds_console_mode"=>"DS","melonds_boot_directly"=>"enabled",
                    "melonds_use_fw_settings"=>"disabled","melonds_language"=>"English",
                    "melonds_screen_layout"=>"Top/Bottom","melonds_screen_gap"=>"0",
                    "melonds_touch_mode"=>"Touch","melonds_opengl_renderer"=>"disabled",
                    "melonds_threaded_renderer"=>"enabled","melonds_jit_enable"=>"enabled",
                    "melonds_jit_block_size"=>"32","melonds_audio_interpolation"=>"Linear",_=>null};
                Marshal.WriteIntPtr(p,IntPtr.Size,value==null?IntPtr.Zero:Str(value));return value!=null;
            case 17:Marshal.WriteByte(p,0);return true;
            case 18:Marshal.WriteByte(p,0);return true; // no standalone games
            case 3:Marshal.WriteByte(p,0);return true;
            case 47:Marshal.WriteIntPtr(p,Str("Player"));return true;
            case 51: return true; // bitmask input
            case 52: Marshal.WriteInt32(p,0);return true; // legacy variables
            case 11:case 16:case 35:case 37:case 53:case 54:case 55:case 65:case 67:case 69:return true;
            default:return false;
        }
    }
    unsafe void Video(IntPtr data,uint w,uint h,nuint pitch)
    {
        if(data==IntPtr.Zero || data==(IntPtr)(-1) || w!=256 || h!=384)return;
        var bits=Frame.LockBits(new Rectangle(0,0,256,384),ImageLockMode.WriteOnly,PixelFormat.Format32bppRgb);
        try{for(int y=0;y<384;y++)Buffer.MemoryCopy((byte*)data+y*(long)pitch,(byte*)bits.Scan0+y*bits.Stride,1024,1024);}
        finally{Frame.UnlockBits(bits);}
    }
    nuint AudioBatch(IntPtr data,nuint count)
    {AudioFrames+=(long)count;output?.Queue(data,checked((int)count),Volume);return count;}
    short Input(uint port,uint device,uint index,uint id)
    {
        if(port!=0)return 0;
        if(device==1)return id==256 ? unchecked((short)state.Joypad) : (short)((state.Joypad>>(int)id)&1);
        if(device==6 && state.Touch is Point t)
        {
            return id switch {0=>(short)Math.Clamp((t.X+.5)*65536/256-32768,-32768,32767),
                1=>(short)Math.Clamp((t.Y+192+.5)*65536/384-32768,-32768,32767),2=>1,_=>0};
        }
        return 0;
    }
    public void Step()
    {LastJoypad=state.Joypad;if(state.Touch!=null)TouchFrames++;retro_run();Frames++;}
    public void Silence()=>output?.Reset();
    public byte[] SaveState()
    {
        int size=checked((int)retro_serialize_size());if(size==0 || size>32*1024*1024)throw new InvalidOperationException("Snapshot não disponível.");
        IntPtr p=Marshal.AllocHGlobal(size);
        try{if(!retro_serialize(p,(nuint)size))throw new InvalidOperationException("Falha ao salvar snapshot.");var b=new byte[size];Marshal.Copy(p,b,0,size);return b;}
        finally{Marshal.FreeHGlobal(p);}
    }
    public void LoadState(byte[] bytes)
    {
        IntPtr p=Marshal.AllocHGlobal(bytes.Length);
        try{Marshal.Copy(bytes,0,p,bytes.Length);if(!retro_unserialize(p,(nuint)bytes.Length))throw new InvalidOperationException("Snapshot incompatível.");}
        finally{Marshal.FreeHGlobal(p);}state.Release();Silence();
    }
    public byte[] ReadRam(){var b=new byte[0x400000];Marshal.Copy(retro_get_memory_data(2),b,0,b.Length);return b;}
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        if(loaded)retro_unload_game();retro_deinit();loaded=false;output?.Dispose();Frame.Dispose();
        Marshal.FreeHGlobal(rom);Marshal.FreeHGlobal(gameInfoPointer);
        foreach(var p in strings.Values)Marshal.FreeCoTaskMem(p);
        GC.KeepAlive(env);GC.KeepAlive(video);GC.KeepAlive(batch);GC.KeepAlive(audio);GC.KeepAlive(input);GC.KeepAlive(poll);
    }
}
