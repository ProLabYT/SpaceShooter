using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Threading;
using static SpaceShooter.MainWindow;
using System.Windows.Automation;
using System.Printing;

namespace SpaceShooter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class Enemy      //trieda reprezentujúca nepriateľov (život, hodnotu v bodoch a farbu projektilov)
    {
        public static int DefaultHeight = 50;
        public static int DefaultWidth = 50;
        public Rectangle Rectangle { get; set; }
        public int Health { get; set; }
        public int PointValue { get; set; }
        public int Speed { get; set; }
        public Brush ProjectileColor { get; set; }

        public Enemy(Rectangle rectangle, int health, int pointValue,  Brush projectileColor)
        {
            Rectangle = rectangle;
            Health = health;
            PointValue = pointValue;
            ProjectileColor = projectileColor;
        }
    }

    public enum EnemyType   //enemyTypy - podľa nich sa priraďuje HP, pointvalue a projectilecolor.
    {
        Type1,
        Type2,
        Type3,
        None
    }

    public class GameConfiguration //trieda, ktorá udržiava keybindy 
    {
        public Key MoveUpKey { get; set; }
        public Key MoveDownKey { get; set; }
        public Key MoveLeftKey { get; set; }
        public Key MoveRightKey { get; set; }
        public Key FireKey { get; set; }
        public bool MouseControl { get; set; }
    }

    public partial class MainWindow : UserControl
    {
        private List<string[]> wavePatterns = new List<string[]>(); //list na wavepatterny z textových súborov

        bool goLeft, goRight, goUp, goDown; //player movement booleany pre ovládanie klávesnicou

        List<Rectangle> itemsToRemove = new List<Rectangle>();  // zoznam nepotrebných objektov na odstránenie

        private GameConfiguration gameConfiguration;
        
        public const Key DefaultMoveUpKey = Key.Up;       //default parametre, sú nastavené ako public aby súbory ako Settings.xaml
        public const Key DefaultMoveDownKey = Key.Down;   //ich mohli čítať
        public const Key DefaultMoveLeftKey = Key.Left;
        public const Key DefaultMoveRightKey = Key.Right;
        public const Key DefaultFireKey = Key.Space;

        public const bool DefaultMouseControl = false;
        //konštanty
        private const int maxWaveNumber = 3;
        private const int playerShipWidth = 80;
        private const int playerShipHeight = 100;
        private const int playerHorizontalSpeed = 5;
        private const int playerVerticalSpeed = 2;
        private const int playerProjectileVelocity = 20;
        private const int playerExplosionDimension = 200;
        private const int defaultLivesCount = 3;
        private const int defaultEnemyPointValue = 0;
        private const int defaultEnemyHealth = 0;
        private const int defaultEnemyProjectileHeight = 40;
        private const int defaultEnemyProjectileWidth = 10;
        private const int defaultEnemyProjectileVelocity = 10;
        private const int defaultEnemySpeed = 6;
        private const int windowTopSafeZone = 50;
        private const int extraLifeThreshold = 2000;
        private const int explosionAnimationFrames = 10;
        //hodnoty, ktoré sa v priebehu hry menia
        private int currentWave = 0;
        private int waveNumber = 0;     
        private int livesCount = defaultLivesCount;
        private int totalEnemies = 0;
        private int scorePoints = 0;
        private int explosionAnimationProgress = 0;
        private int currentFrameIndex = 0;
        private int enemyDirection = 1;
        
        private bool gameOver = false;
        private bool isPaused = false;

        Brush defaultProjectileColor = Brushes.Red;                 
        Brush defaultPlayerProjectileColor = Brushes.Blue;

        List<Enemy> enemyList = new List<Enemy>();                   //listy na game objecty
        List<ImageSource> explosionFrames = new List<ImageSource>(); //list na jednotlivé snímky animácie výbuchu

        DispatcherTimer enemyShootingTimer = new DispatcherTimer();     //Timery
        DispatcherTimer animationTimer = new DispatcherTimer();
        DispatcherTimer gameTimer = new DispatcherTimer();

        Random random = new Random();  //RNG, slúži na generovanie indexu, ktorý z žijúcich nepriateľov vystrelí

        ImageBrush playerShip = new ImageBrush();

        PauseMenu pauseMenu = new PauseMenu();
        MainMenuWindow mainMenu = new MainMenuWindow();

        public MainWindow()
        {
            InitializeComponent();
            
            Loaded += UserControl_Loaded;                               //Eventhandler na to, aby fungovali keybindy
            pauseMenu.ResumeClicked += (sender, e) => ResumeGame();     
            pauseMenu.RestartClicked += (sender, e) => ResetGame();         //eventhandlery pre pausemenu a jeho tlačítka
            pauseMenu.ExitClicked += (sender, e) => SwitchToMainMenu();

            gameConfiguration = LoadKeybindsFromFile("../../../config.txt");    //načítanie keybindov zo súboru
            playerShip.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/player.png"));  
            player.Fill = playerShip;                                           //načitanie obrázku pre hráča

            loadExplosionFrames();     
            setupTimers();
            StartGame();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)   //Keďže pracujem s UserControl-om, tak potrebujem dať focus, inakšie keybindy na klávesnici nefungujú
        {
            myCanvas.Focus();
        }

        private void setupTimers()
        { 
            enemyShootingTimer.Tick += EnemyShootingTimer_Tick;                     //nastavenie timerov na to, kedy budú nepriatelia
            enemyShootingTimer.Interval = TimeSpan.FromMilliseconds(500);           //strieľať, na animáciu výbuchu a herného časovača,
            animationTimer.Tick += AnimationTick;                                   //ktorý rieši kolíznu logiku, skóre, životy, damage,
            animationTimer.Interval = TimeSpan.FromMilliseconds(50);                //vstup cez myš/klávesnicu, víťazné podmienky, atď.
            gameTimer.Tick += GameLoop;
            gameTimer.Interval = TimeSpan.FromMilliseconds(5);
        }

        private void loadExplosionFrames() 
        {
            
            for (int i = 0; i < explosionAnimationFrames; i++)                      //do listu načíta frames explosion animácie
            {
                string framePath = $"pack://application:,,,/images/explosionframes/frame0{i}.gif";
                explosionFrames.Add(new BitmapImage(new Uri(framePath)));
                Console.WriteLine(explosionFrames[i].ToString());
            }
            explosionImage.Source = explosionFrames[currentFrameIndex];
        }

        public void StartGame()
        {
            LoadWavePatterns($"../../../wave{waveNumber}.txt");
            enemyShootingTimer.Start();
            gameTimer.Start();
        }

        public static GameConfiguration LoadKeybindsFromFile(string path)       //funkcia ktorá parsuje textový súbor (config.txt)
        {                                                                       //a ukladá ho do Gameconfiugration
            var gameConfiguration = new GameConfiguration();
            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');                                //súbor je formátovaný ako:
                    if (parts.Length == 2)                                      //NázovOvládaciehoPrvku=Keybind
                    {                                                           //tento parser číta tieto keybindy a ukladá ich 
                        var controlName = parts[0].Trim();                      //do konfigurácie
                        var keyName = parts[1].Trim();

                        if (Enum.TryParse(keyName, out Key key))                //konvertuje "laicky písané" keybindy zo súboru
                        {                                                       //na ich Key.niečo formu
                            switch (controlName)                                
                            {
                                case "FireKey":                                 //pre každú kolónku zo súboru existuje case
                                    gameConfiguration.FireKey = key;
                                    break;
                                case "MoveLeftKey":
                                    gameConfiguration.MoveLeftKey = key;
                                    break;
                                case "MoveRightKey":
                                    gameConfiguration.MoveRightKey = key;
                                    break;
                                case "MoveUpKey":
                                    gameConfiguration.MoveUpKey = key;
                                    break;
                                case "MoveDownKey":
                                    gameConfiguration.MoveDownKey = key;
                                    break;
                            }
                        }
                        else if (controlName=="MouseControl")               //alebo podľa mouseControl sa nastaví boolean
                        {
                            if(bool.TryParse(keyName, out bool result))
                            {
                                gameConfiguration.MouseControl = result;
                            } 
                        }
                    }
                }
            }
            
            catch (Exception)                                               //v prípade akejkoľvek výnimky sa nastavia všetky bindy
            {                                                               //na ich defaultné hodnoty
                gameConfiguration.FireKey = DefaultFireKey;
                gameConfiguration.MoveLeftKey = DefaultMoveLeftKey;
                gameConfiguration.MoveRightKey = DefaultMoveRightKey;
                gameConfiguration.MoveUpKey = DefaultMoveUpKey;
                gameConfiguration.MoveDownKey = DefaultMoveDownKey;
                gameConfiguration.MouseControl = DefaultMouseControl;
            }
            return gameConfiguration;
        }

        private void GameLoop(object? sender, EventArgs e)  //každým tickom gametimeru sa vykoná
        {
            Rect playerHitBox = new Rect(Canvas.GetLeft(player),Canvas.GetTop(player), player.Width,player.Height); //player hitbox 
            score.Content = "Score: " + scorePoints + "     "+totalEnemies ;                    // UI aktualizácia
            lives.Content = "Lives: " + livesCount;

            if (goLeft == true && Canvas.GetLeft(player) > 0)       //handling pohybu hráča podľa bool hodnôt z funkcií KeyIsDown a KeyIsUp
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) - playerHorizontalSpeed);
            }

            if (goRight == true && Canvas.GetLeft(player) + playerShipWidth < Application.Current.MainWindow.Width) //nedovoľuje hráčovi opustiť okno
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) + playerHorizontalSpeed);
            }

            if (goUp == true && Canvas.GetTop(player) > 0)
            {
                Canvas.SetTop(player, Canvas.GetTop(player) - playerVerticalSpeed);
            }

            if (goDown == true && Canvas.GetTop(player) + playerShipHeight < Application.Current.MainWindow.Height)
            {
                Canvas.SetTop(player, Canvas.GetTop(player) + playerVerticalSpeed);
            }

            if (gameConfiguration.MouseControl)         //separátny handling pre mouse input
            {
                Point mousePosition = Mouse.GetPosition(myCanvas);      //získanie pozície mouse pointeru 
                //kontrola, či mouse pointer je ešte stále v okne
                if (mousePosition.X >= 0 && mousePosition.X <= myCanvas.ActualWidth && mousePosition.Y >= 0 && mousePosition.Y <= myCanvas.ActualHeight)    
                {
                    double newX = mousePosition.X - playerShipWidth / 2;    // nové súradnice X a Y, kde umiestní hráča
                    double newY = mousePosition.Y - playerShipHeight / 2;

                    //počítaním novej polohy sa zabráni tomu, aby hráč vyšiel z okna
                    newX = Math.Max(0, Math.Min(Application.Current.MainWindow.Width - playerShipWidth, newX));
                    newY = Math.Max(0, Math.Min(Application.Current.MainWindow.Height - playerShipHeight, newY));

                    Canvas.SetLeft(player, newX);   //umiestnenie hráča na novú polohu
                    Canvas.SetTop(player, newY);
                }
            }

            foreach (var item in myCanvas.Children.OfType<Rectangle>())     // v loope prechádza cez všetky itemy s rôznymi tagmi a rieši kolízie a podobne
            {
                if ((string)item.Tag == "projectile")
                {
                    Canvas.SetTop(item, Canvas.GetTop(item) - playerProjectileVelocity);          //animacia projektilov

                    if (Canvas.GetTop(item) < windowTopSafeZone/defaultEnemyProjectileWidth)            //odstránenie projektilov 
                    {                                                                                   //hráča ak pôjdu moc vysoko
                        itemsToRemove.Add(item);
                    }

                    Rect projectileHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);  
                                                                              //vytvorenie projectile hitboxu pomocou Rect objektu
                                                                              //Rect namiesto Rectangle, pretože konštruktor berie
                                                                              //parametre na rozdiel od Rectangle objektov
                    foreach (var enemy in enemyList)                          //for loop prechádza všetky Enemy objekty
                    {
                        if (enemy != null && myCanvas.Children.Contains(enemy.Rectangle))
                        {  
                            Rect enemyHitBox = new Rect(Canvas.GetLeft(enemy.Rectangle), Canvas.GetTop(enemy.Rectangle), enemy.Rectangle.Width, enemy.Rectangle.Height);
                                                                                    //vytvorenie enemy hitboxov pomocou Rect objektov. Enemy hitboxy 
                            if (projectileHitBox.IntersectsWith(enemyHitBox))       //collision a health logika
                            {
                                itemsToRemove.Add(item);                    //ak sa projektil hráča zrazí s nepriateľom 
                                enemy.Health -= 1;                          //tak nepriateľovi uberie život

                                if (enemy.Health <= 0)                      //ak život klesne však na nulu, nepriateľ zmizne a hráč dostane body
                                {
                                    itemsToRemove.Add(enemy.Rectangle);
                                    totalEnemies -= 1;
                                    scorePoints += enemy.PointValue;

                                    if (scorePoints % extraLifeThreshold == 0)  //v prípade, že hráč prekoná stanovenú hodnotu, získa život navyše
                                    {
                                        livesCount += 1;
                                    }
                                }
                            }
                        }
                    }
                }

                if ((string)item.Tag == "enemy")
                {
                    double currentLeft = Canvas.GetLeft(item);          //v každom Ticku GameTimeru sa každý nepriateľ vo wave posunie 
                    double newLeft = currentLeft + defaultEnemySpeed * enemyDirection; //poloha je vypočítaná ako aktuálna poloha + rýchlosť x smer

                    if (newLeft < 0 || newLeft + item.Width > Application.Current.MainWindow.Width)
                    {
                        enemyDirection *= -1;                   //zmena smeru keď sa enemy dostane na kraj okna. 1 je left to right, -1 je right to left
                        newLeft += defaultEnemySpeed * enemyDirection; // pohyb naspäť 
                    }

                    Canvas.SetLeft(item, newLeft);

                    Rect enemyHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);

                    if (playerHitBox.IntersectsWith(enemyHitBox))       //kolízna logika keď sa zrazí hráč s enemy. 
                    {                                                   //v tomto prípade hráč nedostane žiadne body, 
                        if (livesCount <= 0)                            //ale nepriateľa zničí na jeden náraz
                        {
                            playerDeath();
                            itemsToRemove.Add(item);        //hráč buď zomrie, alebo sa mu uberie život
                        }
                        else
                        {   
                            livesCount -= 1;                
                            itemsToRemove.Add(item);      
                        }
                        totalEnemies -= 1;
                        explode();
                        break;
                    }
                }

                if ((string)item.Tag == "enemyProjectile")
                {
                    Canvas.SetTop(item, Canvas.GetTop(item) + defaultEnemyProjectileVelocity);  //posúvanie projektilu 
                    if (Canvas.GetTop(item) > Application.Current.MainWindow.Height)
                    {
                        itemsToRemove.Add(item);            //odstránenie enemy projektilu, ktorý už nie je viditeľný
                    }

                    Rect enemyProjectileHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height); // hitbox na enemy projektil

                    if (playerHitBox.IntersectsWith(enemyProjectileHitBox))  // logika pre kolíziu hráča s enemy projektilom
                    {
                        if (livesCount <= 0)
                        {
                            playerDeath();
                            itemsToRemove.Add(item);
                        }
                        else
                        {
                            livesCount -= 1;
                            itemsToRemove.Add(item);
                        }
                        explode();
                        break;
                    }
                }
            }


            foreach (Rectangle item in itemsToRemove) //odstraňovanie nepotrebných objektov (v každom ticku gametimeru)
            {
                myCanvas.Children.Remove(item);
            }
             
            if (totalEnemies<1 && waveNumber >=maxWaveNumber) //Víťazná podmienka (všetky waves sa skončili a žiaden nepriateľ nežije)
            {
               showGameOver("Victory");
            }
            else if (totalEnemies < 1) //Logika v prípade, že hráč zabil všetkých nepriateľov vo wave
            {                          //GameTimer sa pozastaví, wavepattern sa vymaže a načíta sa novým potom sa timer znova spustí
               gameTimer.Stop();
               wavePatterns.Clear();
               waveNumber+=1;
               LoadWavePatterns($"../../../wave{waveNumber}.txt");
               gameTimer.Start();
               StartNextWave();
            }
        }

        private void EnemyShootingTimer_Tick(object sender, EventArgs e)
        {
            if (enemyList.Count > 0)
            {
                List<Enemy> aliveEnemies = enemyList.Where(enemy => myCanvas.Children.Contains(enemy.Rectangle)).ToList();
                //kazdym Tickom timeru sa aktualizuje zoznam žijúcich nepriateľov (a teda vhodných kandidýtov na to, aby vystrelili)
                if (aliveEnemies.Count > 0 && !isPaused && !gameOver)
                {
                    int randomEnemyIndex = random.Next(aliveEnemies.Count); //vyberie náhodného nepriateľa zo zoznamu žijúcich nepriateľov a ten vystrelí
                    Enemy randomEnemy = aliveEnemies[randomEnemyIndex];
                    enemyProjectileSpawner(Canvas.GetLeft(randomEnemy.Rectangle) + randomEnemy.Rectangle.Width / 2, Canvas.GetTop(randomEnemy.Rectangle) + randomEnemy.Rectangle.Height, randomEnemy.ProjectileColor);
                }
            }
        }

        private void KeyIsDown(object sender, KeyEventArgs e) //keyboard bindy, nastavuje booleany na true, ak je klávesa stlačená
        {
            if (e.Key == gameConfiguration.MoveLeftKey) 
            { 
                goLeft= true;
            }

            if (e.Key == gameConfiguration.MoveRightKey)
            {
                goRight = true;
            }

            if (e.Key == gameConfiguration.MoveUpKey)
            {
                goUp = true;
            }

            if (e.Key == gameConfiguration.MoveDownKey)
            {
                goDown = true;
            }

            if (e.Key == gameConfiguration.FireKey && !gameOver && !isPaused)
            {
                Shoot();
            }

            if (e.Key == Key.Enter && gameOver==true)       //resetovanie hry v prípade, že skončila klávesou Enter
            {
                ResetGame();
            }

            if (e.Key == Key.Escape && !gameOver)           //Pause/Resume logika pre ESC
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        private void KeyIsUp(object sender, KeyEventArgs e)     //keyboard bindy, nastavuje booleany na false ak nie je klávesa stlačená
        {
            if (e.Key == gameConfiguration.MoveLeftKey) 
            {
                goLeft = false;
            }

            if (e.Key == gameConfiguration.MoveRightKey) 
            {
                goRight = false;
            }

            if (e.Key == gameConfiguration.MoveUpKey) 
            {
                goUp = false;
            }

            if (e.Key == gameConfiguration.MoveDownKey) 
            {
                goDown = false;
            }
        }

        private void MouseButtonDown(object sender, MouseButtonEventArgs e)     //handling mouse button eventu
        {   //v prípade, že je stlačený LMB, hra nie je ukončená, ani pozastavená, a hra je ovládaná myšou, tak vystrelí
            if (e.ChangedButton == MouseButton.Left && !gameOver && !isPaused && gameConfiguration.MouseControl)
            {
                Shoot();
            }
        }

        private void Shoot()                    //Handling vytvárania projektilov hráča
        {
            Rectangle newBullet = new Rectangle     //projektilu vytvorí Rectangle objekt, s tagom pre jednoduchší management
            {
                Tag = "projectile",
                Height = defaultEnemyProjectileHeight/2,
                Width = defaultEnemyProjectileWidth/2,
                Fill = Brushes.White,
                Stroke = defaultPlayerProjectileColor,
            };

            Canvas.SetTop(newBullet, Canvas.GetTop(player) - newBullet.Height);         
            Canvas.SetLeft(newBullet, Canvas.GetLeft(player) + playerShipWidth / 4);        //spawnovanie projektilu v strede hráča a nad ním

            myCanvas.Children.Add(newBullet);
        }

        private void AnimationTick(object sender, EventArgs e)      //Handling animácie výbuchu 
        {
            if (explosionAnimationProgress < explosionAnimationFrames)
            {
                foreach (var item in myCanvas.Children.OfType<Rectangle>().Where(item => (string)item.Tag == "playerExplosion")) // for loop, ktory prehra animaciu vybuchu v poradi
                {
                    ((ImageBrush)item.Fill).ImageSource = explosionFrames[explosionAnimationProgress];
                }
                explosionAnimationProgress++;
            }
            else // ak sa animácia úspešne prehrá, tak snímky sa poodstraňujú, aby nezostali na obrazovke
            {
                var explosionRectangles = myCanvas.Children.OfType<Rectangle>().Where(item => (string)item.Tag == "playerExplosion").ToList();
                foreach (var item in explosionRectangles)
                {
                    myCanvas.Children.Remove(item);
                }
                explosionAnimationProgress = 0;
                animationTimer.Stop();
            }
        }

        private void explode()              
        {
            Rectangle playerExplosion = new Rectangle   //animácii vytvorí Rectangle objekt
            {
                Tag = "playerExplosion",                //kvôli handlingu animácie a odstráneniu nepotrebných snímkov má priradený Tag
                Height = playerExplosionDimension,
                Width = playerExplosionDimension,
                Fill = new ImageBrush
                {
                    ImageSource = explosionFrames[0]    //počiatočne má ako fill prvý snímok animácie
                }
            };

            Canvas.SetTop(playerExplosion, Canvas.GetTop(player) - playerShipHeight/2) ; // umiestnenie výbuchu do stredu hráča
            Canvas.SetLeft(playerExplosion, Canvas.GetLeft(player) - playerShipWidth/2) ;

            myCanvas.Children.Add(playerExplosion);    
            explosionAnimationProgress = 0; //ak by sa progres animácie neresetoval, tak by sa pri ďalšom volaní mohla prehrať od iného snímku ako je prvý
            animationTimer.Start();
        }       

        private void enemyProjectileSpawner(double x, double y, Brush projectileColor)  //spawnovanie nepriateľských projektilov
        {
            Rectangle enemyProjectile = new Rectangle     //každému projektilu vytvorí Rectangle objekt, ktorý vyplní farbou projektilu
            {                                             //korešpondujúcou danému typu nepriateľa
                Tag = "enemyProjectile",
                Height = defaultEnemyProjectileHeight,
                Width = defaultEnemyProjectileWidth,
                Fill = Brushes.Black,
                Stroke = projectileColor,
                StrokeThickness = 2,
            };

            Canvas.SetTop(enemyProjectile, y);          //umiestnenie projektilu do okna
            Canvas.SetLeft(enemyProjectile, x);

            myCanvas.Children.Add(enemyProjectile);
        }

        private void spawnEnemy(EnemyType enemyType,int x, int y) //handluje spawnovanie jednotlivých nepriateľov
        {       
            totalEnemies +=1;                                     //zvyšovanie counteru

                ImageBrush enemySkin = new ImageBrush();
                Brush projectileColor = defaultProjectileColor;

                Rectangle newEnemy = new Rectangle               //každému nepriateľovi vytvorí Rectangle objekt, 
                {                                                //ktorý vyplní korešpondujúcou textúrou a kvôli 
                    Tag = "enemy",                               //manažmentu/kolíziám mu priradí Tag enemy
                    Height = Enemy.DefaultHeight,
                    Width = Enemy.DefaultWidth,
                    Fill = enemySkin
                };

                int enemyHealth=defaultEnemyHealth;
                int enemyPointValue = defaultEnemyPointValue;
                switch (enemyType)                              //podľa typu neprateľa sa nastavia parametre
                {                                               //ako health, koĺko bodov za zabitie dostane hráč,
                    case EnemyType.Type1:                       //farbu projektilu a textúru korešpondujúcu typu nepriateľa
                        enemyHealth = 2;
                        enemyPointValue = 100;
                        enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader1.webp"));
                        projectileColor = Brushes.Red;
                        break;

                    case EnemyType.Type2:
                        enemyHealth = 3;
                        enemyPointValue = 300;
                        enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader2.webp"));
                        projectileColor = Brushes.Green;
                        break;

                    case EnemyType.Type3:
                        enemyHealth = 4;
                        enemyPointValue = 400;
                        enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader3.webp"));
                        projectileColor = Brushes.Purple;
                        break;
                }

                Enemy enemy = new Enemy(newEnemy, enemyHealth, enemyPointValue, projectileColor);     //vytvorenie
                                                                                                      //a umiestnenie 
                Canvas.SetTop(enemy.Rectangle, x);                                                    //Enemy objektu
                Canvas.SetLeft(enemy.Rectangle, y);                                                   //do okna

                myCanvas.Children.Add(enemy.Rectangle);
                enemyList.Add(enemy);                                                                 //pridanie do listu nepriateľov
        }

        private void SpawnEnemiesFromWavePattern(string[] wavePattern) //rozmiestňuje nepriateľov v okne podľa patternu
        {                                                              //prechádza každý row a column 
            for (int row = 0; row < wavePattern.Length; row++)         //textového súboru, ktorý má v sebe wave
            {                                   
                string rowConfig = wavePattern[row];
                for (int col = 0; col < rowConfig.Length; col++)
                {
                    char enemyChar = rowConfig[col];
                    if (enemyChar != ' ')                               //whitespace charactery znamenajú medzery medzi nepriateľmi
                    {
                        int x = row * Enemy.DefaultWidth + windowTopSafeZone; //podľa pozície v textovom súbore rozmiestňuje nepriateľov
                        int y = col * Enemy.DefaultHeight;                    //v okne 

                        spawnEnemy(ParseEnemyType(enemyChar), x, y);    //volanie funkcie na spawnovanie nepriateľa na súradniciach podľa patternu
                    }
                }
            }
        }

        private void StartNextWave()                         //začne ďalšiu wave tým, že načíta korešpondujúci wavePattern
        {                                                    //zo zoznamu a potom ho predá funkcii, ktorá má na starosť spawnovanie 
            string[] wavePattern = wavePatterns[currentWave];//nepriateľov
            SpawnEnemiesFromWavePattern(wavePattern);
        }

        private void LoadWavePatterns(string fileName)  //loaduje obsah textového súboru do wavePatterns
        {
            try
            {
                string[] lines = File.ReadAllLines(fileName);
                wavePatterns.Add(lines);
                //File.WriteAllLines("../../../output.txt", wavePatterns[0]); //debugging statement
            }
            catch (FileNotFoundException)
            {
               //score.Content = "Not Found:" +fileName; //debugging statement
            }
        }

        private EnemyType ParseEnemyType(char enemyChar) // parser enemyTypov z čísel v textovom súbore na EnemyType typy.
        {
            switch (enemyChar)
            {
                case '1':
                    return EnemyType.Type1;
                case '2':
                    return EnemyType.Type2;
                case '3':
                    return EnemyType.Type3;
                default:
                    return EnemyType.None; 
            }
        }

        private void playerDeath()
        {
            player.Visibility = Visibility.Collapsed;  // hráć zmizne s 3 výbuchmi a hra končí
            for (int i = 0; i < 3; i++)
            {
                explode();
            }
            showGameOver("You died");
        }

        private void showGameOver(string msg)       //zobrazenie Game Over, aj so správou (podľa toho, či hráč vyhral alebo prehral
        {
            gameOver = true;
            wavePatterns.Clear();
            gameTimer.Stop();
            enemyShootingTimer.Stop();
            score.Content += " " + msg + "!   Enter to play again";
        }

        private void SwitchToMainMenu() //prepnutie do menu
        {
            wavePatterns.Clear();
            gameTimer.Stop();
            enemyShootingTimer.Stop();
            gameOver = true;
            this.Content = mainMenu;
            mainMenu.Focus();
        }

        private void PauseGame()    //pauza
        {
            isPaused = true;
            gameTimer.Stop();
            enemyShootingTimer.Stop();
            myCanvas.Children.Add(pauseMenu);
        }

        private void ResumeGame()   //resume hry z paused state
        {
            isPaused = false;
            myCanvas.Children.Remove(pauseMenu);
            myCanvas.Focus();
            gameTimer.Start();
            enemyShootingTimer.Start();
        }

        private void ResetGame()    //resetovanie hry na default hodnotách
        {
            if (pauseMenu is not null)
            {
                myCanvas.Children.Remove(pauseMenu);    //kontrola, aby som náhodou neodstraňoval pauseMenu
            }                                           //ktoré neexistuje
            myCanvas.Focus();  
            isPaused = false; 
            gameOver = false;
            waveNumber = 0;
            wavePatterns.Clear();
            totalEnemies = 0;
            scorePoints = 0;
            livesCount = defaultLivesCount;
            itemsToRemove.Clear(); 
            player.Visibility = Visibility.Visible;

            foreach (var item in myCanvas.Children.OfType<Rectangle>().ToList()) //vymazanie nepotrebných assetov - keby som spravil 
            {                                                                    //.Clear(), tak by to zmazalo aj potrebné veci
                if ((string)item.Tag == "enemy" || (string)item.Tag == "projectile" || (string)item.Tag == "enemyProjectile")
                {
                    myCanvas.Children.Remove(item);
                }
            }
            StartGame();
        }
    }
}
