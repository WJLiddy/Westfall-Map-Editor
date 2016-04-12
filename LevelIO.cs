using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using static AD2Editor;

public class LevelIO
{
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


    public static readonly int MaxCharacterHeight = 16;
    public static readonly int RangedPathFindingNodeDistance = 14;

    public static void generatePathfindingMesh(AD2Editor map, RenderTarget2D collmap, string saveloc)
    {
        Color[] pixels = new Color[collmap.Width * collmap.Height];
        collmap.GetData(pixels);
        //All of the places we can walk.
        bool[,] notWalkable = new bool[map.baseMap.Width, map.baseMap.Height];

        for (int x = 0; x != collmap.Width; x++)
        {
            for (int y = 0; y != collmap.Height; y++)
            {
                //BAD: Assumes 255/0/255 magic pank
                if (pixels[x + y * collmap.Width].R == 255 && pixels[x + y * collmap.Width].G == 0 && pixels[x + y * collmap.Width].B == 255)
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
        for (int x = 0; x != map.baseMap.Width; x++)
        {
            for (int y = 0; y != map.baseMap.Height; y++)
            {
                if (x > map.baseMap.Width - MaxCharacterHeight || y > map.baseMap.Height - MaxCharacterHeight)
                {
                    notWalkable[x, y] = true;
                }
            }
        }

        //step two: Generate a List of pathfinding node origins.
        bool[,] nodeClaimed = new bool[map.baseMap.Width, map.baseMap.Height];
        int[,] nodeOwner = new int[map.baseMap.Width, map.baseMap.Height];

        LinkedList<RPFNode> seeds = new LinkedList<RPFNode>();
        Utils.Log("Generating Seeds...");
        int nextMeshRegionID = 0;

        for (int xStart = 0; xStart < map.baseMap.Width; xStart += RangedPathFindingNodeDistance)
        {
            for (int yStart = 0; yStart < map.baseMap.Height; yStart += RangedPathFindingNodeDistance)
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
                        if (map.collide(0, 0, map.baseMap.Width, map.baseMap.Height, coord[0] + dx, coord[1] + dy) && !nodeClaimed[coord[0] + dx, coord[1] + dy] && !notWalkable[coord[0] + dx, coord[1] + dy])
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
                    if (map.collide(0, 0, map.baseMap.Width, map.baseMap.Height, node[0] + dx, node[1] + dy))
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
        string path = saveloc + "/mesh.xml";
        File.Create(path).Close();
        StreamWriter tw = new StreamWriter(path);

        tw.WriteLine("<PathMesh>");
        foreach (RPFNode node in rootRegions)
        {
            foreach (int neighbor in node.edges)
            {
                tw.WriteLine("    <Region" + node.ID + "Edge>" + neighbor + "</Region" + node.ID + "Edge>");
            }
            foreach (int[] pixel in node.nodesInSet)
            {
                tw.WriteLine("    <Region" + node.ID + "Pixel>" + pixel[0] + "," + pixel[1] + "</Region" + node.ID + "Pixel>");
            }
        }
        tw.WriteLine("    <MeshRegionCount>" + nextMeshRegionID + " </MeshRegionCount>");
        tw.WriteLine("</PathMesh>");
        tw.Close();
    }

    public static RenderTarget2D saveRenderedCollideMap(AD2Editor l, string saveloc)
    {
        //create a new file 
        RenderTarget2D map = new RenderTarget2D(Renderer.GraphicsDevice, l.baseMap.Width, l.baseMap.Height);
        //GraphicsDevice oldDevice =
        Renderer.GraphicsDevice.SetRenderTarget(map);

        SpriteBatch b = new SpriteBatch(Renderer.GraphicsDevice);

        b.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.Default,
            RasterizerState.CullNone);

        b.Draw(l.collideMap, new Rectangle(0, 0, map.Width, map.Height), Color.White);
        for (int i = 0; i != l.objectsList.Length; i++)
        {
            foreach (AD2Object a in l.objectsList[i])
            {
                b.Draw(a.collide, new Rectangle(a.X, a.Y, a.t.Width, a.t.Height), Color.White);
            }
        }

        b.End();

        Renderer.GraphicsDevice.SetRenderTarget(null);

        Stream stream = File.Create(saveloc + "/collide.png");

        //Save as PNG
        map.SaveAsPng(stream, map.Width, map.Height);
        stream.Dispose();
        return map;

    }

    public static void generateObjectsXML(AD2Editor map, string saveLoc)
    {
        //create a new file 
        string path = saveLoc + "/objects/";
        Directory.CreateDirectory(path);
        string file = path + "object.xml";
        //File.Create(file).Close();
        StreamWriter tw = new StreamWriter(file);

        tw.WriteLine("<Obj>");
        for (int i = 0; i != map.objectsList.Length; i++)
        {
            foreach (AD2Object a in map.objectsList[i])
            {
                tw.WriteLine("    <object>" + a.name + "," + a.X + "," + a.Y + "</object>");
            }
        }
        tw.WriteLine("</Obj>");
        tw.Close();
    }


    public static void generateMapXML(AD2Editor map, string saveLoc)
    {
        string file = saveLoc + "//map.xml";
        StreamWriter tw = new StreamWriter(file);

        tw.WriteLine("<Map>");
        tw.WriteLine("    <base>base.png</base>");
        tw.WriteLine("    <collision>collide.png</collision>");
        tw.WriteLine("    <object>objects/object.xml</object>");
        tw.WriteLine("    <collisionKeyR>255</collisionKeyR>");
        tw.WriteLine("    <collisionKeyG>0</collisionKeyG>");
        tw.WriteLine("    <collisionKeyB>255</collisionKeyB>");
        tw.WriteLine("</Map>");
        tw.Close();
    }

    public static string getSaveLocation()
    {
        string folderPath = "";
        FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
        if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
        {
            folderPath = folderBrowserDialog1.SelectedPath;
        }
        return folderPath;
    }

    public static void Save(AD2Editor map)
    {
        string saveLocation = getSaveLocation();
        generateObjectsXML(map,saveLocation);
        generateMapXML(map, saveLocation);
        RenderTarget2D cmap = saveRenderedCollideMap(map,saveLocation);
        generatePathfindingMesh(map, cmap,saveLocation);

        //move over basemap as well
        Stream stream = File.Create(saveLocation + "\\base.png");
        map.baseMap.SaveAsPng(stream, map.baseMap.Width, map.baseMap.Height);
        stream.Dispose();


        //Don't forget about objects.
        string[] files = Directory.GetFiles(map.objectDirectory);

        System.IO.Directory.CreateDirectory(saveLocation + "\\objects\\");
        foreach(string file in files)
        {
            string targetPath = saveLocation + "\\objects\\";
            string destFile = System.IO.Path.Combine(targetPath, Path.GetFileName(file));            
            System.IO.File.Copy(file, destFile, true);
           
        }
    }
}


