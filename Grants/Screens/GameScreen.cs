using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Grants.Screens;

public enum ScreenType
{
    MainMenu,
    FighterSelect,
    Fight,
    PostMatch,
    UpgradeTree,
    Profile,
    CharacterBuilder,
}

/// <summary>
/// Base class for all screens. Each screen handles its own Update/Draw lifecycle.
/// </summary>
public abstract class GameScreen
{
    protected Game1 Game { get; private set; } = null!;

    public virtual void Initialize(Game1 game)
    {
        Game = game;
    }

    public virtual void OnEnter(object? data = null) { }
    public virtual void OnExit() { }

    public abstract void Update(GameTime gameTime);
    public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

    protected void SwitchTo(ScreenType screen, object? data = null) =>
        Game.SwitchScreen(screen, data);
}
