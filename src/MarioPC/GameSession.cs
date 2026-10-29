namespace MarioPC;

public sealed class GameSession(Profile profile, string root) : IDisposable
{
    public InputState Input {get;}=new(profile);
    public RetroCore? Core {get;private set;}
    public bool Paused {get;private set;}
    public bool Alternate {get;private set;}
    int reserveFrames;
    public void Start(bool resume=true,bool sound=true)
    {
        Core?.Dispose();Core=null;Input.Clear();Paused=false;Alternate=false;
        Core=new RetroCore(Path.Combine(root,"game.nds"),root,Input,sound){Volume=profile.Volume};
        if(resume && File.Exists(Path.Combine(root,"resume.state")))
        {
            try{Core.LoadState(File.ReadAllBytes(Path.Combine(root,"resume.state")));}
            catch{Core.Dispose();Core=null;throw new InvalidDataException("O snapshot não pôde ser lido. Use 'Iniciar pelo menu original'; seu save original foi preservado.");}
        }
    }
    public void Pause(bool pause)
    {Paused=pause;Input.Release();reserveFrames=0;Core?.Silence();}
    public void Swap(){Alternate=!Alternate;Input.Release();}
    public void Reserve(){if(Core?.Scene==3 && !Paused)reserveFrames=4;}
    public bool Bottom => (profile.ScreenMode switch {"Top"=>false,"Bottom"=>true,_=>Core?.Scene==4 && Core.BottomHasContent}) ^ Alternate;
    public void Tick()
    {
        if(Core==null||Paused)return;
        if(reserveFrames>0){Input.Touch=new Point(215,152);reserveFrames--;}
        else if(!PointerHeld)Input.Touch=null;
        Core.Volume=profile.Volume;Core.Step();
    }
    public bool PointerHeld {get;set;}
    public void SaveCheckpoint()
    {
        if(Core==null)return;
        string path=Path.Combine(root,"resume.state"),temp=path+".tmp";
        File.WriteAllBytes(temp,Core.SaveState());
        if(File.Exists(path))File.Copy(path,path+".bak",true);
        File.Move(temp,path,true);
    }
    public void Dispose(){Core?.Dispose();Core=null;}
}
