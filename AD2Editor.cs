using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
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

    //RangedPathFinding node
    public class RPFNode
    {
        public RPFNode(int startX, int startY, int ID)
        {
            this.ID = ID;
            frontier = new LinkedList<int[]>();
            nodesInSet = new LinkedList<int[]>();
            frontier.AddFirst(new int[2] { startX, startY });
            nodesInSet.AddFirst(new int[2] { startX, startY });
            edges = new LinkedList<int>();
        }
        public int ID;
        public LinkedList<int[]> frontier;
        public LinkedList<int[]> nodesInSet;
        public LinkedList<int> edges;
    }

    //top left corner
    int camX = 0;
    int camY = 0;
    int mouseX = 0;
    int mouseY = 0;


    int MaxCharacterHeight = 16;
    int RangedPathFindingNodeDistance = 14;

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

        return (pointX >= x1 && pointY >= y1 && pointX <= x1 + (width - 1) && pointY <= y1 + (height - 1));
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


        generatePathfindingMesh(map);

    }
    
    // This is probably the biggest for-tower i've ever created and it does a LOT of repeat calcuations.
    public void generatePathfindingMesh(RenderTarget2D collmap)
    {
        Color[] pixels = new Color[collmap.Width * collmap.Height];
        collmap.GetData(pixels);
        //All of the places we can walk.
        bool[,] notWalkable = new bool[baseMap.Width, baseMap.Height];
        
        for (int x = 0; x != collmap.Width; x++)
        {
            for(int y = 0; y != collmap.Height; y++)
            {
                //BAD: Assumes 255/0/255 magic pank
                if(pixels[x + y  *collmap.Width].R == 255 && pixels[x + y * collmap.Width].G == 0 && pixels[x + y * collmap.Width].B == 255)
                {
                    for (int height = MaxCharacterHeight; height >= 0; height--)
                    {
                        for (int width = MaxCharacterHeight; width >= 0; width--)
                        {
                            if (y - height < 0 || x - width < 0)
                                    continue;
                            notWalkable[x - width, y - height] = true;
                        }
                    }
                }
            }
        }

        //Cannot walk along the far sides of map either.
        for (int x = 0; x != baseMap.Width; x++)
        {
            for(int y = 0; y != baseMap.Height; y++)
            {
                if (x > baseMap.Width - MaxCharacterHeight || y > baseMap.Height - MaxCharacterHeight)
                {
                    notWalkable[x, y] = true;
                }
            }
        }

        //step two: Generate a List of pathfinding node origins.
        bool[,] nodeClaimed = new bool[baseMap.Width, baseMap.Height];
        int[,] nodeOwner = new int[baseMap.Width, baseMap.Height];

        LinkedList<RPFNode> seeds = new LinkedList<RPFNode>();
        Utils.Log("Generating Seeds...");
        int nextMeshRegionID = 0;

        for (int xStart = 0; xStart < baseMap.Width; xStart += RangedPathFindingNodeDistance)
        {
            for(int yStart = 0; yStart < baseMap.Height; yStart+= RangedPathFindingNodeDistance)
            {
                if (!notWalkable[xStart, yStart])
                {
                    nodeClaimed[xStart, yStart] = true;
                    RPFNode newNode = new RPFNode(xStart, yStart, nextMeshRegionID);
                    seeds.AddFirst(newNode);
                    nextMeshRegionID++;
                }
            }
        }


        Utils.Log("Growing Seeds...");
        LinkedList<RPFNode> rootRegions = new LinkedList<RPFNode>();
        //step 3: Grow nodes until they can grow no longer. 
        while (seeds.Count > 0)
        {
            foreach (RPFNode seed in seeds)
            {
                //grow frontiers
                LinkedList<int[]> oldFrontier = seed.frontier;
                seed.frontier = new LinkedList<int[]>();
                foreach (int[] coord in oldFrontier)
                {
                    LinkedList<int[]> dirs = new LinkedList<int[]>();
                    dirs.AddFirst(new int[2] { -1, 0 });
                    dirs.AddFirst(new int[2] { 1, 0 });
                    dirs.AddFirst(new int[2] { 0, 1 });
                    dirs.AddFirst(new int[2] { 0, -1 });
                    foreach (int[] d in dirs)
                    {
                        int dx = d[0];
                        int dy = d[1];
                        if (collide(0, 0, baseMap.Width, baseMap.Height, coord[0] + dx, coord[1] + dy) && !nodeClaimed[coord[0] + dx, coord[1] + dy] && !notWalkable[coord[0] + dx, coord[1] + dy])
                        {
                            int[] newcoord = new int[2] { coord[0] + dx, coord[1] + dy };
                            seed.frontier.AddFirst(newcoord);
                            seed.nodesInSet.AddFirst(newcoord);
                            nodeClaimed[newcoord[0], newcoord[1]] = true;
                            nodeOwner[newcoord[0], newcoord[1]] = seed.ID;

                        }
                    }
                }
            }

            //remove nodes who have no frontier to grow.
            LinkedList<RPFNode> toRemove = new LinkedList<RPFNode>();
            foreach (RPFNode seed in seeds)
            {
                if (seed.frontier.Count == 0)
                    toRemove.AddFirst(seed);
            }

            foreach (RPFNode n in toRemove)
            {
                seeds.Remove(n);
                rootRegions.AddFirst(n);
            }
        }


        //step 4: find neighbors.
        Utils.Log("Finding Neighbors...");
        foreach (RPFNode region in rootRegions)
        {
            foreach (int[] node in region.nodesInSet)
            {
                //look for neigthbors.
                LinkedList<int[]> dirs = new LinkedList<int[]>();
                dirs.AddFirst(new int[2] { -1, 0 });
                dirs.AddFirst(new int[2] { 1, 0 });
                dirs.AddFirst(new int[2] { 0, 1 });
                dirs.AddFirst(new int[2] { 0, -1 });
                foreach (int[] d in dirs)
                {
                    int dx = d[0];
                    int dy = d[1];
                    if (collide(0, 0, baseMap.Width, baseMap.Height, node[0] + dx, node[1] + dy))
                    {
                        if (nodeOwner[node[0] + dx, node[1] + dy] != region.ID)
                        {
                            //We have an edge.
                            if (!region.edges.Contains(nodeOwner[node[0] + dx, node[1] + dy]))
                                region.edges.AddFirst(nodeOwner[node[0] + dx, node[1] + dy]);
                        }
                    }
                }
            }
        }

        //Step 5: The graph is complete, we have regions that can reach other regions. Write to XML.
        //create a new file 
        Utils.Log("Printing...");
        string path = "PathFindingMesh.xml";
        File.Create(path).Close();
        StreamWriter tw = new StreamWriter(path);

        tw.WriteLine("<PathMesh>");
        foreach (RPFNode node in rootRegions)
        {
            foreach (int neighbor in node.edges)
            {
                tw.WriteLine("    <Region" + node.ID + "Edge>" + neighbor + "</Region" + node.ID + "Edge>" );
            }
            foreach (int[] pixel in node.nodesInSet)
            {
                tw.WriteLine("    <Region" + node.ID + "Pixel>" + pixel[0] +"," + pixel[1] + "</Region" + node.ID + "Pixel>");
            }
        }
        tw.WriteLine("    <MeshRegionCount>" + nextMeshRegionID + " </MeshRegionCount>");
        tw.WriteLine("</PathMesh>");
        tw.Close();
    }


}

