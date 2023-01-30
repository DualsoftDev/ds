// https://www.developpez.net/forums/d1064165/dotnet/langages/csharp/program-utlise-quick-graph/










// 1 -AJOUTER REFERENCE DANS DOSSIER QuickGraph ?:
//- QuickGraph.dll.dll
//- QuickGraph.Data.dll
//- QuickGraph.Glee.dll
//
// 2 -AJOUTER REFERENCE DANS DANS DOSSIER du controle MS GLEE:
//- Microsoft.GLEE.dll
//- Microsoft.GLEE.Drawing.dll
//- Microsoft.GLEE.GraphViewerGDI.dll
// 3-BOITE ? OUTILS ->CHOISIR ELEMENTS->PARCOURIR dossier MS GLEE
// &  SELECTIONNER FICHIER Microsoft.GLEE.GraphViewerGDI.dll
// 4-AJOUTER LES CONTROLES Bouton,un OpenDialog et un TextBox et GViewer
// 
// 5-NB: j'ai utilise la fonction utilitaire toute prete  Microsoft.VisualBasic.FileIO.TextFieldParser 
// qui permet de parser un fichier texte delimited avec un delimter au choix.   
// libre ? vous d'utiliser d'autres API si possible en C#
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//AJOUTER UNE REFERENCE AU FICHIER Microsoft.VisualBasic.dll
using Microsoft.VisualBasic;
//USINGS DE  QUICKGRAPH
using QuickGraph;
using QuickGraph.Algorithms;
// USING DE  QUICKGRAPH POUR LIAISON AVEC  MS GLEE 
using QuickGraph.Glee;

