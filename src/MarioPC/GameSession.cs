namespace MarioPC;

public sealed class GameSession(Profile profile, string root) : IDisposable
{
    public InputState Input {get;}=new(profile);
    public RetroCore? Core {get;private set;}
    public bool Paused {get;private set;}
    public Bitmap? WorldmapFrame {get;private set;}
    int touchFrames;
    Point pulseTarget;
    public void Start(bool resume=true,bool sound=true)
    {
        Core?.Dispose();Core=null;Input.Clear();Paused=false;touchFrames=0;PointerHeld=false;
        WorldmapFrame?.Dispose();WorldmapFrame=null;
        Core=new RetroCore(Path.Combine(root,"game.nds"),root,Input,sound){Volume=profile.Volume};
        if(resume && File.Exists(Path.Combine(root,"resume.state")))
        {
            try{Core.LoadState(File.ReadAllBytes(Path.Combine(root,"resume.state")));}
            catch{Core.Dispose();Core=null;throw new InvalidDataException("O snapshot não pôde ser lido. Use 'Iniciar pelo menu original'; seu save original foi preservado.");}
        }
        if(resume && File.Exists(Path.Combine(root,"last-worldmap.png")))
        {
            try{using var b=new Bitmap(Path.Combine(root,"last-worldmap.png"));if(b.Width==256 && b.Height==384)WorldmapFrame=new Bitmap(b);}catch{ /* Map preview is optional; the save remains usable. */ }
        }
    }
    public void Pause(bool pause)
    {Paused=pause;Input.Release();touchFrames=0;PointerHeld=false;Core?.Silence();}
    public void PulseTouch(Point target){if(Core!=null && !Paused){pulseTarget=target;touchFrames=4;}}
    public void Reserve(){if(Core?.Scene==3)PulseTouch(new Point(215,152));}
    public static bool UseMenuSurface(int scene,bool hasContent)=>scene==4 && hasContent;
    public bool Bottom => UseMenuSurface(Core?.Scene??-1,Core?.BottomHasContent??false);
    public static readonly Point[] WorldTargets=[new(20,138),new(68,160),new(98,133),new(120,173),new(147,137),new(172,163),new(199,138),new(225,170)];
    public void Tick()
    {
        if(Core==null||Paused)return;
        if(touchFrames>0){Input.Touch=pulseTarget;touchFrames--;}
        else if(!PointerHeld)Input.Touch=null;
        Core.Volume=profile.Volume;Core.Step();
        if(Core.Scene==9)
        {
            WorldmapFrame??=new Bitmap(256,384);
            using var g=Graphics.FromImage(WorldmapFrame);g.DrawImageUnscaled(Core.Frame,0,0);
        }
    }
    public bool PointerHeld {get;set;}
    public void SaveCheckpoint()
    {
        if(Core==null)return;
        string path=Path.Combine(root,"resume.state"),temp=path+".tmp";
        File.WriteAllBytes(temp,Core.SaveState());
        if(File.Exists(path))File.Copy(path,path+".bak",true);
        File.Move(temp,path,true);
        if(WorldmapFrame!=null)
        {
            string map=Path.Combine(root,"last-worldmap.png");WorldmapFrame.Save(map+".tmp",System.Drawing.Imaging.ImageFormat.Png);File.Move(map+".tmp",map,true);
        }
    }
    public void Dispose(){Core?.Dispose();Core=null;WorldmapFrame?.Dispose();WorldmapFrame=null;}
}
