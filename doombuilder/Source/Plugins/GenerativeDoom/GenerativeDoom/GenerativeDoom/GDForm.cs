using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Config;
using System.Threading;

namespace GenerativeDoom
{
    public partial class GDForm : Form
    {

        public const int TYPE_PLAYER_START = 1;
        const int LEVEL_SIZE = 25;

        public enum SalleType
        {
            PLAYER_START,
            ENEMY,
            AMMO,
            LIFE,
            BOSS,
            CLEAR
        }

        public struct Salle
        {
            public SalleType type;
            public int progression;
        }

        private IList<DrawnVertex> points;
        private Random rand = new Random();

        public GDForm()
        {
            InitializeComponent();
            points = new List<DrawnVertex>();
        }

        // We're going to use this to show the form
        public void ShowWindow(Form owner)
        {
            // Position this window in the left-top corner of owner
            this.Location = new Point(owner.Location.X + 20, owner.Location.Y + 90);

            // Show it
            base.Show(owner);
        }

        // Form is closing event
        private void GDForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // When the user is closing the window we want to cancel this, because it
            // would also unload (dispose) the form. We only want to hide the window
            // so that it can be re-used next time when this editing mode is activated.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Just cancel the editing mode. This will automatically call
                // OnCancel() which will switch to the previous mode and in turn
                // calls OnDisengage() which hides this window.
                General.Editing.CancelMode();
                e.Cancel = true;
            }
        }

        private void GDForm_Load(object sender, EventArgs e)
        {

        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            General.Editing.CancelMode();
        }

        private void newSector(IList<DrawnVertex> points, int lumi, int ceil, int floor)
        {
            Console.Write("New sector: ");
            List<DrawnVertex> pSector = new List<DrawnVertex>();

            DrawnVertex v;
            v = new DrawnVertex();
            foreach (DrawnVertex p in points)
            {
                Console.Write(" p:"+p.pos);
                v.pos = p.pos;
                v.stitch = true;
                v.stitchline = true;
                pSector.Add(v);
            }

            Console.Write("\n");

            v.pos = points[0].pos;
            v.stitch = true;
            v.stitchline = true;

            pSector.Add(v);

            Tools.DrawLines(pSector);

            // Snap to map format accuracy
            General.Map.Map.SnapAllToAccuracy();

            // Clear selection
            General.Map.Map.ClearAllSelected();

            // Update cached values
            General.Map.Map.Update();

            // Edit new sectors?
            List<Sector> newsectors = General.Map.Map.GetMarkedSectors(true);

            foreach(Sector s in newsectors)
            {
                s.CeilHeight = ceil;
                s.FloorHeight = floor;
                s.Brightness = lumi;
            }


            // Update the used textures
            General.Map.Data.UpdateUsedTextures();

            // Map is changed
            General.Map.IsChanged = true;

            General.Interface.RedrawDisplay();

            //Ajoute, on enleve la marque sur les nouveaux secteurs
            General.Map.Map.ClearMarkedSectors(false);
        }

        private void newSector(DrawnVertex top_left, float width, float height, int lumi, int ceil, int floor)
        {
            points.Clear();
            DrawnVertex v;
            v = new DrawnVertex();
            v.pos = top_left.pos;
            points.Add(v);

            v.pos.y += height;
            points.Add(v);

            v.pos.x += width;
            
            points.Add(v);

            v.pos.y -= height;
            points.Add(v);

            newSector(points,lumi,ceil,floor);

            points.Clear();
        }

        private Thing addThing(Vector2D pos, String category, float proba = 0.5f)
        {
            Thing t = addThing(pos);
            if (t != null)
            {

                IList<ThingCategory> cats = General.Map.Data.ThingCategories;
                Random r = new Random();

                bool found = false;
                foreach (ThingTypeInfo ti in General.Map.Data.ThingTypes)
                {
                    if (ti.Category.Name == category)
                    {
                        t.Type = ti.Index;
                        Console.WriteLine("Add thing cat " + category + " for thing at pos " + pos);
                        found = true;
                        if (r.NextDouble() > proba)                     
                            break;
                    }

                }
                if (!found)
                {
                    Console.WriteLine("###### Could not find category " + category + " for thing at pos " + pos);
                }else
                    t.Rotate(0);
            }else
            {
                Console.WriteLine("###### Could not add thing for cat " + category + " at pos " + pos);
            }

            return t;
        }

        private Thing addThing(Vector2D pos, int type)
        {
            Thing t = addThing(pos);
            if (t != null)
            {
                t.Type = type;
            }
            return t;
        }

        private Thing addThing(Vector2D pos)
        {
            if (pos.x < General.Map.Config.LeftBoundary || pos.x > General.Map.Config.RightBoundary ||
                pos.y > General.Map.Config.TopBoundary || pos.y < General.Map.Config.BottomBoundary)
            {
                Console.WriteLine( "Error Generaetive Doom: Failed to insert thing: outside of map boundaries.");
                return null;
            }

            // Create thing
            Thing t = General.Map.Map.CreateThing();
            if (t != null)
            {
                General.Settings.ApplyDefaultThingSettings(t);

                t.Move(pos);

                t.UpdateConfiguration();

                // Update things filter so that it includes this thing
                General.Map.ThingsFilter.Update();

                // Snap to map format accuracy
                t.SnapToAccuracy();
            }

            return t;
        }

        private void correctMissingTex()
        {

            String defaulttexture = "-";
            if (General.Map.Data.TextureNames.Count > 1)
                defaulttexture = General.Map.Data.TextureNames[1];

            // Go for all the sidedefs
            foreach (Sidedef sd in General.Map.Map.Sidedefs)
            {
                // Check upper texture. Also make sure not to return a false
                // positive if the sector on the other side has the ceiling
                // set to be sky
                if (sd.HighRequired() && sd.HighTexture[0] == '-')
                {
                    if (sd.Other != null && sd.Other.Sector.CeilTexture != General.Map.Config.SkyFlatName)
                    {
                        sd.SetTextureHigh(General.Settings.DefaultCeilingTexture);
                    }
                }

                // Check middle texture
                if (sd.MiddleRequired() && sd.MiddleTexture[0] == '-')
                {
                    sd.SetTextureMid(General.Settings.DefaultTexture);
                }

                // Check lower texture. Also make sure not to return a false
                // positive if the sector on the other side has the floor
                // set to be sky
                if (sd.LowRequired() && sd.LowTexture[0] == '-')
                {
                    if (sd.Other != null && sd.Other.Sector.FloorTexture != General.Map.Config.SkyFlatName)
                    {
                        sd.SetTextureLow(General.Settings.DefaultFloorTexture);
                    }
                }

            }
        }

        private bool checkIntersect(Line2D measureline)
        {
            bool inter = false;
            foreach (Linedef ld2 in General.Map.Map.Linedefs)
            {
                // Intersecting?
                // We only keep the unit length from the start of the line and
                // do the real splitting later, when all intersections are known
                float u;
                if (ld2.Line.GetIntersection(measureline, out u))
                {
                    if (!float.IsNaN(u) && (u > 0.0f) && (u < 1.0f))
                    {
                        inter = true;
                        break;
                    }
                       
                }
            }

            Console.WriteLine("Chevk inter " + measureline + " is " + inter);

            return inter;
        }

        private bool checkIntersect(Line2D measureline, out float closestIntersect)
        {
            // Check if any other lines intersect this line
            List<float> intersections = new List<float>();
            foreach (Linedef ld2 in General.Map.Map.Linedefs)
            {
                // Intersecting?
                // We only keep the unit length from the start of the line and
                // do the real splitting later, when all intersections are known
                float u;
                if (ld2.Line.GetIntersection(measureline, out u))
                {
                   if (!float.IsNaN(u) && (u > 0.0f) && (u < 1.0f))
                        intersections.Add(u);
                }
            }

            if(intersections.Count() > 0)
            {
                // Sort the intersections
                intersections.Sort();

                closestIntersect = intersections.First<float>();

                return true;
            }

            closestIntersect = 0.0f;
            return false;

            
        }
    

        private void showCategories()
        {
            lbCategories.Items.Clear();
            IList<ThingCategory> cats = General.Map.Data.ThingCategories;
            foreach(ThingCategory cat in cats)
            {
                if (!lbCategories.Items.Contains(cat.Name))
                    lbCategories.Items.Add(cat.Name);
            }
            
        }
        private void makeOnePath( bool playerStart)
        {
            

            Random r = new Random();

            DrawnVertex v = new DrawnVertex();
            float pwidth = 0.0f;
            float pheight = 0.0f;

            int lumi = 200;
            int ceil = (int)(r.NextDouble() * 128 + 128);
            int floor = (int)(r.NextDouble() * 128 + 128);
            int pfloor = floor;
            int pceil = ceil;
            Vector2D pv = new Vector2D();


            int pdir = 0;
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine("---------------------- Sector " + i);

                pv = v.pos;

                //Taille du prochain secteur
                float width = (float)(r.Next() % 10) * 64.0f + 128.0f;
                float height = (float)(r.Next() % 10) * 64.0f + 128.0f;

                //On checke ou on peut le poser
                Line2D l1 = new Line2D();
                bool[] dirOk = new bool[4];

                //A droite
                l1.v2 = l1.v1 = pv;
                l1.v1.x += pwidth;
                l1.v2.x += width + 2048;
                bool droite = !checkIntersect(l1);
                l1.v1.y = l1.v2.y = l1.v1.y + height;
                droite = droite && !checkIntersect(l1);
                dirOk[0] = droite;
                Console.WriteLine("Droite ok:" + droite);


                //A gauche
                l1.v2 = l1.v1 = pv;
                l1.v2.x -= width + 2048;
                bool gauche = !checkIntersect(l1);
                l1.v1.y = l1.v2.y = l1.v1.y + height;
                gauche = gauche && !checkIntersect(l1);
                dirOk[1] = gauche;
                Console.WriteLine("Gauche ok:" + gauche);

                //En haut
                l1.v2 = l1.v1 = pv;
                l1.v1.y = l1.v2.y = l1.v1.y + pheight;
                l1.v2.y += height + 2048;
                bool haut = !checkIntersect(l1);
                l1.v1.x = l1.v2.x = l1.v1.x + width;
                haut = haut && !checkIntersect(l1);
                dirOk[2] = haut;
                Console.WriteLine("Haut ok:" + haut);

                //En bas
                l1.v2 = l1.v1 = pv;
                l1.v2.y -= height + 2048;
                bool bas = !checkIntersect(l1);
                l1.v1.x = l1.v2.x = l1.v1.x + width;
                bas = bas && !checkIntersect(l1);
                dirOk[3] = bas;
                Console.WriteLine("Bas ok:" + bas);

                bool oneDirOk = haut || bas || gauche || droite;

                int nextDir = pdir;
                if (!oneDirOk)
                    Console.WriteLine("No dir available, on va croiser !!!");
                else
                {
                    int nbTry = 0;
                    while ((!dirOk[nextDir] || pdir == nextDir) && nbTry++ < 100)
                        nextDir = r.Next() % 4;
                }



                switch (nextDir)
                {
                    case 0: //droite
                        Console.WriteLine("On va a droite !");
                        v.pos.x += pwidth;
                        break;
                    case 1: //gauche
                        Console.WriteLine("On va a gauche !");
                        v.pos.x -= width;
                        break;
                    case 2: //haut
                        Console.WriteLine("On va en haut !");
                        v.pos.y += pheight;
                        break;
                    case 3: //bas
                        Console.WriteLine("On va en bas !");
                        v.pos.y -= height;
                        break;

                }





                /*if (r.NextDouble() > 0.5)
                    v.pos.x += pwidth; //(r.NextDouble() > 0.5 ? width : -nwidth);
                else
                    v.pos.y += pheight; //(r.NextDouble() > 0.5 ? height : -nheight);*/



                lumi = Math.Min(256, Math.Max(0, lumi + (r.NextDouble() > 0.5 ? 1 : -1) * 16));

                floor = Math.Min(256, Math.Max(0, pfloor + (r.NextDouble() > 0.5 ? 1 : -1) * 16));
                ceil = Math.Min(256, Math.Max(0, pceil + (r.NextDouble() > 0.5 ? 1 : -1) * 16));

                if (ceil - floor < 100)
                {
                    /*ceil = floor = (ceil + floor) / 2;
                    while(ceil - floor < 128)
                    {
                        floor = Math.Max(0, floor - 64);
                        ceil = Math.Min(256, floor + 128);
                    }*/
                    floor = pfloor;
                    ceil = floor + 180 + (r.NextDouble() > 0.5 ? 1 : -1) * 16;


                }



                newSector(v, width,
                    height,
                    lumi,
                    ceil,
                    floor);

                //Faire une porte avec une nouvelle linedef sur son secteur


                if (i == 0 && playerStart)
                {
                    Thing t = addThing(new Vector2D(v.pos.x + width / 2, v.pos.y + height / 2));
                    t.Type = TYPE_PLAYER_START;
                    t.Rotate(0);
                }
                else if (i == 1)
                {
                    addThing(new Vector2D(v.pos.x + width / 2, v.pos.y + height / 2), "weapons");
                }
                else if (i % 3 == 0)
                {
                    while (r.NextDouble() > 0.3f)
                        addThing(new Vector2D(v.pos.x + width / 2 + ((float)r.NextDouble() * (width / 2)) - width / 4,
                            v.pos.y + height / 2 + ((float)r.NextDouble() * (height / 2)) - height / 4), "monsters");
                }
                if (i % 5 == 0)
                {
                    do
                    {

                        addThing(new Vector2D(v.pos.x + width / 2 + ((float)r.NextDouble() * (width / 2)) - width / 4,
                            v.pos.y + height / 2 + ((float)r.NextDouble() * (height / 2)) - height / 4), "ammunition");
                    } while (r.NextDouble() > 0.3f);
                }
                if (i % 20 == 0)
                {
                    do
                    {
                        addThing(new Vector2D(v.pos.x + width / 2 + ((float)r.NextDouble() * (width / 2)) - width / 4,
                            v.pos.y + height / 2 + ((float)r.NextDouble() * (height / 2)) - height / 4), "health");
                    } while (r.NextDouble() > 0.5f);
                    addThing(new Vector2D(v.pos.x + width / 2, v.pos.y + height / 2), "weapons", 0.3f);
                }

                while (r.NextDouble() > 0.5f)
                    addThing(new Vector2D(v.pos.x + width / 2 + ((float)r.NextDouble() * (width / 2)) - width / 4,
                        v.pos.y + height / 2 + ((float)r.NextDouble() * (height / 2)) - height / 4), "decoration", (float)r.NextDouble());


                pwidth = width;
                pheight = height;
                pceil = ceil;
                pfloor = floor;
                pdir = nextDir;

                // Handle thread interruption
                try { Thread.Sleep(0); }
                catch (ThreadInterruptedException) { Console.WriteLine(">>>> thread de generation interrompu at sector " + i); break; }
            }

           


            
        }

        private void btnDoMagic_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Trying to do some magic !!");

            makeOnePath(true);
            makeOnePath(false);
            makeOnePath(false);

            correctMissingTex();

            Console.WriteLine("Did I ? Magic ?");
        }

        private void btnAnalysis_Click(object sender, EventArgs e)
        {
            //foreach (ThingTypeInfo ti in General.Map.Data.ThingTypes)
            //Console.WriteLine(ti.Category.Name);
            showCategories();

        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            Generate();

            correctMissingTex();
        }

        private void Generate(int size = 50)
        {
            //On génère les données du niveau
            TableGraph<Salle> graph = generateGraph(new TableGraph<Salle>(),new Position(0,0), 0, 25, true);
            //On applique le tout dans doom builder
            generateFromGraph(graph);
        }

        /*Construit un graph représentant un niveau*/
        private TableGraph<Salle> generateGraph(TableGraph<Salle> g, Position start, int progression, int size, bool first=false)
        {
            Position pos = start;
            Random r = new Random();
            for (int i = 0; i < size; i++)
            {
                List<Position> next;
                if (i == 0 && first) next = buildRoom(g, pos, 4, 4, 0, true); //Salle de départ
                else if (i == size - 1 && first) next = buildRoom(g, pos, 10, 10, progression + i, false, true);
                else
                {
                    //On construit la prochaine salle et récupère les sorties possibles
                    next = buildRoom(g, pos, 4 + (int)(rand.NextDouble() * 7), 4 + (int)(rand.NextDouble() * 7), i + progression);
                }
                //Si il n'y a pas de sortie, on stoppe
                if (next==null || next.Count <= 0) return g;
                //40% de chance de créer un deuxième chemin 
                if (!(first && i==0) && next.Count>2 && rand.NextDouble() < 0.2)
                {
                    Position newPath = next[(int)(rand.NextDouble() * next.Count)];
                    g = generateGraph(g, newPath, progression, (size-i)/2);
                    size -= (size - i)/2;
                    next.Remove(newPath);
                }
                //On récupère une position valable pour continuer la construction
                //La boucle est la pour être sur qu'on est pas coincé par le chemin alternatif créé avant
                while (next.Count > 0)
                {
                    pos = next[(int)(rand.NextDouble() * next.Count)];
                    if (g.freeNearbyPosition(pos).Count > 0)
                        break;
                    next.Remove(pos);
                }
                //S'il n'y a plus de place, on arrête
                if (next.Count == 0)
                    return g;

            }
            return g;
        }

        /*Construction d'une salle rectangulaire*/
        private List<Position> buildRoom(TableGraph<Salle> graph, Position p, int width, int height,int progression, bool start=false, bool boss=false)
        {
            //On récupère les direction dans laquelle on peut construire
            List<Direction> dirs = graph.freeNearbyPosition(p);
            if (dirs.Count <= 0) return null;
            double rf = rand.NextDouble();
            //On en choisit une au hasard
            Direction chosen = dirs[(int)(rf * dirs.Count)];
            Position center;
            Position entry;
            //Initialisation des données en fonction de la direction
            //graph[p] corresponds au noeud du graphe par lequel le joueur va entrer
            switch (chosen)
            {
                case Direction.LEFT:
                    center = new Position(-width / 2, 0) + p;
                    if(!graph.isEmpty(p))
                        graph[p].left = true;
                    entry = new Position(-1, 0) + p;
                break;
                case Direction.RIGHT:
                    center = new Position(width / 2, 0) + p;
                    if (!graph.isEmpty(p))
                        graph[p].right = true;
                    entry = new Position(1, 0) + p;
                    break;
                case Direction.UP:
                    center = new Position(0, -height/2) + p;
                    if (!graph.isEmpty(p))
                        graph[p].up = true;
                    entry = new Position(0, -1) + p;
                    break;
                case Direction.DOWN:
                    center = new Position(0, height/2) + p;
                    if (!graph.isEmpty(p))
                        graph[p].down = true;
                    entry = new Position(0, 1) + p;
                    break;
                default: center = p; entry = p; break;
            }
            List<Position> ret = new List<Position>();
            bool boss_placed = false;
            //Création des noeuds
            for(int i = -width / 2; i < width / 2; i++)
            {
                for(int j = -height/2; j< height/2; j++)
                {
                    Salle s = new Salle();
                    s.progression = progression;
                    Position sallePos = center + new Position(i, j);
                    if (start) //Si c'est la salle de départ, on ne mets rien d'autre que le joueur
                    {
                        if (sallePos.Equals(center))
                            s.type = SalleType.PLAYER_START;
                        else
                            s.type = SalleType.CLEAR;
                    }
                    else if (boss) //Si c'est la salle du boss, pareil
                    {
                        if (!boss_placed)
                        {
                            s.type = SalleType.BOSS;
                            boss_placed = true;
                        }
                        else
                        {
                            s.type = SalleType.CLEAR;

                        }
                    }
                    else
                    {
                        //On tire au hasard un type de remplissage de la case en fonction de la progression dans le niveau
                        s.type = getRandomFromProgression(progression);
                    }
                    if (graph.isEmpty(sallePos)) //On ajoute la case que si elle n'est pas déjà occupé
                    {
                        graph.Add(sallePos, s);
                        //On mets un chemin entre les noeuds sauf si on est sur les bords
                        if (i > -width / 2) graph[sallePos].left = true;
                        if (i < width / 2 - 1) graph[sallePos].right = true;
                        if (j > -height / 2) graph[sallePos].up = true;
                        if (j < height / 2 - 1) graph[sallePos].down = true;
                    }
                    else if (rand.NextDouble() < 0.01) //faible chance de creuser un mur et relier deux salles entre elles
                    {
                        graph[sallePos].down = true;
                        graph[sallePos].up = true;
                        graph[sallePos].left = true;
                        graph[sallePos].right = true;
                    }
                    //Si on est sur un bord de la salle, on ajoute la case comme une sortie possible
                    if(i==-width/2 || i==width/2-1 || j==-height/2 || j==height/2-1)
                    {
                        if (!sallePos.Equals(p))
                        {
                            ret.Add(sallePos);
                        }
                    }
                }
            }
            //On enlève des sorties possibles celles qui sont bloqués
            for(int i=ret.Count-1;i>=0;i--)
            {
                if (graph.freeNearbyPosition(ret[i]).Count <= 0) ret.RemoveAt(i);
            }
            //Si l'entrée de la salle est la sortie d'une autre salle, on les connecte
            if (!graph.isEmpty(p))
            {
                switch (chosen)
                {
                    case Direction.LEFT:
                        graph[entry].right = true;
                        break;
                    case Direction.RIGHT:
                        graph[entry].left = true;
                        break;
                    case Direction.UP:
                        graph[entry].down = true;
                        break;
                    case Direction.DOWN:
                        graph[entry].up = true;
                        break;
                }

            }
            return ret;
        }

        /*Construit le niveau à partir du graphe*/
        private void generateFromGraph(TableGraph<Salle> graph)
        {
            //Parcours des noeuds du graphe
            foreach(var t in graph.nodes)
            {
                DrawnVertex p = new DrawnVertex();
                p.pos.x = t.Key.x*64;
                p.pos.y = t.Key.y*64;
                //Largeur des noeuds fixes
                int width = 64; 
                int height = 64;
                //On déplace un peu pour créer des murs s'il n'y a pas de lien avec le noeud d'à coté
                if (!t.Value.left)
                {
                    width -= 4;
                    p.pos.x += 4;
                }
                if (!t.Value.right)
                {
                    width -= 4;
                }
                if (!t.Value.up)
                {
                    height -= 4;
                    p.pos.y += 4;
                }
                if (!t.Value.down)
                {
                    height -= 4;
                }
                //Création du sécteur
                newSector(p, width, height, 255, 255, 0);
                //On place les things
                Vector2D pos = new Vector2D(p.pos.x + width / 2, p.pos.y + height / 2);
                switch (t.Value.data.type)
                {
                    case SalleType.PLAYER_START:
                        Thing thing = addThing(pos);
                        thing.Type = TYPE_PLAYER_START;
                        break;
                    case SalleType.ENEMY:
                        addEnemy(pos, t.Value.data.progression);
                        break;
                    case SalleType.AMMO:
                        addAmmo(pos, t.Value.data.progression);
                        break;
                    case SalleType.LIFE:
                        addHealth(pos, t.Value.data.progression);
                        break;
                    case SalleType.BOSS:
                        addThing(pos, 7);
                        break;
                }
                
            }
        }

        /*Génère des types aléatoires*/
        private SalleType getRandomFromProgression(int progression)
        {
            double d = rand.NextDouble();
            float range = 1.0f;
            if (d < 0.8 - (progression / (float)LEVEL_SIZE) )
                return SalleType.CLEAR;
            d = (d-(0.8 - (progression / (float)LEVEL_SIZE)))/(1- (0.8 - (progression / (float)LEVEL_SIZE))); //On remets entre 0 et 1, plus simple
            if (d < 0.5 + (progression / (float)LEVEL_SIZE))
                return SalleType.ENEMY;
            d = (d - (0.5 + (progression / (float)LEVEL_SIZE))) / (1 - (0.5 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.5 - (progression / (float)LEVEL_SIZE))
                return SalleType.LIFE;
            d = (d - (0.5 - (progression / (float)LEVEL_SIZE))) / (1 - (0.5 - (progression / (float)LEVEL_SIZE)));
            return SalleType.AMMO;
        }

        private void addEnemy(Vector2D pos, int progression)
        {
            double factor;
            double d = rand.NextDouble();
            if (d < 0.8 - 4*(progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 3004);
                return;
            }
            d = (d - (0.8 - 2*(progression / (float)LEVEL_SIZE))) / (1 - (0.8 - 2*(progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 9);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            factor = 0.8 - 0.5*(progression / (float)LEVEL_SIZE);
            if (d < factor)
            {
                addThing(pos, 3001);
                return;
            }
            d = (d - factor) / (1 - factor);
            factor = 0.5 - 0.25*(progression / (float)LEVEL_SIZE);
            if (d < factor)
            {
                addThing(pos, 67);
                return;
            }
            d = (d - factor) / (1 - factor);
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 65);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.2 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 16);
                return;
            }
        }

        private void addHealth(Vector2D pos, int progression)
        {
            double d = rand.NextDouble();
            if (d < 0.5)
            {
                addThing(pos, 2015);
                return;
            }
            d = (d - 0.5) / (1 - 0.5);
            if (d < 0.5)
            {
                addThing(pos, 2014);
                return;
            }
            d = (d - (0.5)) / (1 - (0.5));
            if (d < 0.5)
            {
                addThing(pos, 2012);
                return;
            }
            d = (d - 0.5) / (1 - 0.5);
            if (d < 0.5)
            {
                addThing(pos, 2018);
                return;
            }
            d = (d - 0.5) / (1 - (0.5));
            if (d < 0.5)
            {
                addThing(pos, 2011);
                return;
            }
            d = (d - 0.5) / (1 - (0.5));
            addThing(pos, 2019);
            return;
            
        }

        private void addAmmo(Vector2D pos, int progression)
        {
            double d = rand.NextDouble();
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2007);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2001);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2008);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2002);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2048);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2003);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2004);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2047);
                return;
            }
            d = (d - (0.8 - (progression / (float)LEVEL_SIZE))) / (1 - (0.8 - (progression / (float)LEVEL_SIZE)));
            if (d < 0.8 - (progression / (float)LEVEL_SIZE))
            {
                addThing(pos, 2006);
                return;
            }
        }
    }
}
