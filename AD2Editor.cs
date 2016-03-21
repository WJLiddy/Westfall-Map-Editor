using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;

public class AD2Editor : AD2Game
{
    public class AD2Object
    {
        public string name;
        public Texture2D t;
        public Texture2D collide;
        public int X, Y;
    }

    //top left corner
    int camX = 0;
    int camY = 0;
    int mouseX = 0;
    int mouseY = 0;

    public LinkedList<AD2Object>[] objectsList;

    public Texture2D baseMap;
    public Texture2D collideMap;
    public Texture2D mouse;

    public static string[] TextureName;
    public static Texture2D[] TextureList;
    public static Texture2D[] CollideTextureList;

    // Game Dims.
    public static readonly int BaseWidth = 360;
    public static readonly int BaseHeight = 270;


    public int  SCROLL_SPEED = 5;
    public double TRANSITION_AREA = .1;

    public bool putMode = true;
    public int putPointer = 0;

    public bool newKeyLeft;
    public bool newKeyRight;
    public bool newMouseLeft;
    public bool newMouseRight;
    public bool genericNewKey;

    public AD2Editor() : base(BaseWidth, BaseHeight, 40)
    {
        Renderer.Resolution = Renderer.ResolutionType.WindowedLarge;
    }


    protected override void AD2Logic(int ms, KeyboardState keyboardState, SlimDX.DirectInput.JoystickState[] gamePadState)
    {
        mouseX = Mouse.GetState().X;
        mouseY = Mouse.GetState().Y;

        if(keyboardState.IsKeyDown(Keys.Left) && newKeyLeft)
        {
            if (putPointer == 0)
                putPointer = TextureList.Length - 1;
            else
                putPointer = putPointer - 1;
        }

        if (keyboardState.IsKeyDown(Keys.Right) && newKeyRight)
        {
                putPointer = (putPointer + 1) % TextureList.Length;
        }

        if( Mouse.GetState().LeftButton == ButtonState.Pressed && !newMouseLeft)
        {
            if (putMode)
            {
                AD2Object a = new AD2Object();
                a.X = camX + mouseX;
                a.Y = camY + mouseY;
                a.t = TextureList[putPointer];
                a.collide = CollideTextureList[putPointer];
                a.name = TextureName[putPointer];
                //Where is the bottom?
                if(collide(0,0,baseMap.Width,baseMap.Height,a.X,a.Y))
                    objectsList[a.Y + (a.t.Height - 1)].AddFirst(a);

            } else
            {
                for(int i = 0; i != objectsList.Length; i++)
                {
                    LinkedList<AD2Object> removeList = new LinkedList<AD2Object>();

                    foreach(AD2Object a in objectsList[i])
                    {
                        if (collide(a.X, a.Y, a.t.Width, a.t.Height, camX + mouseX, camY + mouseY))
                        {
                            removeList.AddFirst(a);
                        }
                    }

                    foreach(AD2Object a in removeList)
                    {
                        objectsList[i].Remove(a);
                    }
                }
            }
        }


        if (Mouse.GetState().RightButton == ButtonState.Pressed && !newMouseRight)
        {
            putMode = !putMode;
        }

        if (keyboardState.IsKeyDown(Keys.Enter) && genericNewKey)
        {
            generateObjectsXML();
            saveRenderedCollideMap();
        }

        newKeyLeft = !keyboardState.IsKeyDown(Keys.Left);
        newKeyRight = !keyboardState.IsKeyDown(Keys.Right);
        newMouseLeft = Mouse.GetState().LeftButton == ButtonState.Pressed;
        newMouseRight = Mouse.GetState().RightButton == ButtonState.Pressed;
        genericNewKey = !keyboardState.IsKeyDown(Keys.Enter);


        if (mouseX < BaseWidth * TRANSITION_AREA)
            camX -= SCROLL_SPEED;
        if (mouseX > BaseWidth * (1.0-TRANSITION_AREA))
            camX += SCROLL_SPEED;

        if (mouseY < BaseHeight * TRANSITION_AREA)
            camY -= SCROLL_SPEED;
        if (mouseY > BaseHeight * (1.0 - TRANSITION_AREA))
            camY += SCROLL_SPEED;

    }

