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
    public class Enemy      
    {
        //trieda reprezentujúca nepriateľov (život, hodnotu v bodoch a farbu projektilov)
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

    public enum EnemyType  
    {
        //enemyTypy - podľa nich sa priraďuje HP, pointvalue a projectilecolor.
        Type1,
        Type2,
        Type3,
        None
    }

    public class GameConfiguration 
    {
        //trieda, ktorá udržiava keybindy 
        public Key MoveUpKey { get; set; }
        public Key MoveDownKey { get; set; }
        public Key MoveLeftKey { get; set; }
        public Key MoveRightKey { get; set; }
        public Key FireKey { get; set; }
        public bool MouseControl { get; set; }
    }

    public partial class MainWindow : UserControl
    {
        //list na wavepatterny z textových súborov
        private List<string[]> wavePatterns = new List<string[]>();
        //player movement booleany pre ovládanie klávesnicou
        bool goLeft, goRight, goUp, goDown;
        // zoznam nepotrebných objektov na odstránenie
        List<Rectangle> itemsToRemove = new List<Rectangle>();  

        private GameConfiguration gameConfiguration;
        //default parametre, sú nastavené ako public aby súbory ako Settings.xaml ich mohli čítať
        public const Key DefaultMoveUpKey = Key.Up;       
        public const Key DefaultMoveDownKey = Key.Down;   
        public const Key DefaultMoveLeftKey = Key.Left;
        public const Key DefaultMoveRightKey = Key.Right;
        public const Key DefaultFireKey = Key.Space;

        public const bool DefaultMouseControl = false;
        
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

        //listy na game objekty (nepriateľov a snímky animácie výbuchu)
        List<Enemy> enemyList = new List<Enemy>();
        List<ImageSource> explosionFrames = new List<ImageSource>();

        //Timery
        DispatcherTimer enemyShootingTimer = new DispatcherTimer();     
        DispatcherTimer animationTimer = new DispatcherTimer();
        DispatcherTimer gameTimer = new DispatcherTimer();

        //RNG, slúži na generovanie indexu, ktorý zo žijúcich nepriateľov vystrelí
        Random random = new Random();  

        ImageBrush playerShip = new ImageBrush();

        PauseMenu pauseMenu = new PauseMenu();
        MainMenuWindow mainMenu = new MainMenuWindow();

        public MainWindow()
        {
            InitializeComponent();

            //Eventhandler na to, aby fungovali keybindy
            Loaded += UserControl_Loaded;

            //eventhandlery pre pausemenu a jeho tlačítka
            pauseMenu.ResumeClicked += (sender, e) => ResumeGame();     
            pauseMenu.RestartClicked += (sender, e) => ResetGame();         
            pauseMenu.ExitClicked += (sender, e) => SwitchToMainMenu();

            //načítanie keybindov zo súboru
            gameConfiguration = LoadKeybindsFromFile("../../../config.txt");

            //načitanie obrázku pre hráča
            playerShip.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/player.png"));  
            player.Fill = playerShip;                                           

            loadExplosionFrames();     
            setupTimers();
            StartGame();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)   
        {
            //Keďže pracujem s UserControl-om, tak potrebujem dať focus, inakšie keybindy na klávesnici nefungujú
            myCanvas.Focus();
        }

        private void setupTimers()
        {
            //nastavenie timerov na to, kedy budú nepriatelia
            //strieľať, na animáciu výbuchu a herného časovača,
            //ktorý rieši kolíznu logiku, skóre, životy, damage,
            //vstup cez myš/klávesnicu, víťazné podmienky, atď.

            enemyShootingTimer.Tick += EnemyShootingTimer_Tick;                     
            enemyShootingTimer.Interval = TimeSpan.FromMilliseconds(500);           
            animationTimer.Tick += AnimationTick;                                   
            animationTimer.Interval = TimeSpan.FromMilliseconds(50);               
            gameTimer.Tick += GameLoop;
            gameTimer.Interval = TimeSpan.FromMilliseconds(5);
        }

        private void loadExplosionFrames() 
        {
            //do listu načíta frames explosion animácie
            for (int i = 0; i < explosionAnimationFrames; i++)                     
            {
                string framePath = $"pack://application:,,,/images/explosionframes/frame0{i}.gif";
                explosionFrames.Add(new BitmapImage(new Uri(framePath)));
                Console.WriteLine(explosionFrames[i].ToString());
            }
            explosionImage.Source = explosionFrames[currentFrameIndex];
        }

        public void StartGame()                                                 
        {
            //štart hry, tým, že sa najprv načíta 
            //nový wavepattern a potom sa spustia timery 
            LoadWavePatterns($"../../../wave{waveNumber}.txt");                 
            enemyShootingTimer.Start();
            gameTimer.Start();
        }

        public static GameConfiguration LoadKeybindsFromFile(string path)      
        {
            //funkcia ktorá parsuje textový súbor (config.txt)
            //a ukladá ho do Gameconfiugration
            var gameConfiguration = new GameConfiguration();
            try
            {
                //súbor je formátovaný ako:
                //NázovOvládaciehoPrvku=Keybind
                //tento parser číta tieto keybindy a ukladá ich 
                //do konfigurácie.
                //konvertuje "laicky písané" keybindy zo súboru
                //na ich Key.niečo formu
                string[] lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');                                
                    if (parts.Length == 2)                                      
                    {                                                          
                        var controlName = parts[0].Trim();                     
                        var keyName = parts[1].Trim();

                        if (Enum.TryParse(keyName, out Key key))                
                        {                                                      
                            switch (controlName)                                
                            {
                                //pre každú kolónku zo súboru existuje case
                                case "FireKey":                                 
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
                        else if (controlName=="MouseControl")               
                        {
                            //alebo podľa mouseControl sa nastaví boolean
                            if (bool.TryParse(keyName, out bool result))
                            {
                                gameConfiguration.MouseControl = result;
                            } 
                        }
                    }
                }
            }
            
            catch (Exception)                                               
            {
                //v prípade akejkoľvek výnimky sa nastavia všetky bindy
                //na ich defaultné hodnoty
                gameConfiguration.FireKey = DefaultFireKey;
                gameConfiguration.MoveLeftKey = DefaultMoveLeftKey;
                gameConfiguration.MoveRightKey = DefaultMoveRightKey;
                gameConfiguration.MoveUpKey = DefaultMoveUpKey;
                gameConfiguration.MoveDownKey = DefaultMoveDownKey;
                gameConfiguration.MouseControl = DefaultMouseControl;
            }
            return gameConfiguration;
        }

        private void GameLoop(object? sender, EventArgs e)  
        {
            //každým tickom gametimeru sa vykoná
            Rect playerHitBox = new Rect(Canvas.GetLeft(player),Canvas.GetTop(player), player.Width,player.Height); //player hitbox 
            // UI aktualizácia
            score.Content = "Score: " + scorePoints + "     "+totalEnemies ;                    
            lives.Content = "Lives: " + livesCount;

            //handling pohybu hráča podľa bool hodnôt z funkcií KeyIsDown a KeyIsUp
            if (goLeft == true && Canvas.GetLeft(player) > 0)       
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) - playerHorizontalSpeed);
            }

            if (goRight == true && Canvas.GetLeft(player) + playerShipWidth < Application.Current.MainWindow.Width)
            {
                //nedovoľuje hráčovi opustiť okno
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

            if (gameConfiguration.MouseControl)        
            {
                //separátny handling pre mouse input:
                //získanie pozície mouse pointeru 
                Point mousePosition = Mouse.GetPosition(myCanvas);      
                //kontrola, či mouse pointer je ešte stále v okne
                if (mousePosition.X >= 0 && mousePosition.X <= myCanvas.ActualWidth && mousePosition.Y >= 0 && mousePosition.Y <= myCanvas.ActualHeight)    
                {
                    // nové súradnice X a Y, kde umiestní hráča
                    double newX = mousePosition.X - playerShipWidth / 2;    
                    double newY = mousePosition.Y - playerShipHeight / 2;

                    //počítaním novej polohy sa zabráni tomu, aby hráč vyšiel z okna
                    newX = Math.Max(0, Math.Min(Application.Current.MainWindow.Width - playerShipWidth, newX));
                    newY = Math.Max(0, Math.Min(Application.Current.MainWindow.Height - playerShipHeight, newY));

                    //umiestnenie hráča na novú polohu
                    Canvas.SetLeft(player, newX);   
                    Canvas.SetTop(player, newY);
                }
            }

            foreach (var item in myCanvas.Children.OfType<Rectangle>())     
            {
                // v loope prechádza cez všetky itemy s rôznymi tagmi a rieši kolízie a podobne
                if ((string)item.Tag == "projectile")
                {
                    //animacia projektilov
                    Canvas.SetTop(item, Canvas.GetTop(item) - playerProjectileVelocity);         

                    if (Canvas.GetTop(item) < windowTopSafeZone/defaultEnemyProjectileWidth)            
                    {
                        //odstránenie projektilov 
                        //hráča ak pôjdu moc vysoko
                        itemsToRemove.Add(item);
                    }

                    //vytvorenie projectile hitboxu pomocou Rect objektu
                    //Rect namiesto Rectangle, pretože konštruktor berie
                    //parametre na rozdiel od Rectangle objektov
                    Rect projectileHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);                                                        
                    foreach (var enemy in enemyList)                          
                    {
                        //for loop prechádza všetky Enemy objekty
                        if (enemy != null && myCanvas.Children.Contains(enemy.Rectangle))
                        {
                            //vytvorenie enemy hitboxov pomocou Rect objektov. 
                            Rect enemyHitBox = new Rect(Canvas.GetLeft(enemy.Rectangle), Canvas.GetTop(enemy.Rectangle), enemy.Rectangle.Width, enemy.Rectangle.Height);

                            //collision a health logika
                            if (projectileHitBox.IntersectsWith(enemyHitBox))       
                            {
                                //ak sa projektil hráča zrazí s nepriateľom 
                                itemsToRemove.Add(item);                    
                                enemy.Health -= 1;                          

                                if (enemy.Health <= 0)
                                {
                                    //ak život klesne na nulu, nepriateľ zmizne a hráč dostane body
                                    itemsToRemove.Add(enemy.Rectangle);
                                    totalEnemies -= 1;
                                    scorePoints += enemy.PointValue;

                                    if (scorePoints % extraLifeThreshold == 0)  
                                    {
                                        //v prípade, že hráč prekoná stanovenú hodnotu, získa život navyše
                                        livesCount += 1;
                                    }
                                }
                            }
                        }
                    }
                }

                if ((string)item.Tag == "enemy")
                {
                    //v každom Ticku GameTimeru sa každý nepriateľ vo wave posunie 
                    //poloha je vypočítaná ako aktuálna poloha + rýchlosť * smer
                    double currentLeft = Canvas.GetLeft(item);         
                    double newLeft = currentLeft + defaultEnemySpeed * enemyDirection;

                    if (newLeft < 0 || newLeft + item.Width > Application.Current.MainWindow.Width)
                    {
                        //zmena smeru keď sa enemy dostane na kraj okna. 1 je left to right, -1 je right to left
                        enemyDirection *= -1;                   
                        newLeft += defaultEnemySpeed * enemyDirection; 
                    }

                    Canvas.SetLeft(item, newLeft);

                    Rect enemyHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);

                    if (playerHitBox.IntersectsWith(enemyHitBox))       
                    {
                        //kolízna logika keď sa zrazí hráč s enemy. 
                        //v tomto prípade hráč nedostane žiadne body, 
                        //ale nepriateľa zničí na jeden náraz
                        if (livesCount <= 0)                            
                        {
                            playerDeath();
                            itemsToRemove.Add(item);        
                        }
                        else
                        {
                            //hráč buď zomrie, alebo sa mu uberie život
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
                    //posúvanie projektilu 
                    Canvas.SetTop(item, Canvas.GetTop(item) + defaultEnemyProjectileVelocity);  
                    if (Canvas.GetTop(item) > Application.Current.MainWindow.Height)
                    {
                        //odstránenie enemy projektilu, ktorý už nie je viditeľný
                        itemsToRemove.Add(item);            
                    }

                    // hitbox na enemy projektil
                    Rect enemyProjectileHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);

                    // logika pre kolíziu hráča s enemy projektilom
                    if (playerHitBox.IntersectsWith(enemyProjectileHitBox))  
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

            //odstraňovanie nepotrebných objektov (v každom ticku gametimeru)
            foreach (Rectangle item in itemsToRemove)
            {
                myCanvas.Children.Remove(item);
            }

            //Víťazná podmienka (všetky waves sa skončili a žiaden nepriateľ nežije) 
            if (totalEnemies<1 && waveNumber >=maxWaveNumber) 
            {
               showGameOver("Victory");
            }
            else if (totalEnemies < 1) 
            {
               //Logika v prípade, že hráč zabil všetkých nepriateľov vo wave
               //GameTimer sa pozastaví, wavepattern sa vymaže a načíta sa novým potom sa timer znova spustí
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
                    //vyberie náhodného nepriateľa zo zoznamu žijúcich nepriateľov a ten vystrelí
                    int randomEnemyIndex = random.Next(aliveEnemies.Count);
                    Enemy randomEnemy = aliveEnemies[randomEnemyIndex];
                    enemyProjectileSpawner(Canvas.GetLeft(randomEnemy.Rectangle) + randomEnemy.Rectangle.Width / 2, Canvas.GetTop(randomEnemy.Rectangle) + randomEnemy.Rectangle.Height, randomEnemy.ProjectileColor);
                }
            }
        }

        private void KeyIsDown(object sender, KeyEventArgs e) 
        {
            //keyboard bindy, nastavuje booleany na true, ak je klávesa stlačená
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

            if (e.Key == Key.Enter && gameOver==true)      
            {
                //resetovanie hry v prípade, že skončila klávesou Enter
                ResetGame();
            }

            if (e.Key == Key.Escape && !gameOver)           
            {
                //Pause/Resume logika pre ESC
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

        private void KeyIsUp(object sender, KeyEventArgs e)
        {
            //keyboard bindy, nastavuje booleany na false ak nie je klávesa stlačená
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

        private void MouseButtonDown(object sender, MouseButtonEventArgs e)
        {   
            //handling mouse button eventu
            //v prípade, že je stlačený LMB, hra nie je ukončená, ani pozastavená, a hra je ovládaná myšou, tak vystrelí
            if (e.ChangedButton == MouseButton.Left && !gameOver && !isPaused && gameConfiguration.MouseControl)
            {
                Shoot();
            }
        }

        private void Shoot()                   
        {
            //Handling vytvárania projektilov hráča
            //projektilu vytvorí Rectangle objekt, s tagom pre jednoduchší management
            Rectangle newBullet = new Rectangle     
            {
                Tag = "projectile",
                Height = defaultEnemyProjectileHeight/2,
                Width = defaultEnemyProjectileWidth/2,
                Fill = Brushes.White,
                Stroke = defaultPlayerProjectileColor,
            };

            //spawnovanie projektilu v strede hráča a nad ním
            Canvas.SetTop(newBullet, Canvas.GetTop(player) - newBullet.Height);         
            Canvas.SetLeft(newBullet, Canvas.GetLeft(player) + playerShipWidth / 4);        

            myCanvas.Children.Add(newBullet);
        }

        private void AnimationTick(object sender, EventArgs e)      
        {
            //Handling animácie výbuchu 
            if (explosionAnimationProgress < explosionAnimationFrames)
            {
                // for loop, ktorý prehrá animáciu výbuchu v poradí
                foreach (var item in myCanvas.Children.OfType<Rectangle>().Where(item => (string)item.Tag == "playerExplosion")) 
                {
                    ((ImageBrush)item.Fill).ImageSource = explosionFrames[explosionAnimationProgress];
                }
                explosionAnimationProgress++;
            }
            else 
            {
                // ak sa animácia úspešne prehrá, tak snímky sa poodstraňujú, aby nezostali na obrazovke
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
            Rectangle playerExplosion = new Rectangle   
            {
                //animácii vytvorí Rectangle objekt
                //kvôli handlingu animácie a odstráneniu nepotrebných snímkov má priradený Tag
                Tag = "playerExplosion",                
                Height = playerExplosionDimension,
                Width = playerExplosionDimension,
                Fill = new ImageBrush
                {
                    //počiatočne má ako fill prvý snímok animácie
                    ImageSource = explosionFrames[0]    
                }
            };

            // umiestnenie výbuchu do stredu hráča
            Canvas.SetTop(playerExplosion, Canvas.GetTop(player) - playerShipHeight/2) ; 
            Canvas.SetLeft(playerExplosion, Canvas.GetLeft(player) - playerShipWidth/2) ;

            myCanvas.Children.Add(playerExplosion);
            //ak by sa progres animácie neresetoval, tak by sa pri ďalšom volaní mohla
            //prehrať od iného snímku ako je prvý
            explosionAnimationProgress = 0; 
            animationTimer.Start();
        }       

        private void enemyProjectileSpawner(double x, double y, Brush projectileColor)  
        {
            //spawnovanie nepriateľských projektilov
            //každému projektilu vytvorí Rectangle objekt, ktorý vyplní farbou projektilu
            //korešpondujúcou danému typu nepriateľa
            Rectangle enemyProjectile = new Rectangle     
            {                                             
                Tag = "enemyProjectile",
                Height = defaultEnemyProjectileHeight,
                Width = defaultEnemyProjectileWidth,
                Fill = Brushes.Black,
                Stroke = projectileColor,
                StrokeThickness = 2,
            };

            //umiestnenie projektilu do okna
            Canvas.SetTop(enemyProjectile, y);          
            Canvas.SetLeft(enemyProjectile, x);

            myCanvas.Children.Add(enemyProjectile);
        }

        private void spawnEnemy(EnemyType enemyType,int x, int y) 
        {
            //handluje spawnovanie jednotlivých nepriateľov
            //zvyšovanie counteru
            totalEnemies += 1;                                     

                ImageBrush enemySkin = new ImageBrush();
                Brush projectileColor = defaultProjectileColor;
                
                //každému nepriateľovi vytvorí Rectangle objekt,
                //ktorý vyplní korešpondujúcou textúrou a kvôli
                //manažmentu/kolíziám mu priradí Tag enemy
                Rectangle newEnemy = new Rectangle                
                {                                                 
                    Tag = "enemy",                               
                    Height = Enemy.DefaultHeight,
                    Width = Enemy.DefaultWidth,
                    Fill = enemySkin
                };

                int enemyHealth=defaultEnemyHealth;
                int enemyPointValue = defaultEnemyPointValue;
                switch (enemyType)                              
                {
                //podľa typu neprateľa sa nastavia parametre
                //ako health, koĺko bodov za zabitie dostane hráč,
                //farbu projektilu a textúru korešpondujúcu typu nepriateľa
                    case EnemyType.Type1:                       
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

                //vytvorenie a umiestnenie Enemy objektu do okna
                Enemy enemy = new Enemy(newEnemy, enemyHealth, enemyPointValue, projectileColor);     
                                                                                                      
                Canvas.SetTop(enemy.Rectangle, x);                                                    
                Canvas.SetLeft(enemy.Rectangle, y);                                                   

                myCanvas.Children.Add(enemy.Rectangle);
                //pridanie do listu nepriateľov
                enemyList.Add(enemy);                                                                 
        }

        private void SpawnEnemiesFromWavePattern(string[] wavePattern) 
        {
            //rozmiestňuje nepriateľov v okne podľa patternu
            //prechádza každý row a column 
            //textového súboru, ktorý má v sebe wave
            for (int row = 0; row < wavePattern.Length; row++)         
            {                                   
                string rowConfig = wavePattern[row];
                for (int col = 0; col < rowConfig.Length; col++)
                {
                    char enemyChar = rowConfig[col];
                    //whitespace charactery znamenajú medzery medzi nepriateľmi
                    if (enemyChar != ' ')                               
                    {
                        //podľa pozície v textovom súbore rozmiestňuje nepriateľov v okne
                        int x = row * Enemy.DefaultWidth + windowTopSafeZone; 
                        int y = col * Enemy.DefaultHeight;

                        //volanie funkcie na spawnovanie nepriateľa na súradniciach podľa patternu
                        spawnEnemy(ParseEnemyType(enemyChar), x, y);    
                    }
                }
            }
        }

        private void StartNextWave()                        
        {
            //začne ďalšiu wave tým, že načíta korešpondujúci wavePattern
            //zo zoznamu a potom ho predá funkcii, ktorá má na starosť spawnovanie 
            //nepriateľov
            string[] wavePattern = wavePatterns[currentWave];
            SpawnEnemiesFromWavePattern(wavePattern);
        }

        private void LoadWavePatterns(string fileName)  
        {
            //loaduje obsah textového súboru do wavePatterns
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

        private EnemyType ParseEnemyType(char enemyChar) 
        {
            //parser enemyTypov z čísel v textovom súbore na EnemyType typy.
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
            //hráč zmizne s výbuchom a hra končí
            player.Visibility = Visibility.Collapsed; 
            explode();
            showGameOver("You died");
        }

        private void showGameOver(string msg)       
        {
            //zobrazenie Game Over, aj so správou (podľa toho, či hráč vyhral alebo prehral)
            gameOver = true;
            wavePatterns.Clear();
            gameTimer.Stop();
            enemyShootingTimer.Stop();
            score.Content += " " + msg + "!   Enter to play again";
        }

        private void SwitchToMainMenu() 
        {
            //prepnutie do menu
            wavePatterns.Clear();
            gameTimer.Stop();
            enemyShootingTimer.Stop();
            gameOver = true;
            this.Content = mainMenu;
            mainMenu.Focus();
        }

        private void PauseGame()    
        {
            //pauza
            isPaused = true;
            gameTimer.Stop();
            enemyShootingTimer.Stop();
            myCanvas.Children.Add(pauseMenu);
        }

        private void ResumeGame()   
        {
            //resume hry z paused state
            isPaused = false;
            myCanvas.Children.Remove(pauseMenu);
            myCanvas.Focus();
            gameTimer.Start();
            enemyShootingTimer.Start();
        }

        private void ResetGame()    
        {
            //resetovanie hry na default hodnotách
            if (pauseMenu is not null)
            {
                //kontrola, aby som náhodou neodstraňoval pauseMenu, ktoré neexistuje
                myCanvas.Children.Remove(pauseMenu);    
            }                                           
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

            foreach (var item in myCanvas.Children.OfType<Rectangle>().ToList()) 
            {
                //vymazanie nepotrebných assetov - keby som spravil .Clear(), tak by to zmazalo aj potrebné veci
                if ((string)item.Tag == "enemy" || (string)item.Tag == "projectile" || (string)item.Tag == "enemyProjectile")
                {
                    myCanvas.Children.Remove(item);
                }
            }
            StartGame();
        }
    }
}
