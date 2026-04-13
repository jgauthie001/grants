using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

    // Resolution presets: (width, height)
    private static readonly (int W, int H)[] ResolutionPresets =
    {
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
    };
    private int _resolutionIndex = 2; // default: 1920×1080
    private KeyboardState _prevKeys;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth  = ResolutionPresets[_resolutionIndex].W;
        _graphics.PreferredBackBufferHeight = ResolutionPresets[_resolutionIndex].H;
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
        var keys = Keyboard.GetState();

        // F9 = smaller preset, F10 = larger preset, F11 = fullscreen toggle
        if (keys.IsKeyDown(Keys.F9) && !_prevKeys.IsKeyDown(Keys.F9))
        {
            _resolutionIndex = Math.Max(0, _resolutionIndex - 1);
            ApplyResolution();
        }
        else if (keys.IsKeyDown(Keys.F10) && !_prevKeys.IsKeyDown(Keys.F10))
        {
            _resolutionIndex = Math.Min(ResolutionPresets.Length - 1, _resolutionIndex + 1);
            ApplyResolution();
        }
        else if (keys.IsKeyDown(Keys.F11) && !_prevKeys.IsKeyDown(Keys.F11))
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            _graphics.ApplyChanges();
        }

        _prevKeys = keys;
        _currentScreen.Update(gameTime);
        base.Update(gameTime);
    }

    private void ApplyResolution()
    {
        var (w, h) = ResolutionPresets[_resolutionIndex];
        _graphics.PreferredBackBufferWidth  = w;
        _graphics.PreferredBackBufferHeight = h;
        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _currentScreen.Draw(gameTime, _spriteBatch);
        base.Draw(gameTime);
    }
}
