using System.Diagnostics;

namespace MarioPC;

public sealed class MainWindow : Form
{
    readonly string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GenissonEmilio","NewSuperMarioBrosPC");
    readonly Profile profile;
    readonly GameSession game;
    readonly Presentation ui=new();
    readonly HashSet<Keys> physical=[];
    readonly System.Windows.Forms.Timer timer=new(){Interval=8};
    readonly Stopwatch clock=Stopwatch.StartNew();
    double last;
    Rectangle windowBounds;
    bool exiting;
    public MainWindow()
    {
        profile=Profile.Load(root);game=new(profile,root);
        Text="New Super Mario Bros. PC";ClientSize=new(1280,800);MinimumSize=new(800,540);
        StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;KeyPreview=true;AllowDrop=true;
        UpdateHome();timer.Tick+=(_,_)=>TickGame();timer.Start();last=clock.Elapsed.TotalSeconds;
        DragEnter+=(_,e)=>{if(e.Data?.GetDataPresent(DataFormats.FileDrop)==true)e.Effect=DragDropEffects.Copy;};
        DragDrop+=(_,e)=>{if(ui.Panel==Panel.Home && e.Data?.GetData(DataFormats.FileDrop) is string[] f && f.Length==1)Import(f[0]);};
        Deactivate+=(_,_)=>{if(game.Core!=null)SetPanel(Panel.Pause);physical.Clear();};
        Shown+=(_,_)=>{if(profile.Fullscreen)ApplyFullscreen();};
        FormClosing+=(_,e)=>
        {
            if(exiting)return;
            try{game.SaveCheckpoint();profile.Save(root);}
            catch(Exception ex){e.Cancel=true;ui.Notice="Não foi possível salvar: "+ex.Message;SetPanel(Panel.Pause);return;}
            exiting=true;timer.Stop();game.Dispose();
        };
    }
    void UpdateHome(){ui.HasRom=File.Exists(Path.Combine(root,"game.nds"));ui.HasCheckpoint=File.Exists(Path.Combine(root,"resume.state"));}
    void SetPanel(Panel panel)
    {ui.Panel=panel;ui.Selected=0;ui.Capture=null;game.Pause(panel!=Panel.Play);last=clock.Elapsed.TotalSeconds;Invalidate();}
    void TickGame()
    {
        double now=clock.Elapsed.TotalSeconds;
        if(ui.Panel==Panel.Play && game.Core!=null)
        {
            int steps=0;const double frame=560190.0/(32*1024*1024);
            while(now-last>=frame && steps<4){game.Tick();last+=frame;steps++;}
            if(now-last>frame*4)last=now;
        }
        else last=now;
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e){ui.Draw(e.Graphics,ClientSize,profile,game);}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        var k=Profile.Normalize(keyData);
        if(k is Keys.Tab or Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.F11)
        {Key(k);return true;}return base.ProcessCmdKey(ref msg,keyData);
    }
    protected override void OnKeyDown(KeyEventArgs e){Key(e.KeyCode);e.Handled=true;}
    protected override void OnKeyUp(KeyEventArgs e){var k=Profile.Normalize(e.KeyCode);physical.Remove(k);game.Input.Up(k);e.Handled=true;}
    void Key(Keys k)
    {
        k=Profile.Normalize(k);if(!physical.Add(k))return;
        if(ui.Capture is ActionKey a)
        {
            if(k==Keys.Escape){ui.Capture=null;return;}
            if(profile.Bind(a,k)){profile.Save(root);ui.Capture=null;ui.Notice="Controle salvo. A troca vale imediatamente.";}
            else ui.Notice="Essa tecla é reservada pelo Windows. Escolha outra.";
            return;
        }
        if(k==profile.Bindings[ActionKey.Fullscreen]){profile.Fullscreen=!profile.Fullscreen;ApplyFullscreen();profile.Save(root);return;}
        if(ui.Panel==Panel.Play)
        {
            if(k==profile.Bindings[ActionKey.Pause]){SetPanel(Panel.Pause);return;}
            if(k==profile.Bindings[ActionKey.Alternate]){game.Swap();return;}
            if(k==profile.Bindings[ActionKey.Reserve]){game.Reserve();return;}
            game.Input.Down(k);return;
        }
        if(k==Keys.Escape || k==profile.Bindings[ActionKey.Pause]){Back();return;}
        if(k==Keys.Down || k==profile.Bindings[ActionKey.Down]){ui.Selected=(ui.Selected+1)%Math.Max(1,ui.Hits.Count);return;}
        if(k==Keys.Up || k==profile.Bindings[ActionKey.Up]){ui.Selected=(ui.Selected+ui.Hits.Count-1)%Math.Max(1,ui.Hits.Count);return;}
        if(k==Keys.Enter || k==Keys.Space || k==profile.Bindings[ActionKey.Confirm])
        {if(ui.Hits.Count>0)Activate(ui.Hits[Math.Clamp(ui.Selected,0,ui.Hits.Count-1)].Id);}
    }
    void Back(){if(ui.Panel is Panel.Controls or Panel.Settings)SetPanel(game.Core==null?Panel.Home:Panel.Pause);else if(game.Core!=null)SetPanel(Panel.Play);}
    protected override void OnMouseMove(MouseEventArgs e)
    {
        PointF p=Presentation.ToCanvas(e.Location,ClientSize);
        if(ui.Panel!=Panel.Play){int i=ui.Hits.FindIndex(h=>h.Rect.Contains(p));if(i>=0)ui.Selected=i;}
        if(game.PointerHeld)Touch(p);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if(e.Button!=MouseButtons.Left)return;
        PointF p=Presentation.ToCanvas(e.Location,ClientSize);
        var hit=ui.Hits.FirstOrDefault(h=>h.Rect.Contains(p));
        if(hit!=null){Activate(hit.Id);return;}
        if(ui.Panel==Panel.Play && game.Bottom && Presentation.Screen.Contains(p)){game.PointerHeld=true;Capture=true;Touch(p);}
    }
    protected override void OnMouseUp(MouseEventArgs e){if(e.Button==MouseButtons.Left){game.PointerHeld=false;game.Input.Touch=null;Capture=false;}}
    void Touch(PointF p)
    {
        if(!Presentation.Screen.Contains(p)){game.Input.Touch=null;return;}
        game.Input.Touch=new Point((int)((p.X-Presentation.Screen.X)*256/Presentation.Screen.Width),(int)((p.Y-Presentation.Screen.Y)*192/Presentation.Screen.Height));
    }
    void Import(string path)
    {
        try{RomImport.Import(path,root);UpdateHome();ui.Notice="ROM importada. Agora é só jogar.";}
        catch(Exception ex){ui.Notice=ex.Message;}
    }
    void Activate(string id)
    {
        try
        {
            ui.Notice="";
            if(id.StartsWith("bind:")){ui.Capture=Enum.Parse<ActionKey>(id[5..]);return;}
            switch(id)
            {
                case "start":
                    if(!ui.HasRom){using var f=new OpenFileDialog{Filter="ROM de NSMB|*.nds;*.zip",Title="Importar New Super Mario Bros. A2DE revisão 0"};if(f.ShowDialog(this)==DialogResult.OK)Import(f.FileName);break;}
                    Start(true);break;
                case "boot":Start(false);break;
                case "resume":SetPanel(Panel.Play);break;
                case "controls":SetPanel(Panel.Controls);break;
                case "settings":SetPanel(Panel.Settings);break;
                case "back":Back();break;
                case "defaults":profile.Bindings=Profile.Defaults();profile.Save(root);ui.Notice="WASD e Espaço restaurados.";break;
                case "volume":profile.Volume=(profile.Volume+25)%125;profile.Save(root);break;
                case "smooth":profile.Smooth=!profile.Smooth;profile.Save(root);break;
                case "screen":profile.ScreenMode=profile.ScreenMode switch {"Auto"=>"Top","Top"=>"Bottom",_=>"Auto"};profile.Save(root);break;
                case "fullscreen":profile.Fullscreen=!profile.Fullscreen;ApplyFullscreen();profile.Save(root);break;
                case "reserve":game.Reserve();break;
                case "home":game.SaveCheckpoint();game.Dispose();UpdateHome();SetPanel(Panel.Home);break;
                case "data":Directory.CreateDirectory(root);Process.Start(new ProcessStartInfo(root){UseShellExecute=true});break;
                case "exit":Close();break;
            }
        }
        catch(Exception ex){ui.Notice=ex.Message;}
        Invalidate();
    }
    void Start(bool resume){game.Start(resume);SetPanel(Panel.Play);ui.Notice=game.Core?.Warning??"";}
    void ApplyFullscreen()
    {
        if(profile.Fullscreen){windowBounds=Bounds;FormBorderStyle=FormBorderStyle.None;WindowState=FormWindowState.Normal;Bounds=System.Windows.Forms.Screen.FromControl(this).Bounds;}
        else{FormBorderStyle=FormBorderStyle.Sizable;WindowState=FormWindowState.Normal;if(windowBounds.Width>0)Bounds=windowBounds;}
        last=clock.Elapsed.TotalSeconds;
    }
}
