using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Grants.Engine;
using Grants.Models.Match;
using Grants.Screens;

namespace Grants;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    // Shared resources exposed to screens
    public SpriteFont DefaultFont { get; private set; } = null!;
    public SpriteFont SmallFont { get; private set; } = null!;
    public Texture2D Pixel { get; private set; } = null!;
    public PlayerProfile PlayerProfile { get; private set; } = null!;

    private readonly Dictionary<ScreenType, GameScreen> _screens = new();
    private GameScreen _currentScreen = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Register all screens
        _screens[ScreenType.MainMenu]          = new MainMenuScreen();
        _screens[ScreenType.FighterSelect]     = new FighterSelectScreen();
        _screens[ScreenType.StageSelect]       = new StageSelectScreen();
        _screens[ScreenType.Fight]             = new FightScreen();
        _screens[ScreenType.PostMatch]         = new PostMatchScreen();
        _screens[ScreenType.UpgradeTree]       = new UpgradeTreeScreen();
        _screens[ScreenType.UpgradeSelection]  = new UpgradeSelectionScreen();
        _screens[ScreenType.Profile]           = new ProfileScreen();
        _screens[ScreenType.CharacterBuilder]  = new CharacterBuilderScreen();
        _screens[ScreenType.KeywordEditor]     = new KeywordEditorScreen();

        foreach (var screen in _screens.Values)
            screen.Initialize(this);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        DefaultFont = Content.Load<SpriteFont>("Fonts/DefaultFont");
        SmallFont   = Content.Load<SpriteFont>("Fonts/SmallFont");

        Pixel = new Texture2D(GraphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });

        // Load or create player profile
        PlayerProfile = UpgradeEngine.LoadOrCreateProfile("player1", "Player 1_pl");

        // Start at main menu
        _currentScreen = _screens[ScreenType.MainMenu];
        _currentScreen.OnEnter();
    }

    public void SwitchScreen(ScreenType type, object? data = null)
    {
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_debug.log");
        try
        {
            using (var writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Switching to screen: {type}");
                writer.Flush();
            }
        }
        catch { }
        
        _currentScreen.OnExit();
        _currentScreen = _screens[type];
        
        try
        {
            using (var writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Calling OnEnter for {type}");
                writer.Flush();
            }
        }
        catch { }
        
        _currentScreen.OnEnter(data);
        
        try
        {
            using (var writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] OnEnter completed for {type}");
                writer.Flush();
            }
        }
        catch { }
    }

    protected override void Update(GameTime gameTime)
    {
        _currentScreen.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _currentScreen.Draw(gameTime, _spriteBatch);
        base.Draw(gameTime);
    }
}
