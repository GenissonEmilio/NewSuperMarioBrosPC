using System.Text.Json;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace MarioPC;

public static class Verification
{
    static readonly JsonSerializerOptions Json=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    public sealed class Step {public int Frames{get;set;}public string? Key{get;set;}public int[]? Touch{get;set;}public string? Name{get;set;}public string? Command{get;set;}}
    public static int Run(string[] args)
    {
        if(args.Length<2)throw new ArgumentException("--verify ROM OUTPUT [SCRIPT.json]");
        string output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
        string root=Path.Combine(output,"test-data");RomImport.Import(args[0],root);
        var p=new Profile();using var game=new GameSession(p,root);game.Start(false,false);
        var ui=new Presentation {Panel=Panel.Play,HasRom=true};
        var results=new List<object>();var checks=new List<object>();var timer=Stopwatch.StartNew();
        void Check(string name,bool pass){checks.Add(new{name,pass});}
        void Capture(string name)
        {
            game.Core!.Frame.Save(Path.Combine(output,name+"-ds.png"),ImageFormat.Png);
            using var image=new Bitmap(1280,800);using var g=Graphics.FromImage(image);ui.Draw(g,image.Size,p,game);image.Save(Path.Combine(output,name+"-pc.png"),ImageFormat.Png);
            File.WriteAllBytes(Path.Combine(output,name+"-ram.bin"),game.Core.ReadRam());
            results.Add(new{name,game.Core.Frames,game.Core.Scene,game.Bottom,game.Core.AudioFrames,game.Core.TouchFrames,game.Core.LastJoypad});
        }
        var steps=args.Length>2?JsonSerializer.Deserialize<Step[]>(File.ReadAllText(args[2]),Json)!:
            [new(){Frames=240,Name="boot"},new(){Frames=3,Key="Confirm"},new(){Frames=150,Name="title"},
             new(){Frames=3,Key="Confirm"},new(){Frames=180,Name="files"},new(){Frames=3,Key="Confirm"},
             new(){Frames=600,Name="intro"},new(){Frames=3,Key="GamePause"},new(){Frames=600,Name="world"}];
        foreach(var s in steps)
        {
            game.Input.Clear();game.PointerHeld=false;
            if(s.Key!=null)foreach(var key in s.Key.Split('+'))game.Input.Down(p.Bindings[Enum.Parse<ActionKey>(key)]);
            if(s.Touch is {Length:2}){game.Input.Touch=new(s.Touch[0],s.Touch[1]);game.PointerHeld=true;}
            if(s.Command=="pause")game.Pause(true);
            if(s.Command=="resume")game.Pause(false);
            if(s.Command=="swap")game.Swap();
            if(s.Command=="reserve")game.Reserve();
            long before=game.Core!.Frames;
            for(int n=0;n<s.Frames;n++)game.Tick();
            if(game.Paused)Check("pause freezes simulation",game.Core.Frames==before);
            if(s.Name!=null)Capture(s.Name);
        }
        Check("video nonblank",game.Core!.Frame.GetPixel(120,90).ToArgb()!=Color.Black.ToArgb());
        Check("audio generated",game.Core.AudioFrames>1000);
        game.Pause(false);game.Input.Clear();
        var state=game.Core.SaveState();var ramBefore=game.Core.ReadRam();
        game.Input.Down(p.Bindings[ActionKey.Right]);for(int n=0;n<30;n++)game.Tick();
        game.Core.LoadState(state);
        Check("snapshot RAM roundtrip",game.Core.ReadRam().SequenceEqual(ramBefore));
        game.SaveCheckpoint();Check("snapshot written",File.Exists(Path.Combine(root,"resume.state")));
        var doc=new{success=checks.All(c=>(bool)c.GetType().GetProperty("pass")!.GetValue(c)!),desktopInput=false,visibleWindows=false,elapsedSeconds=timer.Elapsed.TotalSeconds,checks,results};
        File.WriteAllText(Path.Combine(output,"verification.json"),JsonSerializer.Serialize(doc,Json));
        return doc.success?0:1;
    }
    public static int SelfTest(string output)
    {
        Directory.CreateDirectory(output);var checks=new List<object>();
        void Check(string name,bool pass){checks.Add(new{name,pass});}
        var p=new Profile();var input=new InputState(p);
        Check("WASD defaults",p.Bindings[ActionKey.Up]==Keys.W&&p.Bindings[ActionKey.Down]==Keys.S&&p.Bindings[ActionKey.Left]==Keys.A&&p.Bindings[ActionKey.Right]==Keys.D);
        Check("Space jumps by default",p.Bindings[ActionKey.Jump]==Keys.Space);
        input.Down(Keys.Space);Check("jump maps both original schemes",(input.Joypad&257)==257);input.Up(Keys.Space);
        input.Down(Keys.D);Check("right reaches core",input.Joypad==128);input.Release();Check("pause releases input",input.Joypad==0);
        input.Down(Keys.D);Check("held resume key suppressed",input.Joypad==0);input.Up(Keys.D);input.Down(Keys.D);Check("new key press after resume",input.Joypad==128);input.Clear();
        p.Bind(ActionKey.Jump,Keys.D);Check("duplicate swaps previous assignment",p.Bindings[ActionKey.Jump]==Keys.D&&p.Bindings[ActionKey.Right]==Keys.Space);
        input.Down(Keys.D);Check("remapping changes gameplay",input.Joypad==257);input.Clear();
        Check("Windows key rejected",!p.Bind(ActionKey.Jump,Keys.LWin));
        Check("modifier normalized",Profile.Normalize(Keys.RShiftKey)==Keys.ShiftKey);
        p.Save(output);var restored=Profile.Load(output);Check("bindings persist",restored.Bindings[ActionKey.Jump]==Keys.D);
        var ui=new Presentation{HasRom=true};
        foreach(var size in new[]{new Size(1280,800),new Size(1024,768),new Size(1920,1080)})
        {
            using var b=new Bitmap(size.Width,size.Height);using var g=Graphics.FromImage(b);ui.Draw(g,size,p,null);
            float scale=Math.Min(size.Width/1280f,size.Height/800f);PointF canvas=new(720,230);
            var pixel=new Point((int)(canvas.X*scale+(size.Width-1280*scale)/2),(int)(canvas.Y*scale+(size.Height-800*scale)/2));
            var actual=Presentation.ToCanvas(pixel,size);Check("scaled import/start hit "+size,ui.Hits.First().Rect.Contains(actual));
            b.Save(Path.Combine(output,$"menu-{size.Width}x{size.Height}.png"));
        }
        foreach(var panel in new[]{Panel.Controls,Panel.Settings,Panel.Pause})
        {ui.Panel=panel;using var b=new Bitmap(1280,800);using var g=Graphics.FromImage(b);ui.Draw(g,b.Size,p,null);b.Save(Path.Combine(output,panel+".png"));Check("menu clickable "+panel,ui.Hits.Count>0);}
        bool rejected=false;try{RomImport.Validate(new byte[1024]);}catch(InvalidDataException){rejected=true;}Check("invalid ROM rejected",rejected);
        var result=new{success=checks.All(c=>(bool)c.GetType().GetProperty("pass")!.GetValue(c)!),checks};
        File.WriteAllText(Path.Combine(output,"self-test.json"),JsonSerializer.Serialize(result,Json));return result.success?0:1;
    }
}
