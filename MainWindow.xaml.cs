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

namespace SpaceShooter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    

    public enum EnemyType
    {
        Type1,
        Type2,
        Type3
    }

    public class GameConfiguration
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

        bool goLeft, goRight, goUp, goDown; //player movement booleany pre ovládanie klávesnicou

        List<Rectangle> itemsToRemove = new List<Rectangle>();  // zoznam nepotrebných objektov na odstránenie

        private GameConfiguration gameConfiguration;
        
        private const Key DefaultMoveUptKey = Key.Up;
        private const Key DefaultMoveDownKey = Key.Down;
        private const Key DefaultMoveLeftKey = Key.Left;
        private const Key DefaultMoveRightKey = Key.Right;
        private const Key DefaultFireKey = Key.Space;
        private const bool DefaultMouseControl = false;

        int enemySpawnCount = 20;
        int livesCount = 3;
        int enemyImages = 0;
        int enemyImagesCount = 8;
        int projectileTimerLimit = 200;             //default parametre
        int totalEnemies = 0;
        int scorePoints = 0;
        int defaultEnemyPointValue = 100;
        int extraLifeThreshold = 2000;
        int enemySpeed = 6;
        int defaultEnemyWidth = 50;
        int defaultEnemyHeight = 50;
        int playerShipWidth = 80;
        int playerShipHeight = 100;
        int explosionAnimationFrames = 10;
        int explosionAnimationProgress = 0;
        int currentFrameIndex = 0;

        //bool mouseControl;
        bool gameOver = false;
        bool isPaused = false;

        List<Enemy> enemyList = new List<Enemy>();      //listy na game objecty
        List<ImageSource> explosionFrames = new List<ImageSource>();

        DispatcherTimer enemyShootingTimer = new DispatcherTimer();     //timery
        DispatcherTimer animationTimer = new DispatcherTimer();
        DispatcherTimer gameTimer = new DispatcherTimer();

        Random random = new Random();  //RNG, slúži na generovanie indexu, ktorý z žijúcich nepriateľov vystrelí

        ImageBrush playerShip = new ImageBrush();

        PauseMenu pauseMenu = new PauseMenu();
        MainMenuWindow mainMenu = new MainMenuWindow();

        public MainWindow()
        {
            InitializeComponent();
            
            Loaded += UserControl_Loaded;
            pauseMenu.ResumeClicked += (sender, e) => ResumeGame();
            pauseMenu.RestartClicked += (sender, e) => ResetGame();
            pauseMenu.ExitClicked += (sender, e) => SwitchToMainMenu();
            gameConfiguration = LoadKeybindsFromFile("../../../config.txt");
            playerShip.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/player.png"));
            player.Fill = playerShip;

            for (int i = 0; i < explosionAnimationFrames; i++)              //do listu načíta frames explosion animácie
            {
                string framePath = $"pack://application:,,,/images/explosionframes/frame0{i}.gif";
                explosionFrames.Add(new BitmapImage(new Uri(framePath)));
                Console.WriteLine(explosionFrames[i].ToString());
            };

            enemyShootingTimer.Tick += EnemyShootingTimer_Tick;
            enemyShootingTimer.Interval = TimeSpan.FromMilliseconds(500);
            animationTimer.Tick += AnimationTick;
            animationTimer.Interval = TimeSpan.FromMilliseconds(50);
            explosionImage.Source = explosionFrames[currentFrameIndex];
            gameTimer.Tick += GameLoop;
            gameTimer.Interval = TimeSpan.FromMilliseconds(1);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)   //Keďže pracujem s UserControl-om, tak potrebujem dať focus, inakšie keybindy na klávesnici nefungujú
        {
            myCanvas.Focus();
        }

        public void StartGame()
        {
            enemyShootingTimer.Start();
            animationTimer.Start();
            gameTimer.Start();
            //spawnEnemies(enemySpawnCount, EnemyType.Type1);
            spawnEnemies(enemySpawnCount, EnemyType.Type2);
        }

        private GameConfiguration LoadKeybindsFromFile(string path)
        {
            var gameConfiguration = new GameConfiguration();
            try
            {
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
                            if(bool.TryParse(keyName, out bool result))
                            {
                                gameConfiguration.MouseControl = result;
                            } 
                        }
                    }
                }
            }
            
            catch (Exception) 
            {
                gameConfiguration.FireKey = DefaultFireKey;
                gameConfiguration.MoveLeftKey = DefaultMoveLeftKey;
                gameConfiguration.MoveRightKey = DefaultMoveRightKey;
                gameConfiguration.MoveUpKey = DefaultMoveUptKey;
                gameConfiguration.MoveDownKey = DefaultMoveDownKey;
                gameConfiguration.MouseControl = DefaultMouseControl;
            }
            return gameConfiguration;
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            Rect playerHitBox = new Rect(Canvas.GetLeft(player),Canvas.GetTop(player), player.Width,player.Height); //player hitbox a aktualizácia UI
            score.Content = "Score: " + scorePoints + "     "+totalEnemies;
            lives.Content = "Lives: " + livesCount;

            if (goLeft == true && Canvas.GetLeft(player) > 0)       //keyboard input
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) - 5);
            }

            if (goRight == true && Canvas.GetLeft(player) + playerShipWidth < Application.Current.MainWindow.Width)
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) + 5);
            }

            if (goUp == true && Canvas.GetTop(player) > 0)
            {
                Canvas.SetTop(player, Canvas.GetTop(player) - 2);
            }

            if (goDown == true && Canvas.GetTop(player) + playerShipHeight < Application.Current.MainWindow.Height)
            {
                Canvas.SetTop(player, Canvas.GetTop(player) + 2);
            }

            if (gameConfiguration.MouseControl)
            {
                Point mousePosition = Mouse.GetPosition(myCanvas);
                double newX = mousePosition.X - playerShipWidth / 2;
                double newY = mousePosition.Y - playerShipHeight / 2;

                newX = Math.Max(0, Math.Min(Application.Current.MainWindow.Width - playerShipWidth, newX));
                newY = Math.Max(0, Math.Min(Application.Current.MainWindow.Height - playerShipHeight, newY));

                Canvas.SetLeft(player, newX);
                Canvas.SetTop(player, newY);
            }

            foreach (var item in myCanvas.Children.OfType<Rectangle>())
            {
                if ((string)item.Tag == "projectile")
                {
                    Canvas.SetTop(item, Canvas.GetTop(item) - 20);

                    if (Canvas.GetTop(item) < 10)
                    {
                        itemsToRemove.Add(item);
                    }

                    Rect projectileHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);

                    foreach (var enemy in enemyList)
                    {
                        if (enemy != null && myCanvas.Children.Contains(enemy.Rectangle))
                        {
                            Rect enemyHitBox = new Rect(Canvas.GetLeft(enemy.Rectangle), Canvas.GetTop(enemy.Rectangle), enemy.Rectangle.Width, enemy.Rectangle.Height);

                            if (projectileHitBox.IntersectsWith(enemyHitBox))
                            {
                                itemsToRemove.Add(item);
                                enemy.Health -= 1;

                                if (enemy.Health <= 0)
                                {
                                    itemsToRemove.Add(enemy.Rectangle);
                                    totalEnemies -= 1;
                                    scorePoints += enemy.PointValue;

                                    if (scorePoints % extraLifeThreshold == 0)
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
                    Canvas.SetLeft(item, Canvas.GetLeft(item) + enemySpeed);

                    if (Canvas.GetLeft(item) > Application.Current.MainWindow.Width)
                    {
                        Canvas.SetLeft(item, -80);
                        Canvas.SetTop(item, Canvas.GetTop(item) + (item.Height + 10));
                    }

                    Rect enemyHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);

                    if (playerHitBox.IntersectsWith(enemyHitBox))
                    {
                        if (livesCount <= 0)
                        {
                            player.Visibility = Visibility.Collapsed;
                            for (int i = 0; i < 3; i++)
                            {
                                explode();
                            }
                            showGameOver("You died");
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
                    Canvas.SetTop(item, Canvas.GetTop(item) + 10);
                    if (Canvas.GetTop(item) > Application.Current.MainWindow.Height)
                    {
                        itemsToRemove.Add(item);
                    }

                    Rect enemyProjectileHitBox = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);

                    if (playerHitBox.IntersectsWith(enemyProjectileHitBox))
                    {
                        if (livesCount <= 0)
                        {
                            player.Visibility = Visibility.Collapsed;
                            for (int i = 0; i < 3; i++)
                            {
                                explode();
                            }
                            showGameOver("You died");
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


            foreach (Rectangle item in itemsToRemove) 
             { 
                 myCanvas.Children.Remove(item);
             }
             
             if (totalEnemies<1)
             {
                showGameOver("Victory");
             }
        }

        private void EnemyShootingTimer_Tick(object sender, EventArgs e)
        {
            if (enemyList.Count > 0)
            {
                List<Enemy> aliveEnemies = enemyList.Where(enemy => myCanvas.Children.Contains(enemy.Rectangle)).ToList();

                if (aliveEnemies.Count > 0 && !isPaused && !gameOver)
                {
                    int randomEnemyIndex = random.Next(aliveEnemies.Count); //vyberie nahodneho nepriatela zo zoznamu zijucich nepriatelov a ten vystrelí
                    Enemy randomEnemy = aliveEnemies[randomEnemyIndex];
                    enemyProjectileSpawner(Canvas.GetLeft(randomEnemy.Rectangle) + randomEnemy.Rectangle.Width / 2, Canvas.GetTop(randomEnemy.Rectangle) + randomEnemy.Rectangle.Height);
                }
            }
        }

        private void KeyIsDown(object sender, KeyEventArgs e)
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

            if (e.Key == Key.Enter && gameOver==true)
            {
                ResetGame();
            }

            if (e.Key == Key.Escape && !gameOver)
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

        private void KeyIsUp(object sender, KeyEventArgs e)     //keyboard bindy
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
        {
            if (e.ChangedButton == MouseButton.Left && !gameOver && !isPaused && gameConfiguration.MouseControl)
            {
                Shoot();
            }
        }

        private void Shoot() 
        {
            Rectangle newBullet = new Rectangle
            {
                Tag = "projectile",
                Height = 20,
                Width = 5,
                Fill = Brushes.White,
                Stroke = Brushes.Blue,
            };

            Canvas.SetTop(newBullet, Canvas.GetTop(player) - newBullet.Height);         
            Canvas.SetLeft(newBullet, Canvas.GetLeft(player) + playerShipWidth / 4);        //spawnovanie projektilu v strede hraca

            myCanvas.Children.Add(newBullet);
        }

        private void AnimationTick(object sender, EventArgs e)      //animovana explozia
        {
            if (explosionAnimationProgress < explosionAnimationFrames)
            {
                foreach (var item in myCanvas.Children.OfType<Rectangle>().Where(item => (string)item.Tag == "playerExplosion")) // for loop, ktory prehra animaciu vybuchu v poradi
                {
                    ((ImageBrush)item.Fill).ImageSource = explosionFrames[explosionAnimationProgress];
                }
                explosionAnimationProgress++;
            }
            else
            {
                var explosionRectangles = myCanvas.Children.OfType<Rectangle>().Where(item => (string)item.Tag == "playerExplosion").ToList();
                foreach (var item in explosionRectangles)
                {
                    myCanvas.Children.Remove(item);
                }
                explosionAnimationProgress = 0; 
            }
        }

        private void explode()
        {   
            Rectangle playerExplosion = new Rectangle
            {
                Tag = "playerExplosion",
                Height = 200,
                Width = 200,
                Fill = new ImageBrush
                {
                    ImageSource = explosionFrames[0]
                }
            };

            Canvas.SetTop(playerExplosion, Canvas.GetTop(player) - playerShipHeight/2) ; // umiestnenie výbuchu do stredu hráča
            Canvas.SetLeft(playerExplosion, Canvas.GetLeft(player) - playerShipWidth/2) ;

            myCanvas.Children.Add(playerExplosion);
            explosionAnimationProgress = 0;
        }

        public class Enemy
        {
            public Rectangle Rectangle { get; set; }
            public int Health { get; set; }
            public int PointValue { get; set; }
            public Enemy(Rectangle rectangle, int health, int pointvalue) 
            {
                Rectangle = rectangle;
                Health = health;
                PointValue = pointvalue;
            }
        }

        private void enemyProjectileSpawner(double x, double y)
        {
            Rectangle enemyProjectile = new Rectangle
            {
                Tag = "enemyProjectile",
                Height = 40,
                Width = 10,
                Fill = Brushes.Black,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
            };

            Canvas.SetTop(enemyProjectile, y);
            Canvas.SetLeft(enemyProjectile, x);

            myCanvas.Children.Add(enemyProjectile);
        }

        private void spawnEnemies(int limit, EnemyType enemyType)
        {
            int left = 0;
            totalEnemies = limit;

            for (int i = 0; i < limit; i++) 
            { 
                ImageBrush enemySkin = new ImageBrush();

                Rectangle newEnemy = new Rectangle
                {
                    Tag = "enemy",
                    Height = defaultEnemyHeight,
                    Width = defaultEnemyWidth,
                    Fill = enemySkin
                };

                int enemyHealth = 0;
                int enemyPointValue = 0;
                switch (enemyType)
                {
                    case EnemyType.Type1:
                        enemyHealth = 2;
                        enemyPointValue = 100;
                        enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader1.webp"));
                        break;

                    case EnemyType.Type2:
                        enemyHealth = 3;
                        enemyPointValue = 300;
                        enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader2.webp"));
                        break;

                    case EnemyType.Type3:
                        enemyHealth = 4;
                        enemyPointValue = 400;
                        enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader3.webp"));
                        break;
                }

                Enemy enemy = new Enemy(newEnemy, enemyHealth, enemyPointValue);

                Canvas.SetTop(enemy.Rectangle, 30);
                Canvas.SetLeft(enemy.Rectangle, left);
                myCanvas.Children.Add(enemy.Rectangle);
                left -= 60;

                enemyList.Add(enemy);

                enemyImages++;

            }
        }

        private void showGameOver(string msg)
        {
            gameOver = true;
            gameTimer.Stop();
            score.Content += " " + msg + "!   Enter to play again";
        }

        private void SwitchToMainMenu() // funguje, ale potom tlacitka nefunguju
        {
            gameTimer.Stop();
            gameOver = true;
            this.Content = mainMenu;
            mainMenu.Focus();
        }

        private void PauseGame() 
        {
            isPaused = true;
            gameTimer.Stop();
            myCanvas.Children.Add(pauseMenu);
        }

        private void ResumeGame()
        {
            isPaused = false;
            myCanvas.Children.Remove(pauseMenu);
            myCanvas.Focus();
            gameTimer.Start();
        }

        private void ResetGame()    //resetovanie hry na default hodnotach
        {
            if (pauseMenu is not null)
            {
                myCanvas.Children.Remove(pauseMenu);
            }
            myCanvas.Focus();  
            isPaused = false; // toto prerobit a miesto globalnych premennych pouzivat konstanty a funkcie ktore si budu predavat parametre
            gameOver = false;
            totalEnemies = 0;
            scorePoints = 0;
            livesCount = 3;
            enemySpeed = 6;
            itemsToRemove.Clear(); 
            player.Visibility = Visibility.Visible;

            foreach (var item in myCanvas.Children.OfType<Rectangle>().ToList()) //vymazanie nepotrebných assetov
            {
                if ((string)item.Tag == "enemy" || (string)item.Tag == "projectile" || (string)item.Tag == "enemyProjectile")
                {
                    myCanvas.Children.Remove(item);
                }
            }

            StartGame();
        }

    }
}
