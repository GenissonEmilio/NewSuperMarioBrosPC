using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace MarioPC;

public enum Panel { Home, Pause, Controls, Settings, Play, Map }
public sealed record Hit(RectangleF Rect,string Id);
public sealed record GameView(Bitmap Frame,int Scene,bool Bottom,Bitmap? Worldmap=null);

public sealed class Presentation
{
    public static readonly RectangleF Screen=new(256,60,768,576);
    public static readonly RectangleF ReserveRect=new(1060,440,176,100);
    public List<Hit> Hits {get;}=[];
    public int Selected {get;set;}
    public ActionKey? Capture {get;set;}
    public string Notice {get;set;}="";
    public Panel Panel {get;set;}=Panel.Home;
    public bool HasRom {get;set;}
    public bool HasCheckpoint {get;set;}
    static readonly Color Ink=Color.FromArgb(12,20,36),Card=Color.FromArgb(24,38,60),Muted=Color.FromArgb(166,185,208),Accent=Color.FromArgb(255,95,73),Gold=Color.FromArgb(255,210,101);
    static readonly string[] Labels=["Subir / entrar em porta","Agachar / descer cano","Esquerda","Direita","Pular / nadar","Correr / disparar / segurar","Usar item reserva","Confirmar no jogo","Voltar no jogo","Pause original (Start)","Mapa / progresso","Menu de pausa PC","Tela cheia"];
    void Text(Graphics g,string text,float x,float y,float size,Color color,FontStyle style=FontStyle.Regular,float width=1200)
    {using var f=new Font("Segoe UI",size,style,GraphicsUnit.Pixel);using var b=new SolidBrush(color);g.DrawString(text,f,b,new RectangleF(x,y,width,190));}
    void Box(Graphics g,RectangleF r,Color c){using var b=new SolidBrush(c);g.FillRectangle(b,r);}
    void Button(Graphics g,RectangleF r,string title,string id,string? subtitle=null)
    {
        int i=Hits.Count;Hits.Add(new(r,id));bool selected=i==Selected;
        Box(g,r,selected?Color.FromArgb(48,69,96):Card);
        if(selected)Box(g,new(r.X,r.Y,4,r.Height),Accent);
        Text(g,title,r.X+20,r.Y+12,id.StartsWith("bind:")?18:21,Color.White,FontStyle.Bold,r.Width-(id.StartsWith("bind:")?170:35));
        if(subtitle!=null)Text(g,subtitle,r.X+20,r.Y+42,14,Muted,width:r.Width-35);
    }
    public static PointF ToCanvas(Point p,Size size)
    {float scale=Math.Min(size.Width/1280f,size.Height/800f);return new((p.X-(size.Width-1280*scale)/2)/scale,(p.Y-(size.Height-800*scale)/2)/scale);}
    public void Draw(Graphics g,Size size,Profile profile,GameSession? game,GameView? fixture=null)
    {
        Hits.Clear();g.Clear(Ink);var saved=g.Save();
        float scale=Math.Min(size.Width/1280f,size.Height/800f);
        g.TranslateTransform((size.Width-1280*scale)/2,(size.Height-800*scale)/2);g.ScaleTransform(scale,scale);
        g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
        var view=fixture ?? (game?.Core is RetroCore c ? new GameView(c.Frame,c.Scene,game.Bottom,game.WorldmapFrame) : null);
        if(view!=null)DrawGame(g,profile,view);
        if(Panel!=Panel.Play){Hits.Clear();DrawPanel(g,profile,game,view);}
        if(Notice.Length>0){Box(g,new(24,750,1232,38),Color.FromArgb(225,34,49,71));Text(g,Notice,38,756,15,Gold,width:1200);}
        g.Restore(saved);
    }
    void Image(Graphics g,Bitmap image,RectangleF dest,RectangleF source,bool smooth)
    {g.InterpolationMode=smooth?InterpolationMode.HighQualityBilinear:InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(image,dest,source,GraphicsUnit.Pixel);g.PixelOffsetMode=PixelOffsetMode.Default;}
    void DrawGame(Graphics g,Profile profile,GameView view)
    {
        var core=view;
        Box(g,Screen,Color.Black);
        Image(g,core.Frame,Screen,new(0,view.Bottom?192:0,256,192),profile.Smooth);
        Text(g,"MARIO / PC",28,22,20,Color.White,FontStyle.Bold);
        Text(g,view.Bottom?"MENU DO JOGO":core.Scene==9?"MAPA-MÚNDI":"AVENTURA",1060,76,14,Gold,FontStyle.Bold,180);
        Text(g,"UMA TELA",28,88,13,Muted,FontStyle.Bold);
        Text(g,$"{profile.Label(ActionKey.Up)} {profile.Label(ActionKey.Left)} {profile.Label(ActionKey.Down)} {profile.Label(ActionKey.Right)}",28,122,24,Color.White,FontStyle.Bold,210);
        Text(g,$"{profile.Label(ActionKey.Jump)}  •  pular",28,174,16,Muted,width:210);
        Text(g,$"{profile.Label(ActionKey.Run)}  •  correr",28,206,16,Muted,width:210);
        Text(g,$"{profile.Label(ActionKey.Up)} / {profile.Label(ActionKey.Down)}  •  portas / canos",28,238,14,Muted,width:216);
        if(core.Scene is 3 or 9)Button(g,new(1060,120,176,56),"MAPA  ["+profile.Label(ActionKey.Map)+"]","map");
        Text(g,profile.Label(ActionKey.Pause)+"  •  menu PC",1060,198,16,Muted,width:190);
        Text(g,profile.Label(ActionKey.GamePause)+"  •  pause do jogo",1060,234,16,Muted,width:190);
        Text(g,"Proporção original 4:3",1060,272,14,Muted,width:185);
        Text(g,"Mapa, item, vidas e moedas reunidos nesta janela.",1060,308,15,Muted,width:180);
        if(core.Scene==3 && !view.Bottom)
        {
            Button(g,ReserveRect,"ITEM  ["+profile.Label(ActionKey.Reserve)+"]","reserve");
            // Actual reserve graphic, star coins/score, and course progress are sampled from the live ROM output.
            Image(g,core.Frame,new(1116,494,64,64),new(184,192+120,64,64),false);
            Image(g,core.Frame,new(28,330,204,56),new(0,192,102,28),false);
            Image(g,core.Frame,new(28,404,192,56),new(160,192,96,28),false);
            Box(g,new(256,650,768,124),Card);
            Image(g,core.Frame,new(268,658,252,108),new(0,192+104,168,80),false);
            Image(g,core.Frame,new(552,658,448,112),new(0,192+32,256,64),false);
        }
        else if(core.Scene==9 && !view.Bottom)
        {
            Box(g,new(256,656,768,100),Card);
            Image(g,core.Frame,new(272,664,400,80),new(0,192+32,256,64),false);
            Image(g,core.Frame,new(704,664,304,80),new(0,192+112,256,80),false);
            Text(g,"ROTA ATUAL",256,762,12,Muted,FontStyle.Bold);
            Text(g,"MUNDOS  •  "+profile.Label(ActionKey.Map)+" PARA SELECIONAR",704,762,12,Muted,FontStyle.Bold,330);
        }
        else
        {
            Box(g,new(256,656,768,80),Card);
            Text(g,"CONTROLE A CAMPANHA PELO TECLADO OU CLIQUE",278,672,18,Color.White,FontStyle.Bold,720);
            Text(g,$"{profile.Label(ActionKey.Confirm)} confirmar  •  {profile.Label(ActionKey.Back)} voltar",278,704,15,Muted,width:720);
        }
    }
    void DrawPanel(Graphics g,Profile profile,GameSession? game,GameView? view)
    {
        Box(g,new(0,0,1280,800),Color.FromArgb(game?.Core==null?255:235,12,20,36));
        Text(g,"GENISSONEMILIO  /  PC EDITION",64,40,14,Muted,FontStyle.Bold);
        Text(g,Panel switch {Panel.Controls=>"Seus controles",Panel.Settings=>"Sua experiência",Panel.Pause=>"Uma pausa na aventura",Panel.Map=>"Mapa e progresso",_=>"New Super\nMario Bros."},64,90,Panel==Panel.Home?58:42,Color.White,FontStyle.Bold,1100);
        if(Panel==Panel.Home)
        {
            // Original geometric decoration; no Nintendo artwork is distributed.
            Box(g,new(64,242,84,6),Accent);
            Text(g,"Uma tela. Seu teclado. A aventura original.",64,274,22,Muted,width:520);
            Text(g,$"{profile.Label(ActionKey.Up)}{profile.Label(ActionKey.Left)}{profile.Label(ActionKey.Down)}{profile.Label(ActionKey.Right)} para mover\n{profile.Label(ActionKey.Jump)} para pular\n{profile.Label(ActionKey.Run)} para correr\n{profile.Label(ActionKey.Reserve)} para o item • {profile.Label(ActionKey.Map)} para o mapa",64,340,22,Color.White,width:540);
            Text(g,"Adaptação independente com melonDS.\nImporte sua própria ROM A2DE revisão 0.\nNenhum jogo acompanha o aplicativo.",64,542,16,Muted,width:510);
            Button(g,new(676,196,532,80),HasRom?(HasCheckpoint?"Continuar aventura":"Jogar"):"Importar sua ROM","start",HasRom?"Campanha com seus controles de PC":"Arraste .nds / .zip ou clique para escolher");
            if(HasRom)Button(g,new(676,292,532,72),"Iniciar pelo menu original","boot","Usa o save original, sem carregar o snapshot");
            Button(g,new(676,384,532,72),"Controles","controls","Altere qualquer tecla; conflitos são trocados");
            Button(g,new(676,472,532,72),"Configurações","settings","Vídeo, áudio e tela cheia");
            Button(g,new(676,560,532,62),"Abrir pasta de dados","data");
            Button(g,new(676,638,532,62),"Sair","exit");
        }
        else if(Panel==Panel.Pause)
        {
            Text(g,"O jogo está congelado enquanto este menu está aberto.",64,154,19,Muted);
            Button(g,new(256,214,768,64),"Continuar","resume");
            Button(g,new(256,294,768,64),"Mapa e progresso","map");
            Button(g,new(256,374,768,64),"Controles","controls");
            Button(g,new(256,454,768,64),"Configurações","settings");
            Button(g,new(256,534,768,64),"Salvar snapshot e voltar ao início","home");
            Button(g,new(256,614,768,64),"Salvar snapshot e sair","exit");
        }
        else if(Panel==Panel.Controls)
        {
            Text(g,Capture==null?"Clique em uma ação e pressione a nova tecla. Conflitos trocam as duas atribuições.":"Pressione a nova tecla. Esc cancela a alteração.",64,154,18,Capture==null?Muted:Gold);
            var actions=Enum.GetValues<ActionKey>();
            for(int i=0;i<actions.Length;i++)
            {
                int col=i/7,row=i%7;var a=actions[i];
                var r=new RectangleF(64+col*584,210+row*62,568,50);
                Button(g,r,Labels[i],"bind:"+a);
                Box(g,new(r.Right-134,r.Y+8,120,34),Capture==a?Accent:Ink);
                Text(g,Capture==a?"...":profile.Label(a),r.Right-124,r.Y+13,16,Color.White,FontStyle.Bold,112);
            }
            Button(g,new(64,668,420,56),"Restaurar WASD / Espaço","defaults");
            Button(g,new(900,668,316,56),"Voltar","back");
        }
        else if(Panel==Panel.Settings)
        {
            Button(g,new(256,222,768,64),"Volume: "+profile.Volume+"%","volume","Clique ou Enter para alternar em passos de 25%");
            Button(g,new(256,306,768,64),"Filtro: "+(profile.Smooth?"suave":"pixels nítidos"),"smooth");
            Button(g,new(256,390,768,64),"Tela cheia: "+(profile.Fullscreen?"sim":"não"),"fullscreen");
            Button(g,new(256,586,768,64),"Voltar","back");
        }
        else if(Panel==Panel.Map && view!=null)
        {
            Text(g,"O jogo está pausado. "+profile.Label(ActionKey.Map)+" ou Esc fecha este painel.",64,154,18,Muted);
            if(view.Scene==9)
            {
                Text(g,"ROTA DO MUNDO ATUAL",64,204,14,Gold,FontStyle.Bold);
                Image(g,view.Frame,new(64,240,768,192),new(0,192+32,256,64),false);
                Image(g,view.Frame,new(904,240,256,80),new(0,192+112,256,80),false);
                Text(g,"Escolha um mundo liberado. Os bloqueios do save original continuam valendo.",904,348,17,Muted,width:290);
                for(int i=0;i<8;i++)Button(g,new(64+(i%4)*288,480+(i/4)*80,272,62),"Mundo "+(i+1),"world:"+i);
            }
            else
            {
                Text(g,"MAPA VISITADO",64,204,14,Gold,FontStyle.Bold);
                if(view.Worldmap!=null)Image(g,view.Worldmap,new(64,240,512,384),new(0,0,256,192),false);
                else{Box(g,new(64,240,512,384),Card);Text(g,"Volte ao mapa-múndi para registrar uma prévia desta aventura.",88,388,20,Muted,width:460);}
                Text(g,"PROGRESSO NA FASE",656,204,14,Gold,FontStyle.Bold);
                if(view.Scene==3)
                {
                    Image(g,view.Frame,new(656,240,544,136),new(0,192+32,256,64),false);
                    Text(g,"PONTUAÇÃO E MOEDAS ESTRELA",656,410,14,Gold,FontStyle.Bold);
                    Image(g,view.Frame,new(656,448,336,160),new(0,192+104,168,80),false);
                }
                Text(g,"A prévia mostra o último mapa visitado. A barra registra sua posição na fase atual.",656,630,16,Muted,width:540);
            }
            Button(g,new(900,690,316,56),"Voltar ao jogo","back");
        }
        Text(g,"v0.1.0-alpha.1   •   Windows x64   •   campanha em modo de emulação",64,764,13,Muted);
    }
}
