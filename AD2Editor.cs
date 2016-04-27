using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

public class AD2Editor : AD2Game
{
    // Represents an object on the map.
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
    public Texture2D allowCollide;
    public Texture2D disableCollide;

    public bool objectsCanCollide = true;

    public static string[] TextureName;
    public static Texture2D[] TextureList;
    public static Texture2D[] CollideTextureList;

    // Game Dims.
    public static readonly int BaseWidth = 360;
    public static readonly int BaseHeight = 270;


    public int SCROLL_SPEED = 5;
    public double TRANSITION_AREA = .05;

    public bool putMode = true;
    public int putPointer = 0;
    
    public bool newMouseLeft;
    public bool newMouseRight;
    public bool genericNewKey;

    public enum Viewmodes { Object, FiftyPercent, Collide }
    public Viewmodes Viewmode = Viewmodes.Object;

    public string objectDirectory;

    public AD2Editor() : base(BaseWidth, BaseHeight, 40)
    {
        Renderer.Resolution = Renderer.ResolutionType.WindowedLarge;
    }


    protected override void AD2Logic(int ms, KeyboardState keyboardState, SlimDX.DirectInput.JoystickState[] gamePadState)
    {
        mouseX = Mouse.GetState().X;
        mouseY = Mouse.GetState().Y;

        
        //Minus = object left
        if (keyboardState.IsKeyDown(Keys.OemMinus) && genericNewKey)
        {
            if (putPointer == 0)
                putPointer = TextureList.Length - 1;
            else
                putPointer = putPointer - 1;
        }

        //Plus = object forward
        if (keyboardState.IsKeyDown(Keys.OemPlus) && genericNewKey)
        {
            putPointer = (putPointer + 1) % TextureList.Length;
        }

        //Click = place or erase object.
        if (Mouse.GetState().LeftButton == ButtonState.Pressed && !newMouseLeft)
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
                if (collide(0, 0, baseMap.Width, baseMap.Height, a.X, a.Y))
                    objectsList[a.Y + (a.t.Height - 1)].AddFirst(a);

            }
            else
            {
                for (int i = 0; i != objectsList.Length; i++)
                {
                    LinkedList<AD2Object> removeList = new LinkedList<AD2Object>();

                    foreach (AD2Object a in objectsList[i])
                    {
                        if (collide(a.X, a.Y, a.t.Width, a.t.Height, camX + mouseX, camY + mouseY))
                        {
                            removeList.AddFirst(a);
                        }
                    }

                    foreach (AD2Object a in removeList)
                    {
                        objectsList[i].Remove(a);
                    }
                }
            }
        }


        // mode switch
        if (Mouse.GetState().RightButton == ButtonState.Pressed && !newMouseRight)
        {
            putMode = !putMode;
        }

        if (keyboardState.IsKeyDown(Keys.F1) && genericNewKey)
        {
            generateNew();
        }

        if (keyboardState.IsKeyDown(Keys.F2) && genericNewKey)
        {
            LevelIO.Save(this);
        }

        if (keyboardState.IsKeyDown(Keys.F3) && genericNewKey)
        {
            loadNew();
        }


        if (keyboardState.IsKeyDown(Keys.C) && genericNewKey)
        {
            objectsCanCollide = !objectsCanCollide;
        }

        if (keyboardState.IsKeyDown(Keys.V) && genericNewKey)
        {
            switch (Viewmode)
            {
                case Viewmodes.Collide:
                    Viewmode = Viewmodes.Object;
                    break;
                case Viewmodes.FiftyPercent:
                    Viewmode = Viewmodes.Collide;
                    break;
                case Viewmodes.Object:
                    Viewmode = Viewmodes.FiftyPercent;
                    break;
            }
        }
        
        newMouseLeft = Mouse.GetState().LeftButton == ButtonState.Pressed;
        newMouseRight = Mouse.GetState().RightButton == ButtonState.Pressed;
        genericNewKey = keyboardState.GetPressedKeys().Length == 0;


        if (mouseX < BaseWidth * TRANSITION_AREA)
            camX -= SCROLL_SPEED;
        if (mouseX > BaseWidth * (1.0 - TRANSITION_AREA))
            camX += SCROLL_SPEED;

        if (mouseY < BaseHeight * TRANSITION_AREA)
            camY -= SCROLL_SPEED;
        if (mouseY > BaseHeight * (1.0 - TRANSITION_AREA))
            camY += SCROLL_SPEED;

    }

    protected override void AD2Draw(AD2SpriteBatch primarySpriteBatch)
    {
        primarySpriteBatch.DrawTexture(baseMap, -camX, -camY);

        for (int y = 0; y != objectsList.Length; y++)
        {
            foreach (AD2Object a in objectsList[y])
            {
                if (Viewmode.Equals(Viewmodes.Object))
                    primarySpriteBatch.DrawTexture(a.t, a.X + -camX, y + -(a.t.Height - 1) + -camY);
                if (Viewmode.Equals(Viewmodes.FiftyPercent))
                {
                    primarySpriteBatch.DrawTexture(a.collide, a.X + -camX, y + -(a.t.Height - 1) + -camY, new Color(1f, 1f, 1f, 1f));
                    primarySpriteBatch.DrawTexture(a.t, a.X + -camX, y + -(a.t.Height - 1) + -camY, new Color(1f, 1f, 1f, 0.7f));
                }
                if (Viewmode.Equals(Viewmodes.Collide))
                    primarySpriteBatch.DrawTexture(a.collide, a.X + -camX, y + -(a.t.Height - 1) + -camY);


            }

            if (putMode && camY + mouseY + (TextureList[putPointer].Height - 1) == y)
            {
                if (Viewmode.Equals(Viewmodes.Object))
                    primarySpriteBatch.DrawTexture(TextureList[putPointer], mouseX, mouseY);
                else
                {
                    primarySpriteBatch.DrawTexture(CollideTextureList[putPointer], mouseX, mouseY);
                    primarySpriteBatch.DrawTexture(TextureList[putPointer], mouseX, mouseY, new Color(1f, 1f, 1f, 0.7f));
                }

            }
        }

        if (!putMode)
        {
            primarySpriteBatch.DrawTexture(mouse, mouseX, mouseY);
        }

        //GUI on top
        Utils.DrawRect(primarySpriteBatch, 0, 0, BaseWidth, 10, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        Utils.DefaultFont.Draw(primarySpriteBatch, "F1/NEW  F2/SAVE  F3/LOAD  F4/HELP  V/VIEW  C/COLLIDE  R/RULER", 1, 1, Color.White, 1, true, Color.Black);

        if (objectsCanCollide)
            primarySpriteBatch.DrawTexture(allowCollide, BaseWidth - 10, 0);
        else
            primarySpriteBatch.DrawTexture(disableCollide, BaseWidth - 10, 0);
    }

    protected override void AD2LoadContent()
    {
        baseMap = Utils.TextureLoader("base.png");
        collideMap = Utils.TextureLoader("base_c.png");
        allowCollide = Utils.TextureLoader("allowCollide.png");
        disableCollide = Utils.TextureLoader("noCollide.png");
        objectsList = new LinkedList<AD2Object>[baseMap.Height];
        for (int i = 0; i != baseMap.Height; i++)
        {
            objectsList[i] = new LinkedList<AD2Object>();
        }
        mouse = Utils.TextureLoader("mouse.png");
        //Default: go to utils.
        objectDirectory = Utils.PathToAssets + "objects\\";
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

        return (pointX >= x1 && pointY >= y1 && pointX <= x1 + (width - 1) && pointY <= y1 + (height - 1));
    }

    public void generateNew()
    {
        // Create an instance of the open file dialog box.

        System.Windows.Forms.OpenFileDialog baseMapD = new System.Windows.Forms.OpenFileDialog();
        baseMapD.Title = "Select the Base Map";
        baseMapD.ShowDialog();

        System.Windows.Forms.OpenFileDialog collideMapD = new System.Windows.Forms.OpenFileDialog();
        collideMapD.Title = "Select the Collide Map";
        collideMapD.ShowDialog();

        System.Windows.Forms.FolderBrowserDialog folderBrowserD = new System.Windows.Forms.FolderBrowserDialog();
        folderBrowserD.Description = "Select the folder containing the objects";
        folderBrowserD.ShowDialog();

        // Process input if the user clicked OK.
        baseMap = Utils.TextureLoader(baseMapD.FileName,false);
        collideMap = Utils.TextureLoader(collideMapD.FileName,false);
        objectsList = new LinkedList<AD2Object>[baseMap.Height];
        for (int i = 0; i != baseMap.Height; i++)
        {
            objectsList[i] = new LinkedList<AD2Object>();
        }

        //Default: go to utils.
        objectDirectory = folderBrowserD.SelectedPath;
        string[] files = Directory.GetFiles(objectDirectory);

        TextureName = new string[files.Length];
        TextureList = new Texture2D[files.Length];
        CollideTextureList = new Texture2D[files.Length];

        for (int i = 0; i != files.Length; i++)
        {
            TextureName[i] = Path.GetFileName(files[i]);
            TextureList[i] = Utils.TextureLoader(objectDirectory + "\\" + Path.GetFileName(files[i]),false);
            CollideTextureList[i] = Utils.TextureLoader(objectDirectory + "\\collide\\" + Path.GetFileName(files[i]),false);
        }
    }

    public void loadNew()
    {
        // Load a new map, but this time, for the objects, read the object XML and put it in.
        System.Windows.Forms.OpenFileDialog baseMapD = new System.Windows.Forms.OpenFileDialog();
        baseMapD.Title = "Select the Base Map";
        baseMapD.ShowDialog();

        System.Windows.Forms.OpenFileDialog collideMapD = new System.Windows.Forms.OpenFileDialog();
        collideMapD.Title = "Select the Collide Map";
        collideMapD.ShowDialog();

        System.Windows.Forms.FolderBrowserDialog folderBrowserD = new System.Windows.Forms.FolderBrowserDialog();
        folderBrowserD.Description = "Select the folder containing the objects WITH collision";
        folderBrowserD.ShowDialog();

        System.Windows.Forms.OpenFileDialog objectxmlD = new System.Windows.Forms.OpenFileDialog();
        objectxmlD.Title = "Select the Object.XML";
        objectxmlD.ShowDialog();

        // Process input if the user clicked OK.
        baseMap = Utils.TextureLoader(baseMapD.FileName, false);
        collideMap = Utils.TextureLoader(collideMapD.FileName, false);
        objectsList = new LinkedList<AD2Object>[baseMap.Height];
        for (int i = 0; i != baseMap.Height; i++)
        {
            objectsList[i] = new LinkedList<AD2Object>();
        }

        //Default: go to utils.
        objectDirectory = folderBrowserD.SelectedPath;
        string[] files = Directory.GetFiles(objectDirectory);

        TextureName = new string[files.Length];
        TextureList = new Texture2D[files.Length];
        CollideTextureList = new Texture2D[files.Length];

        for (int i = 0; i != files.Length; i++)
        {
            TextureName[i] = Path.GetFileName(files[i]);
            TextureList[i] = Utils.TextureLoader(objectDirectory + "\\" + Path.GetFileName(files[i]), false);
            CollideTextureList[i] = Utils.TextureLoader(objectDirectory + "\\collide\\" + Path.GetFileName(files[i]), false);
        }

        Dictionary<string, LinkedList<string>> objsInPlace = Utils.GetXMLEntriesHash(objectxmlD.FileName,false);
        foreach (string obj in objsInPlace["object"])
        {
            AD2Object newObj = new AD2Object();
            string[] args = obj.Split(',');
            newObj.name = args[0];
            newObj.X = Int32.Parse(args[1]);
            newObj.Y = Int32.Parse(args[2]);
            for (int i = 0; i != TextureName.Length; i++)
            {
                if (newObj.name.Equals(TextureName[i]))
                {
                    newObj.t = TextureList[i];
                    newObj.collide = CollideTextureList[i];
                    break;
                }
            }
            objectsList[newObj.Y + (newObj.t.Height - 1)].AddFirst(newObj);
        }
    }
}
    
