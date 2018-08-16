using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace Pseudoman
{

    public partial class Form1 : Form
    {
        #region Init of variables

        /// <summary>
        /// Game mode.
        /// </summary>
        public bool isMultiplayer = false, isBonus = false, isClient = true, canContinue = false;

        /// <summary>
        /// Main direction key.
        /// </summary>
        Keys[] desiredKey = new Keys[5];

        /// <summary>
        /// If desiredKey is not pressed, direction changes in this direction.
        /// </summary>
        Keys[] nextKey = new Keys[5];

        /// <summary>
        /// Direction of characters momentum, character moves in this direction, if all other direcion keys fail.
        /// </summary>
        Keys[] momentum = new Keys[5];

        /// <summary>
        /// Location of every monster in the game.
        /// </summary>
        int[,] loc = new int[6, 2];

        /// <summary>
        /// Default position of every monster in the game.
        /// </summary>
        int[,] defpos = new int[6, 2];

        /// <summary>
        /// Array containing all the ghosts.
        /// </summary>
        PictureBox[] ghosts = new PictureBox[4];

        /// <summary>
        /// Second character for multiplayer.
        /// </summary>
        PictureBox papa;

        /// <summary>
        /// Array as big as map containing all special types of pellets.
        /// </summary>
        PictureBox[,] pellet;

        /// <summary>
        /// Number of map pixels every character moves for.
        /// </summary>
        int basicdelta = 8;

        /// <summary>
        /// Number of lives main character Pseudu starts with.
        /// </summary>
        int numberOfLives = 3;

        /// <summary>
        /// Number of lives second character in multiplayer starts with.
        /// </summary>
        int numberOfPapaLives = 3;

        /// <summary>
        /// Half the size of characters, used for center their image for their location.
        /// </summary>
        int centering = 12;

        /// <summary>
        /// Game map.
        /// </summary>
        int[][] map;

        /// <summary>
        /// Another thread going along with main form thread.
        /// </summary>
        Thread thread, server;

        /// <summary>
        /// Time the ghosts are edible for.
        /// </summary>
        int edible;

        /// <summary>
        /// Basic time after eating Power Pellet, the ghosts will be edible for.
        /// </summary>
        int edibleTime = 30;

        /// <summary>
        /// Image of one of the characters.
        /// </summary>
        Image red, pink, aqua, orange, blue, yellow;

        /// <summary>
        /// Image of the map.
        /// </summary>
        Image map0, map1, map2, map3;

        /// <summary>
        /// Number of pellets currenty left in the map.
        /// </summary>
        int pelletsCount;

        /// <summary>
        /// Level number.
        /// </summary>
        int level;

        /// <summary>
        /// Labels used in multiplayer or bonus mode.
        /// </summary>
        Label label2, label5, label6;

        /// <summary>
        /// Pictureboxes used in bonus mode.
        /// </summary>
        PictureBox pictureBox2, pictureBox3, pictureBox4, pictureBox5;

        /// <summary>
        /// Images used in bonus mode.
        /// </summary>
        Image[] pseuImages;

        /// <summary>
        /// Character score.
        /// </summary>
        int pseuScore = 0, papaScore = 0;

        /// <summary>
        /// Random generator.
        /// </summary>
        Random rnd = new Random();

        /// <summary>
        /// Array of TcpListeners used in bonus mode.
        /// </summary>
        public TcpListener[] servers;

        /// <summary>
        /// Array of TcpClients used in bonus mode if user is server.
        /// </summary>
        public TcpClient[] clients;

        /// <summary>
        /// Array of NetworkStreams corresponding to servers/clients.
        /// </summary>
        public NetworkStream[] streams;

        /// <summary>
        /// Number of port client is assigned to.
        /// </summary>
        public int clientPort;

        bool endServer = false;


        /// <summary>
        /// Delegate used in bonus mode to move every character.
        /// </summary>
        /// <param name="i">Character Id.</param>
        /// <param name="b">Is there a collision between main character and ghost?</param>
        delegate void MoveChar(int i, out bool b);

        #endregion

        #region Form thread
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Realizes what it has to do, singleplayer, multiplayer or bonus mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {


            string[] mapInput = new string[260];

            thread = new Thread(GameLogic);

            ghosts[0] = gho1;
            ghosts[1] = gho2;
            ghosts[2] = gho3;
            ghosts[3] = gho4;

            red = gho1.Image;
            pink = gho2.Image;
            aqua = gho3.Image;
            orange = gho4.Image;
            blue = Image.FromFile("../../../images/my_ghosts/ghovul.png");


            mapInput = new string[260];
            map = new int[260][];


            int i = 0;

            map0 = Image.FromFile("../../../images/map/map_img_0.png");
            level = 1;

            StreamReader reader = new StreamReader("../../../images/map/map_0.txt");
            while (reader.Peek() != -1)
            {

                mapInput[i] = reader.ReadLine();
                map[i] = new int[mapInput[i].Length];
                for (int j = 0; j < mapInput[i].Length; j++)
                {
                    map[i][j] = int.Parse(mapInput[i][j].ToString());
                }
                i++;
            }
            reader.Close();


            pellet = new PictureBox[i - 1, map[0].Length];

            SetPellets();

            if (isMultiplayer)
            {
                loc[5, 0] = defpos[5, 0] = defpos[0, 0] - 16;
                loc[5, 1] = defpos[5, 1] = defpos[0, 1];

                Invoke((Action)delegate { this.Controls.Remove(pellet[loc[5, 1], loc[5, 0]]); });
                Invoke((Action)delegate { this.Controls.Remove(pellet[loc[5, 1], loc[5, 0] + 8]); });


                pellet[loc[5, 1], loc[5, 0]] = null;
                pellet[loc[5, 1], loc[5, 0] + 8] = null;

                papa = new PictureBox();
                papa.Size = new Size(25, 25);
                papa.Image = Image.FromFile("../../../images/Papa_pseudu.png");
                papa.Location = new Point(pseu.Location.X - 16, pseu.Location.Y);
                papa.BackColor = Color.Transparent;

                this.Controls.Add(papa);

                PictureBox papaDown = new PictureBox();
                papaDown.Location = new Point(421, 506);
                papaDown.Size = new Size(25, 25);
                papaDown.Image = papa.Image;
                papaDown.BackColor = Color.Transparent;

                this.Controls.Add(papaDown);

                label2 = new Label();
                label2.Size = new Size(25, 25);
                label2.Location = new Point(390, 506);
                label2.Text = numberOfPapaLives.ToString();
                label2.BackColor = Color.Black;
                label2.ForeColor = Color.White;
                label2.Font = new Font(label1.Font.FontFamily, 12.0f);

                this.Controls.Add(label2);

                label5 = new Label();
                label5.Location = new Point(342, 506);
                label5.Text = 0.ToString();
                label5.BackColor = Color.Black;
                label5.ForeColor = Color.White;
                label5.Font = new Font(label1.Font.FontFamily, 12.0f);

                this.Controls.Add(label5);

                map[loc[5, 1]][loc[5, 0]] = 5;
            }


            if (isBonus)
            {

                yellow = Image.FromFile("../../../images/my_ghosts/gho5.png");
                pseuImages = new Image[5];

                /*
                thread = new Thread(BonusMode);

                yellow = Image.FromFile("../../../images/my_ghosts/gho5.png");

                server = (isClient) ? new Thread(DoClient) : new Thread(DoServer);

                thread.Start();
                if(clients[0]!=null)
                    server.Start();
                */

                thread = new Thread(Bonus);
            }

            thread.Start();
        }


        /// <summary>
        /// When arrow key is pressed, it will be considered as standart input key to fit the needs of KeyDown method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Up:
                case Keys.Left:
                case Keys.Right:
                    e.IsInputKey = true;
                    break;
            }
        }

        /// <summary>
        /// When key is pressed, desiredKey is changed to pressed key. Only controls of the game can be considered as desiredKey.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            int i = (!isBonus || !isClient) ? 0 : clientPort - 1300;



            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Up:
                case Keys.Right:
                case Keys.Left:
                    desiredKey[i] = e.KeyCode;
                    break;
                case Keys.W: if (isMultiplayer) desiredKey[1] = Keys.Up; break;
                case Keys.A: if (isMultiplayer) desiredKey[1] = Keys.Left; break;
                case Keys.S: if (isMultiplayer) desiredKey[1] = Keys.Down; break;
                case Keys.D: if (isMultiplayer) desiredKey[1] = Keys.Right; break;
            }
        }


        /// <summary>
        /// If this form is closed, all threads need to be aborted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            thread.Abort();
            if (server != null) server.Abort();
        }
        #endregion

        #region Single/multiplayer methods
        /// <summary>
        /// Main game logic for single and multiplayer.
        /// </summary>
        public void GameLogic()
        {
            edible = 0;

            Init();

            //Infinite cycle until real control of the game is pressed.
            while (desiredKey[0] == default(Keys) && desiredKey[1] == default(Keys)) { }
            nextKey[0] = desiredKey[0];
            nextKey[1] = desiredKey[1];

            while (numberOfLives > 0 || (isMultiplayer && numberOfPapaLives >= 0))
            {
                bool pacInCollision = false;

                //Main character(s) movement------------------------------------------

                if (numberOfLives > 0)
                    MoveUser(0, out pacInCollision);

                if (isMultiplayer && numberOfPapaLives > 0)
                {
                    MoveUser(5, out pacInCollision);
                }

                if (pacInCollision)
                {
                    if (Collision())
                    {
                        Thread.Sleep(250);
                        continue;
                    }
                }

                //Ghosts movement--------------------------------------
                for (int i = 0; i < 4; i++)
                {
                    MoveAI(i, out bool contact);

                    if (contact) { pacInCollision = true; }
                }

                if (pacInCollision)
                {
                    Collision();
                }

                if (pelletsCount == 0)
                {
                    NextLevel();
                }

                if (edible > 0)
                {
                    edible--;
                    if (edible == 0) MakeGhostsEatersAgain();
                }


                Thread.Sleep(250);
            }
        }

        /// <summary>
        /// Makes default initialization.
        /// </summary>
        private void Init()
        {
            Point point;
            if (numberOfLives > 0)
            {
                point = new Point(2 * defpos[0, 0] - centering, 2 * defpos[0, 1] - centering);
                Invoke((Action)delegate { pseu.Location = point; });
            }

            point = new Point(2 * defpos[1, 0] - centering, 2 * defpos[1, 1] - centering);
            Invoke((Action)delegate { gho1.Location = point; });

            point = new Point(2 * defpos[2, 0] - centering, 2 * defpos[2, 1] - centering);
            Invoke((Action)delegate { gho2.Location = point; });

            point = new Point(2 * defpos[3, 0] - centering, 2 * defpos[3, 1] - centering);
            Invoke((Action)delegate { gho3.Location = point; });

            point = new Point(2 * defpos[4, 0] - centering, 2 * defpos[4, 1] - centering);
            Invoke((Action)delegate { gho4.Location = point; });

            if (isMultiplayer && numberOfPapaLives > 0)
            {
                point = new Point(2 * defpos[5, 0] - centering, 2 * defpos[5, 1] - centering);
                Invoke((Action)delegate { papa.Location = point; });
            }

            DeleteCurrentPositions();

            for (int i = 0; i < 6; i++)
            {
                loc[i, 0] = defpos[i, 0];
                loc[i, 1] = defpos[i, 1];
                map[loc[i, 1]][loc[i, 0]] = 6;
            }
            if (numberOfLives > 0 && !isBonus) map[loc[0, 1]][loc[0, 0]] = 5;
            if (isMultiplayer && numberOfPapaLives > 0)
            {

                loc[5, 0] = defpos[5, 0];
                loc[5, 1] = defpos[5, 1];
                map[loc[5, 1]][loc[5, 0]] = 5;
            }
            if (numberOfLives == 0) Kill(0);
            if (numberOfPapaLives == 0) Kill(5);

            edible = 0;
            if (label1.Text[0] == 'R')
                MakeGhostsEatersAgain();

            if (isMultiplayer)
                if (numberOfLives == 0 && numberOfPapaLives == 0)
                    return;


        }

        /// <summary>
        /// When main character runs out of lives in multiplayer mode, this method should be called to get rid of him in game map.
        /// </summary>
        /// <param name="i">Character id.</param>
        private void Kill(int i)
        {
            map[loc[i, 1]][loc[i, 0]] = 1;
            loc[i, 0] = 0;
            loc[i, 1] = 1;
        }

        /// <summary>
        /// Sets pelets, default positions of characters on their place by map codes
        /// </summary>
        private void SetPellets()
        {
            int ii = 1;
            for (int i = 0; i < map.Length; i++)
            {
                for (int j = 0; j < map[i].Length; j++)
                {
                    switch (map[i][j])
                    {
                        case 2: pellet[i, j] = MakePellet(2 * j, 2 * i); pelletsCount++; break;
                        case 3: pellet[i, j] = MakePowerPellet(2 * j, 2 * i); break;
                        case 5:
                            defpos[0, 0] = j;
                            defpos[0, 1] = i;
                            break;
                        case 6:
                            defpos[ii, 0] = j;
                            defpos[ii, 1] = i;
                            ii++;
                            break;
                        case 7: pellet[i, j] = MakeTeleport(2 * j, 2 * i); break;
                    }
                }
            }
        }

        /// <summary>
        /// Makes one pellet and puts it on form.
        /// </summary>
        /// <param name="x">X coordinate on map.</param>
        /// <param name="y">Y coordinate on map.</param>
        /// <returns></returns>
        private PictureBox MakePellet(int x, int y)
        {
            PictureBox pellet = new PictureBox();
            pellet.BackColor = Color.Yellow;
            pellet.Size = new Size(5, 5);
            pellet.Location = new Point(x, y);
            if (InvokeRequired) { Invoke((Action)delegate { this.Controls.Add(pellet); }); }
            else { this.Controls.Add(pellet); }
            return pellet;
        }

        /// <summary>
        /// Makes one Power pellet and puts in on form
        /// </summary>
        /// <param name="x">X coordinate on map.</param>
        /// <param name="y">Y coordinate on map.</param>
        /// <returns></returns>
        private PictureBox MakePowerPellet(int x, int y)
        {
            PictureBox pPellet = new PictureBox();
            pPellet.BackColor = Color.Yellow;
            pPellet.Size = new Size(9, 9);
            pPellet.Location = new Point(x - 2, y - 2);
            if (InvokeRequired) { Invoke((Action)delegate { this.Controls.Add(pPellet); }); }
            else { this.Controls.Add(pPellet); }
            return pPellet;
        }

        /// <summary>
        /// Makes teleport.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private PictureBox MakeTeleport(int x, int y)
        {
            PictureBox teleport = new PictureBox();
            teleport.Tag = "teleport";
            return teleport;
        }

        /// <summary>
        /// Looks if character can be moved in direction (deltax,deltay).
        /// </summary>
        /// <param name="x">X coordinate of charcter.</param>
        /// <param name="deltax">Direction of movement in x axis.</param>
        /// <param name="y">Y coordinate of charcter.</param>
        /// <param name="deltay">Direction of movement in y axis.</param>
        /// <param name="contact">Was there a contact between user character and ghost?</param>
        /// <returns>Returns if can be object moved in direction.</returns>
        private bool CanBeMoved(int x, int deltax, int y, int deltay, out bool contact)
        {
            Label playerScore = (loc[0, 1] == y && loc[0, 0] == x) ? label4 : label5;

            if (isBonus)
            {
                playerScore = (loc[0, 1] == y && loc[0, 0] == x) ? label1 :
                    (loc[1, 1] == y && loc[1, 0] == x) ? label2 :
                    (loc[2, 1] == y && loc[2, 0] == x) ? label4 :
                    (loc[3, 1] == y && loc[3, 0] == x) ? label5 : label6;
            }

            contact = false;
            int h1, h2;

            if (y + deltay < 0 || x + deltax < 0 || y + deltay >= map.Length || x + deltax >= map[0].Length) return false;

            //checking for move
            for (int i = 1; i <= basicdelta; i++)
            {
                h1 = y + (i * deltay) / basicdelta;
                h2 = x + (i * deltax) / basicdelta;

                if (map[y][x] == 5)
                {
                    switch (map[h1][h2])
                    {

                        case 2: EatPellet(h1, h2, playerScore); break;
                        case 3: EatPowerPellet(h1, h2, playerScore); break;
                        case 4: case 1: break;
                        case 6: contact = true; break;
                        case 7: return true;
                        default: return false;
                    }
                }
                else
                {
                    switch (map[h1][h2])
                    {
                        case 2: case 3: case 4: case 1: break;
                        case 5: contact = true; break;
                        case 7: return true;
                        default: return false;
                    }
                }
            }

            //checking for further contact
            int plusx, minusx, plusy, minusy;
            for (int i = 1; i <= basicdelta; i++)
            {
                plusx = (deltay + i != 0) ? y + deltay + i : 0;
                minusx = (deltay - i != 0 && y + deltay - i > 0) ? y + deltay - i : 0;
                plusy = (deltax + i != 0 && x + deltax + i < map[0].Length) ? x + deltax + i : 0;
                minusy = (deltax - i != 0 && x + deltax - i > 0) ? x + deltax - i : 0;

                if (map[y][x] == 5)
                {
                    if (map[plusx][x + deltax] == 6 ||
                        map[y + deltay][minusy] == 6 ||
                        map[minusx][x + deltax] == 6 ||
                        map[y + deltay][plusy] == 6)
                    {
                        contact = true;
                    }
                    else if (map[plusx][x + deltax] == 5 ||
                             map[y + deltay][minusy] == 5 ||
                             map[minusx][x + deltax] == 5 ||
                             map[y + deltay][plusy] == 5)
                    {
                        return false;
                    }
                }
                else if (map[plusx][x + deltax] == 5 ||
                    map[y + deltay][minusy] == 5 ||
                    map[minusx][x + deltax] == 5 ||
                    map[y + deltay][plusy] == 5)
                {
                    contact = true;
                }
                else if (map[plusx][x + deltax] == 6 ||
                    map[y + deltay][minusy] == 6 ||
                    map[minusx][x + deltax] == 6 ||
                    map[y + deltay][plusy] == 6)
                {
                    return false;
                }

            }

            return true;
        }

        /// <summary>
        /// Moves character in direction (deltax,deltay).
        /// </summary>
        /// <param name="x">X coordinate of character.</param>
        /// <param name="deltax">Direction of movement in x axis.</param>
        /// <param name="y">Y coordinate of character.</param>
        /// <param name="deltay">Direction of movement in y axis.</param>
        /// <param name="i">Index of character to be moved.</param>
        private void MoveThem(int x, int deltax, int y, int deltay, int i)
        {
            int id = (map[y][x] == 5) ? 5 : 6;
            if (deltay != 0 || deltax != 0)
            {
                map[y + deltay][x + deltax] = id;
                if (pellet[y, x] == null) map[y][x] = 1;
                else if (pellet[y, x].Width == 5) map[y][x] = 2;
                else if ((string)pellet[y, x].Tag == "teleport") map[y][x] = 7;
                else map[y][x] = 3;
            }
            if (pellet[y + deltay, x + deltax] != null &&
                (string)pellet[y + deltay, x + deltax].Tag == "teleport")
                Teleport(i, y + deltay, x + deltax);
            else
            {
                Point point = new Point(2 * (loc[i, 0] + deltax) - centering, 2 * (loc[i, 1] + deltay) - centering);

                loc[i, 0] += deltax;
                loc[i, 1] += deltay;
                if (i == 0)
                {
                    Invoke((Action)delegate { pseu.Location = point; });
                }
                else if (i == 5)
                {
                    Invoke((Action)delegate { papa.Location = point; });
                }
                else
                {
                    Invoke((Action)delegate { ghosts[i - 1].Location = point; });
                }
            }
        }

        /// <summary>
        /// Deletes classic pellet from map, adds to score of player
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="playerScore"></param>
        private void EatPellet(int i, int j, Label playerScore)
        {
            map[i][j] = 1;
            Invoke((Action)delegate { this.Controls.Remove(pellet[i, j]); });
            pellet[i, j] = null;
            pelletsCount--;
            Invoke((Action)delegate { playerScore.Text = (int.Parse(playerScore.Text) + 20).ToString(); });
        }

        /// <summary>
        /// Deletes Power pellet from map, adds to score, makes ghosts edible.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="playerScore"></param>
        private void EatPowerPellet(int i, int j, Label playerScore)
        {
            map[i][j] = 1;
            Invoke((Action)delegate { this.Controls.Remove(pellet[i, j]); });
            pellet[i, j] = null;
            MakeGhostsEdible();
            Invoke((Action)delegate { playerScore.Text = (int.Parse(playerScore.Text) + 50).ToString(); });

        }

        /// <summary>
        /// Starts a timer, while this timer is active, ghosts do not cause harm.
        /// </summary>
        private void MakeGhostsEdible()
        {
            edible += edibleTime;
            Invoke((Action)delegate
            {
                for (int i = 0; i < 4; i++)
                {
                    ghosts[i].Image = blue;
                }
            });

        }

        /// <summary>
        /// Colors ghosts back to their default color.
        /// </summary>
        private void MakeGhostsEatersAgain()
        {
            if (isBonus && map[loc[0, 1]][loc[0, 0]] != 5)
            {
                Invoke((Action)delegate
                {
                    pseu.Image = yellow;
                });
            }
            for (int i = 0; i < 4; i++)
            {
                if (map[loc[i + 1, 1]][loc[i + 1, 0]] != 5)
                {
                    switch (i)
                    {
                        case 0:
                            Invoke((Action)delegate
                            {
                                gho1.Image = red;
                            });
                            break;
                        case 1:
                            Invoke((Action)delegate
                            {
                                gho2.Image = pink;
                            });
                            break;
                        case 2:
                            Invoke((Action)delegate
                            {
                                gho3.Image = aqua;
                            });
                            break;
                        case 3:
                            Invoke((Action)delegate
                            {
                                gho4.Image = orange;
                            });
                            break;
                    }
                }
            }
            /*Invoke((Action)delegate
            {
                gho1.Image = red;
                gho2.Image = pink;
                gho3.Image = aqua;
                gho4.Image = orange;
            });*/

        }

        /// <summary>
        /// Teleports character on the other side of map.
        /// </summary>
        /// <param name="i">Id of character (0 - main, 1-4 - ghosts, 5 - multiplayer second).</param>
        /// <param name="x">X coordinate on map.</param>
        /// <param name="y">Y coordinate on map.</param>
        private void Teleport(int i, int x, int y)
        {
            int id = (i == 0) ? 5 : 6;
            map[x][y] = 7;
            if (y == 6)
            {
                map[x][map[0].Length - 6] = id;
                loc[i, 0] = map[0].Length - 6;
            }
            else
            {
                map[x][6] = id;
                loc[i, 0] = 6;
            }


            Point point = new Point(2 * loc[i, 0] - centering, 2 * loc[i, 1] - centering);

            if (i == 0)
            {
                Invoke((Action)delegate { pseu.Location = point; });
            }
            else if (i == 5)
            {
                Invoke((Action)delegate { papa.Location = point; });
            }
            else
            {
                Invoke((Action)delegate { ghosts[i - 1].Location = point; });
            }
        }

        /// <summary>
        /// Delets current map position according to what should be there.
        /// </summary>
        private void DeleteCurrentPositions()
        {
            for (int i = 0; i < 6; i++)
            {
                int x = loc[i, 1];
                int y = loc[i, 0];

                if (pellet[x, y] == null) map[x][y] = 1;
                else if (pellet[x, y].Width == 5) map[x][y] = 2;
                else if ((string)pellet[x, y].Tag == "teleport") map[x][y] = 7;
                else map[x][y] = 3;
            }
        }

        /// <summary>
        /// From position set by parameters finds the closest path to main character.
        /// </summary>
        /// <param name="x">X coordinate on map.</param>
        /// <param name="y">Y coordinate on map.</param>
        /// <returns></returns>
        private Point Bfs(int x, int y)
        {
            Point start = new Point(x, y);
            Point p;
            Point[] newones = new Point[4];
            Queue<Point> points = new Queue<Point>();
            int[,] bfsmap = new int[map.Length, map[0].Length];
            Point[,] backtrack = new Point[map.Length, map[0].Length];
            int length = 1;
            bool foundpac = false;
            bool stop = false;
            int playerFound = 0;

            points.Enqueue(start);
            backtrack[start.Y, start.X] = start;
            bfsmap[start.Y, start.X] = length;

            while (points.Count != 0 && !foundpac)
            {
                p = points.Dequeue();
                switch (map[p.Y][p.X])
                {
                    case 5:
                        foundpac = true;
                        stop = true;
                        break;
                    case 0:
                        stop = true;
                        break;
                }

                if (!stop)
                {
                    newones[0] = (p.X - basicdelta > 0) ? new Point(p.X - basicdelta, p.Y) : default(Point);
                    newones[1] = (p.X + basicdelta < map[0].Length) ? new Point(p.X + basicdelta, p.Y) : default(Point);
                    newones[2] = (p.Y - basicdelta > 0) ? new Point(p.X, p.Y - basicdelta) : default(Point);
                    newones[3] = (p.Y + basicdelta < map.Length) ? new Point(p.X, p.Y + basicdelta) : default(Point);

                    for (int i = 0; i < 4; i++)
                    {
                        if (backtrack[newones[i].Y, newones[i].X] == default(Point))
                        {
                            backtrack[newones[i].Y, newones[i].X] = p;
                            bfsmap[newones[i].Y, newones[i].X] = bfsmap[p.Y, p.X] + 1;
                            points.Enqueue(newones[i]);
                        }
                    }
                }
                else stop = false;
            }

            playerFound = (backtrack[loc[0, 1], loc[0, 0]] != default(Point)) ? 0 : 5;

            if (foundpac)
            {
                p = backtrack[loc[playerFound, 1], loc[playerFound, 0]];
                while (backtrack[p.Y, p.X] != start && backtrack[p.Y, p.X] != default(Point))
                    p = backtrack[p.Y, p.X];
                return new Point(p.X, p.Y);
            }
            else return default(Point);
        }

        /// <summary>
        /// Chooses randomly one direction.
        /// </summary>
        /// <param name="deltax">Direction of movement in X axis.</param>
        /// <param name="deltay">Direction of movement in Y axis.</param>
        /// <param name="rnd">Random generator.</param>
        private void ChooseRandom(out int deltax, out int deltay, Random rnd)
        {
            int help;
            help = rnd.Next(4);
            deltax = 0;
            deltay = 0;

            switch (help)
            {
                case 0: deltax = 0; deltay = basicdelta; break;
                case 1: deltax = 0; deltay = -basicdelta; break;
                case 2: deltax = basicdelta; deltay = 0; break;
                case 3: deltax = -basicdelta; deltay = 0; break;
            }
        }

        /// <summary>
        /// Chooses randomly one direction according to how many directions the character can go.
        /// </summary>
        /// <param name="y">Y coordinate of the character.</param>
        /// <param name="x">X coordinate of the character.</param>
        /// <param name="deltax">Direction of movement in X axis.</param>
        /// <param name="deltay"><Direction of movement in Y axis./param>
        /// <param name="rnd">Random generator.</param>
        private void ChooseRandom(int y, int x, out int deltax, out int deltay, Random rnd)
        {
            deltax = 0;
            deltay = 0;
            int[] paths = new int[4];
            int a = CountPaths(y, x, paths);
            int help = rnd.Next(a);

            switch (paths[help])
            {
                case 0: deltay = 0; deltax = basicdelta; break;
                case 1: deltay = 0; deltax = -basicdelta; break;
                case 2: deltay = basicdelta; deltax = 0; break;
                case 3: deltay = -basicdelta; deltax = 0; break;
            }
        }

        /// <summary>
        /// Counts number of directions the character can go.
        /// </summary>
        /// <param name="x">X coordinate of the character.</param>
        /// <param name="y">Y coordinate of the character.</param>
        /// <param name="paths">An array containing directions the character can go (0 - right, 1 - left, 2 - down, 3 - right).</param>
        /// <returns> Number of directions the character can go.</returns>
        private int CountPaths(int x, int y, int[] paths)
        {
            int i = 0;
            if (x + 16 < map[0].Length)
                switch (map[y][x + 16])
                {
                    case 1: case 2: case 3: case 4: case 5: paths[i] = 0; i++; break;
                }
            if (x - 16 > 0)
                switch (map[y][x - 16])
                {
                    case 1: case 2: case 3: case 4: case 5: paths[i] = 1; i++; break;
                }
            switch (map[y + 16][x])
            {
                case 1: case 2: case 3: case 4: case 5: paths[i] = 2; i++; break;
            }
            if (y - 16 > 0)
                switch (map[y - 16][x])
                {
                    case 1: case 2: case 3: case 4: case 5: paths[i] = 3; i++; break;
                }
            return i;
        }

        /// <summary>
        /// Sets direction according to what key was pressed.
        /// </summary>
        /// <param name="key">Pressed key.</param>
        /// <param name="sofardeltax">Direction of movement in x axis before.</param>
        /// <param name="sofardeltay">Direction of movement in y axis before.</param>
        /// <param name="deltapacx">Final direction of movement in x axis.</param>
        /// <param name="deltapacy">Final direction of movement in y axis.</param>
        private void SetDelta(Keys key, int sofardeltax, int sofardeltay, out int deltapacx, out int deltapacy)
        {
            deltapacx = sofardeltax;
            deltapacy = sofardeltay;
            switch (key)
            {
                case Keys.Down:
                    deltapacx = 0;
                    deltapacy = basicdelta;
                    break;
                case Keys.Up:
                    deltapacx = 0;
                    deltapacy = -basicdelta;
                    break;
                case Keys.Right:
                    deltapacx = basicdelta;
                    deltapacy = 0;
                    break;
                case Keys.Left:
                    deltapacx = -basicdelta;
                    deltapacy = 0;
                    break;
            }
        }

        /// <summary>
        /// Moves one of the user-controlled characters.
        /// </summary>
        /// <param name="player">Id of character to be moved.</param>
        /// <param name="pacInCollision">Is main character in collision?</param>
        private void MoveUser(int player, out bool pacInCollision)
        {
            int deltapacx = 0;
            int deltapacy = 0;
            int keynum, i = 0;

            if (player == 0 || player == 5)
            {
                keynum = player / 5;
            }
            else
            {
                keynum = player;
            }

            if (desiredKey[keynum] != default(Keys))
            {
                SetDelta(desiredKey[keynum], deltapacx, deltapacy, out deltapacx, out deltapacy);

                if (CanBeMoved(loc[player, 0], deltapacx, loc[player, 1], deltapacy, out pacInCollision))
                {

                    MoveThem(loc[player, 0], deltapacx, loc[player, 1], deltapacy, player);

                    momentum[keynum] = desiredKey[keynum];
                    desiredKey[keynum] = default(Keys);
                    return;
                }
                else
                {
                    nextKey[keynum] = desiredKey[keynum];
                    desiredKey[keynum] = default(Keys);

                    SetDelta(momentum[keynum], deltapacx, deltapacy, out deltapacx, out deltapacy);
                    if (CanBeMoved(loc[player, 0], deltapacx, loc[player, 1], deltapacy, out pacInCollision))
                    {
                        MoveThem(loc[player, 0], deltapacx, loc[player, 1], deltapacy, player);
                        return;
                    }
                    else
                    {
                        momentum[keynum] = default(Keys);
                        return;
                    }
                }
            }

            if (nextKey[keynum] != default(Keys))
            {
                SetDelta(nextKey[keynum], deltapacx, deltapacy, out deltapacx, out deltapacy);
                if (CanBeMoved(loc[player, 0], deltapacx, loc[player, 1], deltapacy, out pacInCollision))
                {
                    MoveThem(loc[player, 0], deltapacx, loc[player, 1], deltapacy, player);

                    momentum[keynum] = nextKey[keynum];
                    nextKey[keynum] = default(Keys);
                    return;
                }
                else
                {
                    SetDelta(momentum[keynum], deltapacx, deltapacy, out deltapacx, out deltapacy);
                    if (CanBeMoved(loc[player, 0], deltapacx, loc[player, 1], deltapacy, out pacInCollision))
                    {
                        MoveThem(loc[player, 0], deltapacx, loc[player, 1], deltapacy, player);
                        return;
                    }
                    else
                    {
                        momentum[keynum] = default(Keys);
                        return;
                    }
                }
            }

            SetDelta(momentum[keynum], deltapacx, deltapacy, out deltapacx, out deltapacy);
            if (CanBeMoved(loc[player, 0], deltapacx, loc[player, 1], deltapacy, out pacInCollision))
            {
                MoveThem(loc[player, 0], deltapacx, loc[player, 1], deltapacy, player);
                //ReallyMoveThem(0, deltapacx, deltapacy);
                return;
            }
            else
            {
                momentum[keynum] = default(Keys);
                return;
            }
        }

        /// <summary>
        /// Moves one of the AI-controlled characters.
        /// </summary>
        /// <param name="i">Id of character to be moved.</param>
        /// <param name="pacInCollision">Is main character in collision?</param>
        private void MoveAI(int i, out bool pacInCollision)
        {
            Point bfs;
            int deltax, deltay;
            pacInCollision = false;

            if (isBonus) i--;
            bfs = Bfs(loc[i + 1, 0], loc[i + 1, 1]);

            //bfs = default(Point);

            if (bfs == default(Point))
            {
                ChooseRandom(loc[i + 1, 0], loc[i + 1, 1], out deltax, out deltay, rnd);
            }
            else
            {
                deltax = bfs.X - loc[i + 1, 0];
                deltay = bfs.Y - loc[i + 1, 1];
            }
            Point p = new Point(2 * (loc[i + 1, 0] + deltax) - centering, 2 * (loc[i + 1, 1] + deltay) - centering);

            //If ghost is edible, it is trying to move the other way from main character.
            if (edible > 0)
            {
                deltax *= -1;
                deltay *= -1;
            }

            if (CanBeMoved(loc[i + 1, 0], deltax, loc[i + 1, 1], deltay, out pacInCollision))
            {
                MoveThem(loc[i + 1, 0], deltax, loc[i + 1, 1], deltay, i + 1);

            }
            else
            {
                ChooseRandom(loc[i + 1, 0], loc[i + 1, 1], out deltax, out deltay, rnd);

                if (CanBeMoved(loc[i + 1, 0], deltax, loc[i + 1, 1], deltay, out pacInCollision))
                {

                    MoveThem(loc[i + 1, 0], deltax, loc[i + 1, 1], deltay, i + 1);

                }
            }
        }

        /// <summary>
        /// Finds which main character is in collision with which ghost and acts accordingly (character dies or eats ghost).
        /// </summary>
        /// <returns></returns>
        private bool Collision()
        {
            int collider = 0;
            int ii = 5;
            int numplayers = (isMultiplayer) ? 1 : 0;
            Label eaterScore;
            PictureBox ghost = null;

            //find which ghost is in collision
            for (int player = 0; player <= numplayers; player++)
            {
                if (FindCollision(player * 5, out int ghostNum)) { collider = player; ii = ghostNum; }
            }
            eaterScore = (collider == 0) ? label4 : label5;

            if (ii != 5) ghost = ghosts[--ii];

            //Eat or be eaten
            if (ghost == null) return false;
            if (ghost.Image == blue)
            {
                EatAGhost(ii, eaterScore);
            }
            else
            {
                if (isMultiplayer)
                {
                    if (collider == 0)
                    {
                        numberOfLives--;
                        if (numberOfLives == 0)
                        {
                            Invoke((Action)delegate { this.Controls.Remove(pseu); });
                        }
                        Invoke((Action)delegate { label1.Text = numberOfLives.ToString(); });
                    }
                    else
                    {
                        numberOfPapaLives--;
                        if (numberOfPapaLives == 0)
                        {
                            Invoke((Action)delegate { this.Controls.Remove(papa); });
                            map[defpos[5, 1]][defpos[5, 0]] = 1;
                        }
                        Invoke((Action)delegate { label2.Text = numberOfPapaLives.ToString(); });
                    }
                }
                else
                {
                    numberOfLives--;
                    if (numberOfLives >= 0)
                        Invoke((Action)delegate { label1.Text = numberOfLives.ToString(); });
                    else
                    {
                        Invoke((Action)delegate
                        {
                            thread.Abort();
                            Form1_FormClosed(null, null);
                        });
                    }
                }
                Init();

                //Infinite cycle until real control of the game is pressed.
                while (desiredKey[0] == default(Keys) && desiredKey[1] == default(Keys)) { }
                nextKey[0] = desiredKey[0];
                nextKey[1] = desiredKey[1];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds collision.
        /// </summary>
        /// <param name="ghost">Returns collision ghost.</param>
        /// <returns>Id of character (0 - main, 1 - second character for multiplayer).</returns>
        private bool FindCollision(int player, out int ghost)
        {
            ghost = 5;


            for (int i = 0; i <= 4; i++)
            {
                if (!isBonus && i == 0) continue;
                else if (isBonus && player == i) continue;
                if (loc[player, 0] - 8 <= loc[i, 0] && loc[player, 0] + 8 >= loc[i, 0] &&
                    loc[player, 1] - 8 <= loc[i, 1] && loc[player, 1] + 8 >= loc[i, 1])
                {
                    ghost = i;
                    return true;
                }
            }


            return false;
        }

        /// <summary>
        /// Sets ghost in collision to default position and default color.
        /// </summary>
        /// <param name="which">Id of ghost.</param>
        /// <param name="eaterScore">Label with score to be added bonus to.</param>
        private void EatAGhost(int which, Label eaterScore)
        {
            switch (which)
            {
                case 0: ghosts[which].Image = red; break;
                case 1: ghosts[which].Image = pink; break;
                case 2: ghosts[which].Image = aqua; break;
                case 3: ghosts[which].Image = orange; break;
                case 4: ghosts[which].Image = yellow; break;
            }

            int x = loc[which + 1, 1];
            int y = loc[which + 1, 0];

            if (pellet[x, y] == null)
            {
                map[x][y] = 1;
            }
            else if (pellet[x, y].Width == 5)
            {
                EatPellet(x, y, eaterScore);
            }
            else
            {
                EatPowerPellet(x, y, eaterScore);
            }

            loc[which + 1, 0] = defpos[which + 1, 0];
            loc[which + 1, 1] = defpos[which + 1, 1];

            Point p = new Point(2 * loc[which + 1, 0] - centering, 2 * loc[which + 1, 1] - centering);

            Invoke((Action)delegate
            {
                eaterScore.Text = (int.Parse(eaterScore.Text) + 200).ToString();
                ghosts[which].Location = p;
            });
        }

        /// <summary>
        /// Changes level to next level.
        /// </summary>
        private void NextLevel()
        {
            string[] mapInput = new string[260];

            bool loadImage = false;
            StreamReader reader = null;
            int i = 0;


            if (level / 4 == 0) loadImage = true;

            level++;
            switch (level % 4)
            {
                case 1:
                    this.BackgroundImage = map0;
                    reader = new StreamReader("../../../images/map/map_0.txt");
                    break;
                case 2:
                    if (loadImage) map1 = Image.FromFile("../../../images/map/map_img_1.png");
                    this.BackgroundImage = map1;
                    reader = new StreamReader("../../../images/map/map_1.txt");
                    break;
                case 3:
                    if (loadImage) map2 = Image.FromFile("../../../images/map/map_img_2.png");
                    this.BackgroundImage = map2;
                    reader = new StreamReader("../../../images/map/map_2.txt");
                    break;
                case 0:
                    if (loadImage) map3 = Image.FromFile("../../../images/map/map_img_3.png");
                    this.BackgroundImage = map3;
                    reader = new StreamReader("../../../images/map/map_3.txt");
                    break;
            }

            while (reader.Peek() != -1)
            {

                mapInput[i] = reader.ReadLine();
                map[i] = new int[mapInput[i].Length];
                for (int j = 0; j < mapInput[i].Length; j++)
                {
                    map[i][j] = int.Parse(mapInput[i][j].ToString());
                }
                i++;
            }

            pellet = new PictureBox[i - 1, map[0].Length];

            this.Invoke((Action)delegate { label3.Text = "LEVEL " + level; });

            SetPellets();

            Init();

            //Infinite cycle until real control of the game is pressed.
            while (desiredKey[0] == default(Keys) && desiredKey[1] == default(Keys)) { }
            nextKey[0] = desiredKey[0];
            nextKey[1] = desiredKey[1];
        }

        #endregion

        #region Bonus mode methods (contains also references to Single/multiplayer methods region)
        /// <summary>
        /// Main logic behind bonus mode.
        /// </summary>
        private void BonusMode()
        {
            ghosts = new PictureBox[5];
            ghosts[0] = gho1;
            ghosts[1] = gho2;
            ghosts[2] = gho3;
            ghosts[3] = gho4;
            ghosts[4] = pseu;

            bool pacInCollision = false, dead = false;
            bool[] controlled = new bool[5];
            Label[] scores;
            PictureBox[] characters;


            //INITIALIZATION


            Init();
            Invoke((Action)delegate { InitLabels(); });


            //Infinite cycle until real control of the game is pressed.
            while (desiredKey[0] == default(Keys)) { }

            for (int i = 0; i < 5; i++)
            {
                nextKey[i] = desiredKey[i];
            }

            //game movement
            int n = 0;

            MoveChar[] moves = new MoveChar[5];
            moves[0] = new MoveChar(MoveUser);
            controlled[0] = true;

            for (int i = 1; i <= 4; i++)
            {
                if (clients[n] != null || isClient)
                {
                    controlled[i] = true;
                    moves[i] = new MoveChar(MoveUser);
                    n++;
                }
                else
                {
                    controlled[i] = false;
                    moves[i] = new MoveChar(MoveAI);
                }
            }

            int[] prevPos = new int[2];
            //game logic
            for (int i = 0; i <= n; i++)
            {
                Invoke((Action)delegate { label3.Text = "ROUND " + i.ToString(); });

                while (!dead)
                {
                    canContinue = false;

                    moves[i](i, out pacInCollision);


                    if (pacInCollision)
                        if (BonusModeCollision(i, out dead))
                        {
                            if (dead)
                            {
                                Thread.Sleep(250);
                                continue;
                            }
                        }
                    pacInCollision = false;

                    for (int j = 0; j <= 4; j++)
                    {
                        if (j == i) continue;

                        if (!controlled[j])
                        {
                            prevPos[0] = loc[j, 0];
                            prevPos[1] = loc[j, 1];
                        }

                        moves[j](j, out bool contact);

                        if (!controlled[j])
                        {
                            if (prevPos[0] - loc[j, 0] == 0)
                            {
                                if (prevPos[1] - loc[j, 1] < 0) desiredKey[j] = Keys.Down;
                                else if (prevPos[1] - loc[j, 1] > 0) desiredKey[j] = Keys.Up;
                                else desiredKey[j] = Keys.None;
                            }
                            else if (prevPos[0] - loc[j, 0] < 0) desiredKey[j] = Keys.Right;
                            else desiredKey[j] = Keys.Left;
                        }

                        if (contact) pacInCollision = true;
                    }

                    if (pacInCollision) if (BonusModeCollision(i, out dead))

                            if (pelletsCount == 0)
                            {
                                dead = true;
                            }
                    Thread.Sleep(250);
                    if (clients[0] != null && !dead) while (!canContinue) { }
                }
            }
        }

        /// <summary>
        /// Dealing with collision in bonus mode; finds which ghost collided with main character and acts accordingly.
        /// </summary>
        /// <param name="chased">Id of chased player.</param>
        /// <param name="dead">Is chased player killed in collision?</param>
        /// <returns></returns>
        private bool BonusModeCollision(int chased, out bool dead)
        {
            int ii = 5;
            int numplayers = (isMultiplayer) ? 1 : 0;
            Label eaterScore;
            PictureBox ghost = null;

            //find which ghost is in collision

            FindCollision(chased, out ii);

            switch (chased)
            {
                case 0: eaterScore = label1; break;
                case 1: eaterScore = label2; break;
                case 2: eaterScore = label4; break;
                case 3: eaterScore = label5; break;
                case 4: eaterScore = label6; break;
                default: eaterScore = null; break;
            }

            ii = (ii == 0) ? 4 : ii - 1;

            ghost = ghosts[ii];

            //Eat or be eaten
            if (ghost == null)
            {
                dead = false;
                return false;
            }

            if (ghost.Image == blue)
            {
                EatAGhost(ii, eaterScore);
            }
            else
            {
                switch (ii)
                {
                    case 4: Invoke((Action)delegate { label1.Text = (int.Parse(label1.Text) + 1000).ToString(); }); break;
                    case 0: Invoke((Action)delegate { label2.Text = (int.Parse(label2.Text) + 1000).ToString(); }); break;
                    case 1: Invoke((Action)delegate { label4.Text = (int.Parse(label4.Text) + 1000).ToString(); }); break;
                    case 2: Invoke((Action)delegate { label5.Text = (int.Parse(label5.Text) + 1000).ToString(); }); break;
                    case 3: Invoke((Action)delegate { label6.Text = (int.Parse(label6.Text) + 1000).ToString(); }); break;
                }

                dead = true;
                return true;
            }

            dead = false;
            return false;
        }

        /// <summary>
        /// Initializes all labels needed in bonus mode.
        /// </summary>
        private void InitLabels()
        {
            label1.Location = new Point(31, 510);
            label1.Font = new Font(label1.Font.FontFamily, 8);
            label1.Text = 0.ToString();
            pictureBox1.BringToFront();
            label3.Font = new Font(label3.Font.FontFamily, 8);
            label3.Location = new Point(219, 510);

            pictureBox2 = new PictureBox();
            pictureBox2.Image = Image.FromFile("../../../images/Pseudu_red.png");
            pictureBox2.Size = new Size(25, 25);
            pictureBox2.Location = new Point(74, 506);
            pictureBox2.BackColor = Color.Transparent;
            this.Controls.Add(pictureBox2);
            pictureBox2.BringToFront();

            label2 = new Label();
            label2.Location = new Point(99, 510);
            label2.Text = 0.ToString();
            label2.BackColor = Color.Black;
            label2.ForeColor = Color.White;
            label2.Font = new Font(label1.Font.FontFamily, 8);
            this.Controls.Add(label2);

            pictureBox3 = new PictureBox();
            pictureBox3.Image = Image.FromFile("../../../images/Pseudu_pink.png");
            pictureBox3.Size = new Size(25, 25);
            pictureBox3.Location = new Point(145, 506);
            pictureBox3.BackColor = Color.Transparent;
            this.Controls.Add(pictureBox3);
            pictureBox3.BringToFront();


            label4 = new Label();
            label4.Location = new Point(170, 510);
            label4.Text = 0.ToString();
            label4.BackColor = Color.Black;
            label4.ForeColor = Color.White;
            label4.Font = new Font(label1.Font.FontFamily, 8);
            this.Controls.Add(label4);
            label4.Visible = true;
            label4.BringToFront();


            pictureBox4 = new PictureBox();
            pictureBox4.Image = Image.FromFile("../../../images/Pseudu_aqua.png");
            pictureBox4.Size = new Size(25, 25);
            pictureBox4.Location = new Point(306, 506);
            pictureBox4.BackColor = Color.Transparent;
            this.Controls.Add(pictureBox4);
            pictureBox4.BringToFront();

            label5 = new Label();
            label5.Location = new Point(331, 510);
            label5.Text = 0.ToString();
            label5.BackColor = Color.Black;
            label5.ForeColor = Color.White;
            label5.Font = new Font(label1.Font.FontFamily, 8);
            this.Controls.Add(label5);


            pictureBox5 = new PictureBox();
            pictureBox5.Image = Image.FromFile("../../../images/Pseudu_orange.png");
            pictureBox5.Size = new Size(25, 25);
            pictureBox5.Location = new Point(377, 506);
            pictureBox5.BackColor = Color.Transparent;
            this.Controls.Add(pictureBox5);
            pictureBox5.BringToFront();

            label6 = new Label();
            label6.Location = new Point(402, 510);
            label6.Text = 0.ToString();
            label6.BackColor = Color.Black;
            label6.ForeColor = Color.White;
            label6.Font = new Font(label1.Font.FontFamily, 8);
            this.Controls.Add(label6);
            label6.BringToFront();


            label4.BringToFront();
            label3.BringToFront();

            pseuImages[0] = pictureBox2.Image;
            pseuImages[1] = pictureBox3.Image;
            pseuImages[2] = pictureBox4.Image;
            pseuImages[3] = pictureBox5.Image;
            pseuImages[4] = pictureBox1.Image;
        }

        /// <summary>
        /// Infinite loop that waits for key to be pressed and evaluated by form thread.
        /// </summary>
        /// <param name="i"></param>
        private void ReadKey(int i)
        {
            while (desiredKey[i] == default(Keys)) { }
        }

        /// <summary>
        /// Initialization for bonus round revolving around certain player.
        /// </summary>
        /// <param name="player">Id of player that plays main character this round.</param>
        private void BonusRoundInit(int player)
        {
            int i = 0;
            string[] mapInput = new string[260];
            StreamReader reader = new StreamReader("../../../images/map/map_0.txt");
            while (reader.Peek() != -1)
            {

                mapInput[i] = reader.ReadLine();
                map[i] = new int[mapInput[i].Length];
                for (int j = 0; j < mapInput[i].Length; j++)
                {
                    map[i][j] = int.Parse(mapInput[i][j].ToString());
                }
                i++;
            }
            reader.Close();

            SetPellets();

            if (player != 0)
            {
                for (int j = 0; j <= 1; j++)
                {
                    i = defpos[player, j];
                    defpos[player, j] = defpos[0, j];
                    defpos[0, j] = i;
                }

                player--;

                Invoke((Action)delegate
                {
                    pseu.Image = yellow;
                    gho1.Image = red;
                    gho2.Image = pink;
                    gho3.Image = aqua;
                    gho4.Image = orange;

                    ghosts[player].Image = pseuImages[player];
                });

                player++;

            }

            Init();

            map[loc[player, 1]][loc[player, 0]] = 5;
            MakeGhostsEatersAgain();


        }

        #endregion

        #region Servers
        private void DoServer()
        {
            while (!endServer)
            {
                var t1 = Task.Run(() =>
                SetDesKeyServer(1));

                var t2 = Task.Run(() =>
                SetDesKeyServer(2));

                var t3 = Task.Run(() =>
                SetDesKeyServer(3));

                var t4 = Task.Run(() =>
                SetDesKeyServer(4));

                t1.Wait();
                t2.Wait();
                t3.Wait();
                t4.Wait();

                for (int i = 0; i < 4; i++)
                {
                    if (clients[i] == null) break;
                    var writer = new BinaryWriter(streams[i]);
                    for (int j = 0; j < 5; j++)
                    {
                        int s = 0;
                        switch (desiredKey[j])
                        {
                            case Keys.Up: s = 101; break;
                            case Keys.Down: s = 102; break;
                            case Keys.Left: s = 103; break;
                            case Keys.Right: s = 104; break;
                            case Keys.None: s = 100; break;
                        }

                        writer.Write(s);
                    }
                }
                canContinue = true;
            }
        }

        private void SetDesKeyServer(int i)
        {
            if (clients[i - 1] == null) return;
            var reader = new BinaryReader(streams[i - 1]);
            bool ok = false;
            int num = 0;

            while (!ok)
            {
                num = reader.Read();
                if (num >= 100 && num < 105)
                    ok = true;
            }

            switch (num)
            {
                case 101: desiredKey[i] = Keys.Up; break;
                case 102: desiredKey[i] = Keys.Down; break;
                case 103: desiredKey[i] = Keys.Left; break;
                case 104: desiredKey[i] = Keys.Right; break;
                case 100: desiredKey[i] = default(Keys); break;
            }
        }

        private void DoClient()
        {
            while (desiredKey[clientPort - 1300] == default(Keys)) { }

            var writer = new BinaryWriter(streams[0]);
            var reader = new BinaryReader(streams[0]);
            int s = 0;

            while (!endServer)
            {
                switch (desiredKey[clientPort - 1300])
                {
                    case Keys.Up: s = 101; break;
                    case Keys.Down: s = 102; break;
                    case Keys.Left: s = 103; break;
                    case Keys.Right: s = 104; break;
                    case default(Keys): s = 100; break;
                }
                writer.Write(s);

                for (int i = 0; i < 5; i++)
                {
                    int k = reader.Read();
                    switch (k)
                    {
                        case 101: desiredKey[i] = Keys.Up; break;
                        case 102: desiredKey[i] = Keys.Down; break;
                        case 103: desiredKey[i] = Keys.Left; break;
                        case 104: desiredKey[i] = Keys.Right; break;
                        default: desiredKey[i] = default(Keys); break;
                    }
                }

                canContinue = true;
            }
        }
        #endregion

        #region Actual functioning code
        //TODO: CLEAR!!!


        //-----------------
        //JUST TRYING
        //-----------------
        #region Comm
        /*
    private void ServerSide()
    {

        //INITIALIZATION
        Invoke((Action)delegate { label3.Text = "ROUND 1"; });

        bool pacInCollision = false, dead;
        bool[] controlled = new bool[5];
        Label[] scores;
        PictureBox[] characters;
        int connected = 0;
        int[] prevPos = new int[2];
        int n = 0;


        ghosts = new PictureBox[5];
        ghosts[0] = gho1;
        ghosts[1] = gho2;
        ghosts[2] = gho3;
        ghosts[3] = gho4;
        ghosts[4] = pseu;

        Init();
        map[loc[0, 1]][loc[0, 0]] = 5;
        Invoke((Action)delegate { InitLabels(); });




        //GAME MOVEMENT

        MoveChar[] moves = new MoveChar[5];
        moves[0] = new MoveChar(MoveUser);
        controlled[0] = true;

        for (int i = 1; i <= 4; i++)
        {
            if (clients[n] != null || isClient)
            {
                controlled[i] = true;
                moves[i] = new MoveChar(MoveUser);
                n++;
            }
            else
            {
                controlled[i] = false;
                moves[i] = new MoveChar(MoveAI);
            }
        }


        //GAME LOGIC
        for (int i = 0; i <= n; i++)
        {
            Invoke((Action)delegate { label3.Text = "ROUND " + (i + 1).ToString(); });

            //Infinite cycle until real control of the game is pressed.
            ReadKey(0);

            for (int m = 0; m < 5; m++)
            {
                nextKey[m] = desiredKey[m];
            }

            dead = false;

            //GAME CYCLE
            while (!dead)
            {
                //SERVER 1 (listening)
                for (int l = 1; l <= 4; l++)
                {
                    SetDesKeyServer(l);
                }

                //SERVER 2 (sending char moves)
                for (int ii = 0; ii <= 4; ii++)
                {
                    if (clients[ii] == null) break;
                    var writer = new BinaryWriter(streams[ii]);
                    for (int j = 0; j <= n; j++)
                    {
                        int s = 0;
                        switch (desiredKey[j])
                        {
                            case Keys.Up: s = 101; break;
                            case Keys.Down: s = 102; break;
                            case Keys.Left: s = 103; break;
                            case Keys.Right: s = 104; break;
                            case Keys.None: s = 100; break;
                        }

                        writer.Write(s);
                    }
                }

                //MOVEPAC
                moves[i](i, out pacInCollision);


                if (pacInCollision)
                    if (BonusModeCollision(i, out dead))
                    {
                        if (dead)
                        {
                            Thread.Sleep(250);
                            continue;
                        }
                    }
                pacInCollision = false;

                //MOVEGHO
                for (int j = 0; j <= 4; j++)
                {
                    if (j == i) continue;

                    if (!controlled[j])
                    {
                        prevPos[0] = loc[j, 0];
                        prevPos[1] = loc[j, 1];
                    }

                    moves[j](j, out bool contact);

                    if (!controlled[j])
                    {
                        if (prevPos[0] - loc[j, 0] == 0)
                        {
                            if (prevPos[1] - loc[j, 1] < 0) desiredKey[j] = Keys.Down;
                            else if (prevPos[1] - loc[j, 1] > 0) desiredKey[j] = Keys.Up;
                            else desiredKey[j] = Keys.None;
                        }
                        else if (prevPos[0] - loc[j, 0] < 0) desiredKey[j] = Keys.Right;
                        else desiredKey[j] = Keys.Left;
                    }

                    if (contact) pacInCollision = true;
                }

                if (pacInCollision) if (BonusModeCollision(i, out dead)) ;

                if (pelletsCount == 0)
                {
                    dead = true;
                }

                //SERVER 3 (sending AI movement)
                for (int ii = 0; ii <= 4; ii++)
                {
                    if (clients[ii] == null) break;
                    var writer = new BinaryWriter(streams[ii]);
                    for (int j = n + 1; j < 5; j++)
                    {
                        int s = 0;
                        switch (desiredKey[j])
                        {
                            case Keys.Up: s = 101; break;
                            case Keys.Down: s = 102; break;
                            case Keys.Left: s = 103; break;
                            case Keys.Right: s = 104; break;
                            case Keys.None: s = 100; break;
                        }

                        writer.Write(s);
                    }
                }

                //POWER PELLET CONTROL
                if (edible > 0)
                {
                    edible--;
                    if (edible == 0) MakeGhostsEatersAgain();
                }

                //SLEEP
                Thread.Sleep(250);
            }

            //NEW LEVEL INIT
            if (i < 4)
                BonusRoundInit(i + 1);
        }
    }


    private void ClientSide()
    {
        //INITIALIZATION
        bool pacInCollision = false, dead = false;
        bool[] controlled = new bool[5];
        Label[] scores;
        PictureBox[] characters;
        int n = 0;
        MoveChar[] moves = new MoveChar[5];
        int[] prevPos = new int[2];
        var writer = new BinaryWriter(streams[0]);
        var reader = new BinaryReader(streams[0]);
        int s = 0;


        Invoke((Action)delegate { label3.Text = "ROUND 1"; });
        ghosts = new PictureBox[5];
        ghosts[0] = gho1;
        ghosts[1] = gho2;
        ghosts[2] = gho3;
        ghosts[3] = gho4;
        ghosts[4] = pseu;

        Init();
        map[loc[0, 1]][loc[0, 0]] = 5;
        Invoke((Action)delegate { InitLabels(); });

        for (int i = 0; i < 5; i++)
        {
            nextKey[i] = desiredKey[i];
        }

        //GAME MOVEMENT
        moves[0] = new MoveChar(MoveUser);
        controlled[0] = true;

        for (int i = 1; i <= 4; i++)
        {
            if (clients[n] != null || isClient)
            {
                controlled[i] = true;
                moves[i] = new MoveChar(MoveUser);
                n++;
            }
            else
            {
                controlled[i] = false;
                moves[i] = new MoveChar(MoveAI);
            }
        }

        //GAME LOGIC
        for (int i = 0; i <= n; i++)
        {
            Invoke((Action)delegate { label3.Text = "ROUND " + i.ToString(); });

            ReadKey(clientPort - 1300);

            dead = false;

            //GAME CYCLE
            while (!dead)
            {
                //CLIENT 1 (send move)
                switch (desiredKey[clientPort - 1300])
                {
                    case Keys.Up: s = 101; break;
                    case Keys.Down: s = 102; break;
                    case Keys.Left: s = 103; break;
                    case Keys.Right: s = 104; break;
                    case default(Keys): s = 100; break;
                }
                writer.Write(s);

                //CLIENT 2 (listen to all moves)
                int k;
                k = reader.Read();
                while (k == 0) { k = reader.Read(); }
                for (int ii = 0; ii < 5; ii++)
                {
                    switch (k)
                    {
                        case 101: desiredKey[ii] = Keys.Up; break;
                        case 102: desiredKey[ii] = Keys.Down; break;
                        case 103: desiredKey[ii] = Keys.Left; break;
                        case 104: desiredKey[ii] = Keys.Right; break;
                        case 100: desiredKey[ii] = default(Keys); break;
                        default: ii--; break;
                    }
                    k = reader.Read();
                }

                //MOVEPAC
                moves[i](i, out pacInCollision);


                if (pacInCollision)
                    if (BonusModeCollision(i, out dead))
                    {
                        if (dead)
                        {
                            Thread.Sleep(250);
                            continue;
                        }
                    }
                pacInCollision = false;

                //MOVEAI
                for (int j = 0; j <= 4; j++)
                {
                    if (j == i) continue;

                    moves[j](j, out bool contact);

                    if (contact) pacInCollision = true;
                }

                if (pacInCollision) if (BonusModeCollision(i, out dead)) ;

                if (pelletsCount == 0)
                {
                    dead = true;
                }


                //POWER PELLET CONTROL
                if (edible > 0)
                {
                    edible--;
                    if (edible == 0) MakeGhostsEatersAgain();
                }

                //SLEEP
                Thread.Sleep(250);
            }

            //NEW LEVEL
            BonusRoundInit(i + 1);
        }
    }
    */

        #endregion
        private void Bonus()
        {
            //INITIALIZATION
            Invoke((Action)delegate { label3.Text = "ROUND 1"; });

            bool pacInCollision = false, dead;
            bool[] controlled = new bool[5];
            Label[] scores;
            PictureBox[] characters;
            MoveChar[] moves = new MoveChar[5];
            int[] prevPos = new int[2];
            int n = 0;
            var writer = (isClient) ? new BinaryWriter(streams[0]) : null;
            var reader = (isClient) ? new BinaryReader(streams[0]) : null;
            int user = (isClient) ? clientPort - 1300 : 0;

            BonusInit();

            //GAME MOVEMENT
            moves[0] = new MoveChar(MoveUser);
            controlled[0] = true;

            for (int i = 1; i <= 4; i++)
            {
                if (clients[n] != null || isClient)
                {
                    controlled[i] = true;
                    moves[i] = new MoveChar(MoveUser);
                    n++;
                }
                else
                {
                    controlled[i] = false;
                    moves[i] = new MoveChar(MoveAI);
                }
            }

            //GAME LOGIC
            for (int i = 0; i <= n; i++)
            {
                Invoke((Action)delegate { label3.Text = "ROUND " + (i + 1).ToString(); });

                //Infinite cycle until real control of the game is pressed.
                ReadKey(user);

                for (int m = 0; m < 5; m++)
                {
                    nextKey[m] = desiredKey[m];
                }

                dead = false;

                //GAME CYCLE
                while (!dead)
                {
                    if (isClient)
                    {
                        SendMove(writer);
                        GetMoves(reader);
                    }
                    else
                    {
                        SetAllDesiredKeys();
                        SendUserMoves(writer, n);
                    }
                    
        #region Moving pseudu...
                    moves[i](i, out pacInCollision);


                    if (pacInCollision)
                        if (BonusModeCollision(i, out dead))
                        {
                            if (dead)
                            {
                                Thread.Sleep(250);
                                continue;
                            }
                        }
                    pacInCollision = false;

                    #endregion

        #region Moving ghosts...
                    for (int j = 0; j <= 4; j++)
                    {
                        if (j == i) continue;

                        if (!controlled[j] && !isClient)
                        {
                            prevPos[0] = loc[j, 0];
                            prevPos[1] = loc[j, 1];
                        }

                        moves[j](j, out bool contact);

                        if (!controlled[j] && !isClient)
                        {
                            if (prevPos[0] - loc[j, 0] == 0)
                            {
                                if (prevPos[1] - loc[j, 1] < 0) desiredKey[j] = Keys.Down;
                                else if (prevPos[1] - loc[j, 1] > 0) desiredKey[j] = Keys.Up;
                                else desiredKey[j] = Keys.None;
                            }
                            else if (prevPos[0] - loc[j, 0] < 0) desiredKey[j] = Keys.Right;
                            else desiredKey[j] = Keys.Left;
                        }

                        if (contact) pacInCollision = true;
                    }

                    if (pacInCollision) if (BonusModeCollision(i, out dead)) ;
                    #endregion

                    if (pelletsCount == 0)
                    {
                        dead = true;
                    }

                    if(!isClient)
                    {
                        SendAIMoves(writer, n);
                    }

                    SpecialControl();

                    /*
                    //CLIENT 1 (send move)
                    SendMove(writer);

                    //SERVER 1 (listening)
                    SetAllDesiredKeys();

                    //CLIENT 2 (listen to all moves)
                    GetMoves(reader);

                    //SERVER 2 (sending char moves)
                    SendUserMoves(writer, n);


                    


                    //SERVER 3 (sending AI movement)
                    */


                    //SLEEP
                    Thread.Sleep(250);
                }

                //NEW LEVEL INIT
                if (i < 4)
                    BonusRoundInit(i + 1);
            }

        }
        

        private void SendMove(BinaryWriter writer)
        {
            int s = 0;

            switch (desiredKey[clientPort - 1300])
            {
                case Keys.Up: s = 101; break;
                case Keys.Down: s = 102; break;
                case Keys.Left: s = 103; break;
                case Keys.Right: s = 104; break;
                case default(Keys): s = 100; break;
            }
            writer.Write(s);
        }

        private void GetMoves(BinaryReader reader)
        {
            int k;
            k = reader.Read();
            while (k == 0) { k = reader.Read(); }
            for (int ii = 0; ii < 5; ii++)
            {
                switch (k)
                {
                    case 101: desiredKey[ii] = Keys.Up; break;
                    case 102: desiredKey[ii] = Keys.Down; break;
                    case 103: desiredKey[ii] = Keys.Left; break;
                    case 104: desiredKey[ii] = Keys.Right; break;
                    case 100: desiredKey[ii] = default(Keys); break;
                    default: ii--; break;
                }
                k = reader.Read();
            }
        }

        private void SpecialControl()
        {
            if (edible > 0)
            {
                edible--;
                if (edible == 0) MakeGhostsEatersAgain();
            }
        }

        private void SetAllDesiredKeys()
        {
            for (int l = 1; l <= 4; l++)
            {
                SetDesKeyServer(l);
            }
        }

        private void SendUserMoves(BinaryWriter writer, int numberControlled)
        {
            int s;
            for (int ii = 0; ii <= 4; ii++)
            {
                if (clients[ii] == null) break;
                writer = new BinaryWriter(streams[ii]);
                for (int j = 0; j <= numberControlled; j++)
                {
                    s = 0;
                    switch (desiredKey[j])
                    {
                        case Keys.Up: s = 101; break;
                        case Keys.Down: s = 102; break;
                        case Keys.Left: s = 103; break;
                        case Keys.Right: s = 104; break;
                        case Keys.None: s = 100; break;
                    }

                    writer.Write(s);
                }
            }
        }

        private void SendAIMoves(BinaryWriter writer, int numberControlled)
        {
            int s;

            for (int ii = 0; ii <= 4; ii++)
            {
                if (clients[ii] == null) break;
                writer = new BinaryWriter(streams[ii]);
                for (int j = numberControlled + 1; j < 5; j++)
                {
                    s = 0;
                    switch (desiredKey[j])
                    {
                        case Keys.Up: s = 101; break;
                        case Keys.Down: s = 102; break;
                        case Keys.Left: s = 103; break;
                        case Keys.Right: s = 104; break;
                        case Keys.None: s = 100; break;
                    }

                    writer.Write(s);
                }
            }
        }

        private void BonusInit()
        {
            ghosts = new PictureBox[5];
            ghosts[0] = gho1;
            ghosts[1] = gho2;
            ghosts[2] = gho3;
            ghosts[3] = gho4;
            ghosts[4] = pseu;

            Init();
            map[loc[0, 1]][loc[0, 0]] = 5;
            Invoke((Action)delegate { InitLabels(); });

        }


        #endregion
    }
}