//USINGS MICROSOFT MS GLEE
using Microsoft.Glee;
using Microsoft.Glee.Drawing;
using Microsoft.Glee.Splines;
using P = Microsoft.Glee.Splines.Point;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        // VARIABLES POUR LIRE CLASSE ARC ET LISTE ARCS
        //---------------------------------------------
        public Arc monArc = new Arc();
        public List<Arc> lstArcs = new List<Arc>();

        //DESCRIPTION DU GRAPHE
        //---------------------
        // GRAPHE D'ADJACENCE SIMPLE TYPE QuickGraph
        AdjacencyGraph<string, Edge<string>> monGraph;
        // ARC  DU GRAPHE
        QuickGraph.Edge<string> arcGraphe;
        //"COUT ARC"  DE TYPE DICTIONNAIRE (ARCGRAPH,LONGUEUR)
        public Dictionary<Edge<string>, Double> coutArc = new Dictionary<Edge<string>, Double>();


        //EXPLORATION DU GRAPHE
        //--------------------
        // ALGORITHME TYPE "BFS"
        QuickGraph.Algorithms.Search.BreadthFirstSearchAlgorithm<string, QuickGraph.Edge<string>> monBFS;
        //"SOMMET OBSERVATEUR" (CHEMIN D'EXPLORATION SOMMET)
        QuickGraph.Algorithms.Observers.VertexPredecessorRecorderObserver<string, QuickGraph.Edge<String>> predecessorObserverSommet;
        //"ARC OBSERVATEUR" (CHEMIN D'EXPLORATION ARC)
        QuickGraph.Algorithms.Observers.EdgeRecorderObserver<string, QuickGraph.Edge<String>> predecessorObserverArc;
        //"OBSERVATEUR DISTANCE" (LONGUEUR DU CHEMIN EXPLORE)
        QuickGraph.Algorithms.Observers.VertexDistanceRecorderObserver<string, QuickGraph.Edge<string>> distObserver;

        //VISUALISATION GRAPHIQUE
        //-----------------------
        //"GRAPHE VISUEL"  TYPE MS GLEE
        Microsoft.Glee.Drawing.Graph grapheVisuel;
        public Form1()
        {
            InitializeComponent();
        }
        //'obtient le nom de fichier texte contenant les donnees du graphe
        private void btnFichierArcs_Click(object sender, EventArgs e)
        {

            string nomFichierArcs = "";
            OpenFileDialog dlgOuvreFichier = new OpenFileDialog();
            dlgOuvreFichier.Filter = "Fichier Arcs(*.txt)|*.txt";
            if (dlgOuvreFichier.ShowDialog() == DialogResult.OK)
            {

                nomFichierArcs = dlgOuvreFichier.SafeFileName;
                if (nomFichierArcs == null)
                {

                    MessageBox.Show("entrer un nom de fichier svp...");
                    return;
                }
                LitFichierGraphe(nomFichierArcs, monArc, lstArcs);
                AfficheGraphe();

            }
        }

        //Cree le Graphe & l'affiche  avec le controle Gviewer ( lib MS GLEE )  
        private void AfficheGraphe()
        {
            //Peuple  le "graphe" ? partir des donnes arcs
            monGraph = new AdjacencyGraph<string, QuickGraph.Edge<string>>();
            coutArc = new Dictionary<Edge<string>, double>(monGraph.EdgeCount);
            Peupler_Graphe(monGraph);

            //Initialise  Algo BFS 
            monBFS = new QuickGraph.Algorithms.Search.BreadthFirstSearchAlgorithm<string, QuickGraph.Edge<string>>(monGraph);
            predecessorObserverSommet = new QuickGraph.Algorithms.Observers.VertexPredecessorRecorderObserver<string, QuickGraph.Edge<String>>();
            predecessorObserverSommet.Attach(monBFS);
            predecessorObserverArc = new QuickGraph.Algorithms.Observers.EdgeRecorderObserver<string, QuickGraph.Edge<String>>();
            predecessorObserverArc.Attach(monBFS);
            distObserver = new QuickGraph.Algorithms.Observers.VertexDistanceRecorderObserver<string, QuickGraph.Edge<string>>(QuickGraph.Algorithms.AlgorithmExtensions.GetIndexer(coutArc));
            distObserver.Attach(monBFS);
            //Execute Algo "BFS" avec  noeud "SOURCE" comme racine
            monBFS.Compute("SOURCE");
            //Liaison avec le controle graphe GVIEWER MS GLEE se fait 
            //avec la classe "populator de QuickGraph" comme suit:

            QuickGraph.Glee.GleeGraphPopulator<string, QuickGraph.Edge<string>>
            populator = QuickGraph.Glee.GleeGraphExtensions.CreateGleePopulator(monGraph);

            //2 Hooks (abonnements) aux evenements populator_NodeAdded & populator_EdgeAdded pour personnaliser l'affichage des Noeuds et Arcs du graphe
            try
            {
                populator.NodeAdded += new QuickGraph.Glee.GleeVertexNodeEventHandler<string>(populator_NodeAdded);
                populator.EdgeAdded += new QuickGraph.Glee.GleeEdgeEventHandler<string, Edge<string>>(populator_EdgeAdded);
                populator.Compute();
            }
            finally
            {
                populator.NodeAdded += new QuickGraph.Glee.GleeVertexNodeEventHandler<string>(populator_NodeAdded);
                populator.EdgeAdded += new QuickGraph.Glee.GleeEdgeEventHandler<string, Edge<string>>(populator_EdgeAdded);
            }
            //Obtient un graphe visuel  pour  MS GLEE
            grapheVisuel = populator.VisitedGraph.ToGleeGraph(populator_NodeAdded, populator_EdgeAdded);

            //Ajuste le controle Gviewer
            //active options  Sauvegarde & Scroll
            GViewer1.SaveButtonVisible = true;
            GViewer1.AutoScroll = true;

            //Affiche le "graphe visuel" dans Gviewer
            GViewer1.Graph = grapheVisuel;

            //Affiche en plus une simple liste des Noeuds et Arcs 
            //du Chemin BFS dans TextBox1
            AfficheCheminGraphVisite();


        }
        // PROC <img src="images/smilies/icon_razz.gif" border="0" alt="" title=":P" class="inlineimg" />eupler_Graphe
        // QUI VA AJOUTER LES ARCS LUS DANS LA SIMPLE liste lstArcs
        // ? LA STRUCTURE Graphe monGraphe 
        private void Peupler_Graphe(AdjacencyGraph<string, Edge<string>> GrapheCourant)
        {
            foreach (Arc elem in lstArcs)
            {
                //Ajoute Sommets et Arcs au graphe
                arcGraphe = new QuickGraph.Edge<string>(elem.premNoeud, elem.deuxNoeud);
                GrapheCourant.AddVerticesAndEdge(arcGraphe);
                //Ajoute "cout arc" au Dictionnaire "coutArc" associe (ici des distances entre arcs)
                coutArc.Add(arcGraphe, elem.Longueur);
            }
        }
        // HANDLER : populator_NodeAdded
        private void populator_NodeAdded(object sender, QuickGraph.Glee.GleeVertexEventArgs<string> e)
        {
            // changer ici l'aparence des Vertex (noeuds  "graphiques" de ms GLEE) 
            Microsoft.Glee.Drawing.Style NoeudStyle = new Microsoft.Glee.Drawing.Style();
            NoeudStyle = Microsoft.Glee.Drawing.Style.Rounded;
            Microsoft.Glee.Drawing.Node noeudGlee = e.Node;
            string nomNoeud = e.Vertex;
            if (nomNoeud == "SOURCE")
            {
                noeudGlee.Attr.Color = Microsoft.Glee.Drawing.Color.Black;
                noeudGlee.Attr.Fillcolor = Microsoft.Glee.Drawing.Color.Yellow;
                noeudGlee.Attr.LineWidth = 2;
                noeudGlee.Attr.Shape = Microsoft.Glee.Drawing.Shape.Box;
                noeudGlee.Attr.AddStyle(NoeudStyle);
                noeudGlee.Attr.FontName = "VERDANA";
                noeudGlee.Attr.Fontcolor = Microsoft.Glee.Drawing.Color.Red;
                noeudGlee.Attr.FontName = "Arial";
                noeudGlee.Attr.Fontsize = 10;
                noeudGlee.Attr.Label = "Racine: " + nomNoeud;
            }
            else
            {
                noeudGlee.Attr.Color = Microsoft.Glee.Drawing.Color.Black;
                noeudGlee.Attr.Fillcolor = Microsoft.Glee.Drawing.Color.GreenYellow;
                noeudGlee.Attr.LineWidth = 1;
                noeudGlee.Attr.Shape = Microsoft.Glee.Drawing.Shape.Box;
                noeudGlee.Attr.AddStyle(NoeudStyle);
                noeudGlee.Attr.Fontcolor = Microsoft.Glee.Drawing.Color.Black;
                noeudGlee.Attr.FontName = "Times New Roman";
                noeudGlee.Attr.Fontsize = 8;
                noeudGlee.Attr.Label = nomNoeud;
            }

        }
        // HANDLER : populator_EdgeAdded
        private void populator_EdgeAdded(object sender, QuickGraph.Glee.GleeEdgeEventArgs<string, Edge<string>> e)
        {
            // changer ici l'aparence des Edges (arcs "graphiques" de ms GLEE) 
            // du premier arc au dernier
            int numOrderVisite;
            Microsoft.Glee.Drawing.Style arcStyle;
            Microsoft.Glee.Drawing.Edge arcGlee = e.GEdge;

            arcGlee.Attr.Color = Microsoft.Glee.Drawing.Color.Blue;
            arcGlee.Attr.LineWidth = 1;
            arcStyle = Microsoft.Glee.Drawing.Style.Dashed;
            arcGlee.Attr.AddStyle(arcStyle);
            arcGlee.Attr.FontName = "Verdana";
            arcGlee.Attr.Fontsize = 8;
            arcGlee.Attr.Fontcolor = Microsoft.Glee.Drawing.Color.Red;
            arcGlee.Attr.Label = "non visite";
            //si  un arc dans  liste de predecessorObserverArc 
            //changer sa couleur("arc dans arbre BFS explore")

            if (predecessorObserverArc.Edges.Contains(e.Edge))
            {
                numOrderVisite = predecessorObserverArc.Edges.IndexOf(e.Edge) + 1;
                arcGlee.Attr.Color = Microsoft.Glee.Drawing.Color.Black;
                arcGlee.Attr.ClearStyles();
                arcStyle = Microsoft.Glee.Drawing.Style.Solid;
                arcGlee.Attr.AddStyle(arcStyle);
                arcGlee.Attr.LineWidth = 3;
                double cout = 0.0;
                coutArc.TryGetValue(e.Edge, out cout);
                double totalDist = 0.0;
                distObserver.Distances.TryGetValue(e.Edge.Target, out totalDist);
                arcGlee.Attr.Label = "cout arc :" + cout.ToString() + Environment.NewLine;
                arcGlee.Attr.Label = arcGlee.Attr.Label + " ordre visite:" + numOrderVisite.ToString() +
                " distance :" + totalDist.ToString();
            }

        }
        //PROC  : AfficheCheminGraphVisite 
        //LISTE LES ARCS VISITES DANS UN TEXTBOX
        private void AfficheCheminGraphVisite()
        {
            //comptage des  noeuds et arcs du "graphe visite" 
            this.textBox1.ForeColor = System.Drawing.Color.DarkOliveGreen;
            this.textBox1.Font = new Font("Courier New", 10, FontStyle.Bold);
            this.textBox1.Text = "nombre Noeuds :" + (predecessorObserverSommet.VertexPredecessors.Count + 1).ToString() + Environment.NewLine;
            this.textBox1.Text = textBox1.Text + "nombre Arcs :" + (predecessorObserverArc.Edges.Count).ToString() + Environment.NewLine;
            //Affiche  Liste  des "arcs visites ordonnes"
            foreach (QuickGraph.Edge<string> cheminArc in predecessorObserverArc.Edges)
            {
                double totalDist = 0.0;
                distObserver.Distances.TryGetValue(cheminArc.Target, out totalDist);
                this.textBox1.Text = textBox1.Text + cheminArc.Source + " -" + cheminArc.Target + " - " + totalDist.ToString() + Environment.NewLine;
            }

        }

        //PROC  : LitFichierGraphe
        //LIT LE FICHIER TEXTE ET CHARGE DONNEES GRAPHE
        private void LitFichierGraphe(string nomFichierArcs, Arc monArc, List<Arc> lstArcs)
        {
            Microsoft.VisualBasic.FileIO.TextFieldParser MyReader = new
            Microsoft.VisualBasic.FileIO.TextFieldParser(nomFichierArcs);
            try
            {
                MyReader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                MyReader.SetDelimiters(";");
                string[] currentRow;
                while (!MyReader.EndOfData)
                {
                    try
                    {
                        currentRow = MyReader.ReadFields();
                        monArc = new Arc();
                        for (int i = 0; i < currentRow.Count(); i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    monArc.premNoeud = currentRow[i];
                                    break;
                                case 1:
                                    monArc.deuxNoeud = currentRow[i];
                                    break;
                                case 2:
                                    monArc.Longueur = double.Parse(currentRow[i]);
                                    break;
                                default:
                                    break;
                            }
                            lstArcs.Add(monArc);
                        }
                    }

                    catch (Microsoft.VisualBasic.FileIO.MalformedLineException ex)
                    {
                        MessageBox.Show("Line " + ex.Message + "is not valid and will be skipped.");
                    }
                }
            }
            finally
            {
                if (MyReader != null)
                    ((IDisposable)MyReader).Dispose();
            }
        }
    }
}
 
//
// CLASSE ARC MAPPE SUR LE FICHIER VILLES.TXT
// POUR LIRE ET STOCKER LES DONNEES DANS UNE LISTE 
// AVANT DE LES LIRE DANS L'API QuickGraph CAR ELLE 
// NE DISPOSE PAS DE METHODE DE LECTURE ET ECRITURE DIRECTE 
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
 
namespace WindowsFormsApplication1
{
    public class Arc
    {
        public String premNoeud;
        public String deuxNoeud;
        public Double Longueur;
        public Arc()
        {
            this.premNoeud = "";
            this.deuxNoeud = "";
            this.Longueur = 0.0;
        }
    }
}