    protected override void AD2Draw(AD2SpriteBatch primarySpriteBatch)
    {
        primarySpriteBatch.DrawTexture(baseMap, -camX, -camY);
        Utils.Log(mouseX + "");
        for(int y = 0; y != objectsList.Length;y++)
        {
            foreach(AD2Object a in objectsList[y])
            {
                primarySpriteBatch.DrawTexture(a.t, a.X + -camX, y +- (a.t.Height - 1) + -camY);
            }

            if(putMode && camY + mouseY + (TextureList[putPointer].Height - 1) == y )
            {
                primarySpriteBatch.DrawTexture(TextureList[putPointer], mouseX, mouseY);
            }
        }

        if (!putMode)
        {
            primarySpriteBatch.DrawTexture(mouse, mouseX, mouseY);
        }

    }

    protected override void AD2LoadContent()
    {
        baseMap = Utils.TextureLoader("base.png");
        collideMap = Utils.TextureLoader("base_c.png");
        objectsList = new LinkedList<AD2Object>[baseMap.Height];
        for(int i = 0; i != baseMap.Height; i++)
        {
            objectsList[i] = new LinkedList<AD2Object>();
        }
        mouse = Utils.TextureLoader("mouse.png");

        string[] files = Directory.GetFiles(Utils.PathToAssets + "objects\\");

        TextureName = new string[files.Length];
        TextureList = new Texture2D[files.Length];
        CollideTextureList = new Texture2D[files.Length];

        for (int i = 0; i != files.Length; i++)
        {
  
            TextureName[i] = Path.GetFileName(files[i]);
            TextureList[i] = Utils.TextureLoader("objects\\" + Path.GetFileName(files[i]));
            CollideTextureList[i] = Utils.TextureLoader("objects\\collide\\" + Path.GetFileName(files[i]));
        }
    }

    public bool collide(int x1, int y1, int width, int height, int pointX, int pointY)
    {
        return (pointX >= x1 && pointY >= y1 && pointX <= x1 + width && pointY <= y1 + height);
    }

    public void generateObjectsXML()
    {
        //create a new file 
        string path = "TestMapObjects.xml";
        File.Create(path).Close() ;
        StreamWriter tw = new StreamWriter(path);

        tw.WriteLine("<Obj>");
        for (int i = 0; i != objectsList.Length; i++)
        {
            foreach (AD2Object a in objectsList[i])
            {
                tw.WriteLine("    <object>" + a.name + "," + a.X + "," + a.Y + "</object>");
            }
        }
        tw.WriteLine("</Obj>");
        tw.Close();
    }

    public void saveRenderedCollideMap()
    {
        //create a new file 
        RenderTarget2D map = new RenderTarget2D(Renderer.GraphicsDevice, baseMap.Width, baseMap.Height);
        //GraphicsDevice oldDevice =
        Renderer.GraphicsDevice.SetRenderTarget(map);

        SpriteBatch b = new SpriteBatch(Renderer.GraphicsDevice);

        b.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.Default,
            RasterizerState.CullNone);

        b.Draw(collideMap, new Rectangle(0, 0, map.Width, map.Height), Color.White);
        for (int i = 0; i != objectsList.Length; i++)
        {
            foreach (AD2Object a in objectsList[i])
            {
                b.Draw(a.collide, new Rectangle(a.X, a.Y, a.t.Width, a.t.Height), Color.White);
            }
        }

        b.End();

        Renderer.GraphicsDevice.SetRenderTarget(null);

        Stream stream = File.Create("collide.png");

        //Save as PNG
        map.SaveAsPng(stream, map.Width, map.Height);
        stream.Dispose();



    }
}

