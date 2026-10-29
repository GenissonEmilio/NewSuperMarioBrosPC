using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarioPC;

public enum ActionKey { Up, Down, Left, Right, Jump, Run, Reserve, Confirm, Back, GamePause, Map, Pause, Fullscreen }

public sealed class Profile
{
    public Dictionary<ActionKey, Keys> Bindings { get; set; } = Defaults();
    public int Volume { get; set; } = 75;
    public bool Smooth { get; set; }
    public bool Fullscreen { get; set; }
    public static Dictionary<ActionKey, Keys> Defaults() => new()
    {
        [ActionKey.Up]=Keys.W, [ActionKey.Down]=Keys.S, [ActionKey.Left]=Keys.A, [ActionKey.Right]=Keys.D,
        [ActionKey.Jump]=Keys.Space, [ActionKey.Run]=Keys.ShiftKey, [ActionKey.Reserve]=Keys.E,
        [ActionKey.Confirm]=Keys.Enter, [ActionKey.Back]=Keys.Back, [ActionKey.GamePause]=Keys.P,
        [ActionKey.Map]=Keys.M, [ActionKey.Pause]=Keys.Escape, [ActionKey.Fullscreen]=Keys.F11
    };
    static readonly JsonSerializerOptions Options = new() { WriteIndented=true, Converters={new JsonStringEnumConverter()} };
    public static Profile Load(string root)
    {
        Profile p;
        try { p=JsonSerializer.Deserialize<Profile>(File.ReadAllText(Path.Combine(root,"profile.json")),Options) ?? new(); }
        catch { p=new(); }
        p.Bindings??=Defaults();
        var used=new HashSet<Keys>();
        foreach(var (a,k) in Defaults())
            if(!p.Bindings.TryGetValue(a,out var assigned) || !Allowed(assigned) || !used.Add(assigned))
            { p.Bindings[a]=k; }
        // A damaged profile must never leave ambiguous hotkeys.
        if(p.Bindings.Values.Distinct().Count()!=Defaults().Count) p.Bindings=Defaults();
        p.Volume=Math.Clamp(p.Volume,0,100);
        return p;
    }
    public void Save(string root)
    {
        Directory.CreateDirectory(root);
        string path=Path.Combine(root,"profile.json"), temp=path+".tmp";
        File.WriteAllText(temp,JsonSerializer.Serialize(this,Options));
        File.Move(temp,path,true);
    }
    public static bool Allowed(Keys key) => key is not (Keys.None or Keys.LWin or Keys.RWin or Keys.Menu or Keys.F4 or Keys.ProcessKey) && Enum.IsDefined(key);
    public bool Bind(ActionKey action, Keys key)
    {
        key=Normalize(key);
        if(!Allowed(key)) return false;
        var previous=Bindings[action];
        var conflict=Bindings.FirstOrDefault(p=>p.Key!=action && p.Value==key);
        if(Bindings.Any(p=>p.Key!=action && p.Value==key)) Bindings[conflict.Key]=previous;
        Bindings[action]=key;
        return true;
    }
    public static Keys Normalize(Keys key) => key switch { Keys.LShiftKey or Keys.RShiftKey=>Keys.ShiftKey, Keys.LControlKey or Keys.RControlKey=>Keys.ControlKey, Keys.LMenu or Keys.RMenu=>Keys.Menu, _=>key & Keys.KeyCode };
    public string Label(ActionKey a) => Label(Bindings[a]);
    public static string Label(Keys k) => k switch { Keys.Space=>"Espaço", Keys.ShiftKey=>"Shift", Keys.ControlKey=>"Ctrl", Keys.Escape=>"Esc", Keys.Back=>"Backspace", Keys.Enter=>"Enter", _=>k.ToString() };
}

public sealed class InputState(Profile profile)
{
    readonly HashSet<Keys> held=[];
    readonly HashSet<Keys> blocked=[];
    public void Down(Keys k) { k=Profile.Normalize(k); if(!blocked.Contains(k))held.Add(k); }
    public void Up(Keys k) { k=Profile.Normalize(k);held.Remove(k);blocked.Remove(k); }
    public bool Is(ActionKey a)=>held.Contains(profile.Bindings[a]);
    public void Release() { blocked.UnionWith(held);held.Clear();Touch=null; }
    public void Clear() { held.Clear();blocked.Clear();Touch=null; }
    public Point? Touch {get;set;}
    public ushort Joypad
    {
        get
        {
            ushort mask=0;
            void Set(ActionKey a,int id){if(Is(a)) mask|=(ushort)(1<<id);}
            Set(ActionKey.Up,4);Set(ActionKey.Down,5);Set(ActionKey.Left,6);Set(ActionKey.Right,7);
            // Both DS jump buttons and both run buttons keep the PC mapping stable under either original scheme.
            Set(ActionKey.Jump,0);Set(ActionKey.Jump,8);Set(ActionKey.Run,1);Set(ActionKey.Run,9);
            Set(ActionKey.Confirm,8);Set(ActionKey.Back,0);Set(ActionKey.GamePause,3);
            return mask;
        }
    }
}
