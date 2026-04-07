using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Upgrades;
using Grants.Fighters.Grants;

namespace Grants.Screens;

/// <summary>
/// Character builder screen. Allows editing fighter cards and their stats.
/// Select a fighter, then a card type (Generic/Unique/Special), then a specific card to edit.
/// </summary>
public class CharacterBuilderScreen : GameScreen
{
    private enum EditMode { FighterSelect, CreateCharacter, CardTypeSelect, CardSelect, StatEdit, CardNameEdit, KeywordEdit, KeywordValueEdit, UpgradeSelect }

    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;

    private List<FighterDefinition> _fighters = new();
    private int _fighterIndex = 0;
    private FighterDefinition? _selectedFighter = null;

    private string[] _cardTypes = { "Generic", "Unique", "Special" };
    private int _cardTypeIndex = 0;

    private List<CardBase> _currentCards = new();
    private int _cardIndex = 0;
    private CardBase? _selectedCard = null;

    // Stat editing state
    private string[] _editableStats = { "Power", "Defense", "Speed", "Movement", "Cooldown", "Name", "Keywords", "Upgrades" };
    private int _statIndex = 0;

    // Keyword editing
    private CardKeywordValue? _editingKeyword = null;
    private int _keywordIndex = 0;
    private string _keywordInputBuffer = "";
    private int _keywordValueEdit = 1;

    // Character creation
    private string _newCharacterName = "";

    // Upgrade selection
    private List<CardBase> _cardsNeedingUpgrades = new();
    private int _upgradeIndex = 0;
    private int _upgradesSelected = 0;

    private EditMode _mode = EditMode.FighterSelect;
    private KeyboardState _prevKeys;
    private Keys? _lastTextInputKey = null;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;

        _fighters = new List<FighterDefinition>
        {
            GrantsFighter.CreateDefinition(),
        };

        _mode = EditMode.FighterSelect;
        _fighterIndex = 0;
        _statIndex = 0;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        try
        {
            switch (_mode)
            {
                case EditMode.FighterSelect:
                    UpdateFighterSelect(keys);
                    break;
                case EditMode.CreateCharacter:
                    UpdateCreateCharacter(keys);
                    break;
                case EditMode.CardTypeSelect:
                    UpdateCardTypeSelect(keys);
                    break;
                case EditMode.CardSelect:
                    UpdateCardSelect(keys);
                    break;
                case EditMode.StatEdit:
                    UpdateStatEdit(keys);
                    break;
                case EditMode.CardNameEdit:
                    UpdateCardNameEdit(keys);
                    break;
                case EditMode.KeywordEdit:
                    UpdateKeywordEdit(keys);
                    break;
                case EditMode.KeywordValueEdit:
                    UpdateKeywordValueEdit(keys);
                    break;
                case EditMode.UpgradeSelect:
                    UpdateUpgradeSelect(keys);
                    break;
            }
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_debug.log");
            using (var writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CharacterBuilder UPDATE ERROR: {ex.Message}\n{ex.StackTrace}");
                writer.Flush();
            }
        }

        // Backspace exits to main menu from FighterSelect only
        if (_mode == EditMode.FighterSelect && IsPressed(keys, _prevKeys, Keys.Back))
            SwitchTo(ScreenType.MainMenu);

        _prevKeys = keys;
    }

    private void UpdateFighterSelect(KeyboardState keys)
    {
        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _fighterIndex = (_fighterIndex - 1 + _fighters.Count + 1) % (_fighters.Count + 1);

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _fighterIndex = (_fighterIndex + 1) % (_fighters.Count + 1);

        if (IsPressed(keys, _prevKeys, Keys.Enter))
        {
            // Last index is "Create New Fighter"
            if (_fighterIndex == _fighters.Count)
            {
                _mode = EditMode.CreateCharacter;
                _newCharacterName = "";
            }
            else
            {
                _selectedFighter = _fighters[_fighterIndex];
                _mode = EditMode.CardTypeSelect;
                _cardTypeIndex = 0;
            }
        }
    }

    private void UpdateCreateCharacter(KeyboardState keys)
    {
        // Process only newly pressed keys (not held keys)
        var pressedKeys = keys.GetPressedKeys();
        
        foreach (var key in pressedKeys)
        {
            // Skip if this key was already pressed last frame
            if (key == _lastTextInputKey)
                continue;

            if (key == Keys.Enter && _newCharacterName.Length > 0)
            {
                // Create a new fighter based on Grants template
                var newFighter = CreateNewFighterFromTemplate(_newCharacterName);
                _fighters.Add(newFighter);
                _selectedFighter = newFighter;
                _mode = EditMode.CardTypeSelect;
                _cardTypeIndex = 0;
                _lastTextInputKey = null;
                return;
            }
            else if (key == Keys.Back)
            {
                if (_newCharacterName.Length > 0)
                {
                    _newCharacterName = _newCharacterName.Substring(0, _newCharacterName.Length - 1);
                }
                else
                {
                    _mode = EditMode.FighterSelect;
                    _newCharacterName = "";
                    _lastTextInputKey = null;
                    return;
                }
            }
            else if (key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)(key - Keys.A + 'A');
                if (keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift))
                    _newCharacterName += c;
                else
                    _newCharacterName += char.ToLower(c);
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                _newCharacterName += (char)(key - Keys.D0 + '0');
            }
            else if (key == Keys.Space)
            {
                _newCharacterName += " ";
            }
        }

        // Track the last pressed key for repeat prevention
        _lastTextInputKey = pressedKeys.Length > 0 ? pressedKeys[0] : null;

        if (IsPressed(keys, _prevKeys, Keys.Escape))
        {
            _mode = EditMode.FighterSelect;
            _newCharacterName = "";
            _lastTextInputKey = null;
        }
    }

    private void UpdateCardTypeSelect(KeyboardState keys)
    {
        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _cardTypeIndex = (_cardTypeIndex - 1 + _cardTypes.Length) % _cardTypes.Length;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _cardTypeIndex = (_cardTypeIndex + 1) % _cardTypes.Length;

        if (IsPressed(keys, _prevKeys, Keys.Enter))
        {
            LoadCardsForType();
            _mode = EditMode.CardSelect;
            _cardIndex = 0;
        }

        if (IsPressed(keys, _prevKeys, Keys.Back))
        {
            _mode = EditMode.FighterSelect;
            _selectedFighter = null;
        }
    }

    private void UpdateCardSelect(KeyboardState keys)
    {
        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _cardIndex = (_cardIndex - 1 + _currentCards.Count) % _currentCards.Count;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _cardIndex = (_cardIndex + 1) % _currentCards.Count;

        if (IsPressed(keys, _prevKeys, Keys.Enter))
        {
            _selectedCard = _currentCards[_cardIndex];
            _mode = EditMode.StatEdit;
            _statIndex = 0;
        }

        if (IsPressed(keys, _prevKeys, Keys.Back))
        {
            _mode = EditMode.CardTypeSelect;
            _currentCards.Clear();
        }
    }

    private void UpdateStatEdit(KeyboardState keys)
    {
        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _statIndex = (_statIndex - 1 + _editableStats.Length) % _editableStats.Length;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _statIndex = (_statIndex + 1) % _editableStats.Length;

        // Modify numeric stat values
        if (_statIndex < 5)
        {
            if (IsPressed(keys, _prevKeys, Keys.Right) || IsPressed(keys, _prevKeys, Keys.D))
                ModifyStat(1);

            if (IsPressed(keys, _prevKeys, Keys.Left) || IsPressed(keys, _prevKeys, Keys.A))
                ModifyStat(-1);
        }

        // Edit Name
        if (IsPressed(keys, _prevKeys, Keys.Enter) && _statIndex == 5)
        {
            _keywordInputBuffer = _selectedCard?.Name ?? "";
            _mode = EditMode.CardNameEdit;
        }

        // Edit Keywords
        if (IsPressed(keys, _prevKeys, Keys.Enter) && _statIndex == 6)
        {
            _mode = EditMode.KeywordEdit;
            _keywordIndex = 0;
        }

        // Edit Upgrades (for all cards in the fighter)
        if (IsPressed(keys, _prevKeys, Keys.Enter) && _statIndex == 7)
        {
            _mode = EditMode.UpgradeSelect;
            _upgradeIndex = 0;
            _upgradesSelected = 0;
            _cardsNeedingUpgrades.Clear();
            // Will be populated on first UpdateUpgradeSelect() call
        }

        if (IsPressed(keys, _prevKeys, Keys.Back))
        {
            _mode = EditMode.CardSelect;
            _selectedCard = null;
        }
    }

    private void UpdateCardNameEdit(KeyboardState keys)
    {
        var pressedKeys = keys.GetPressedKeys();

        foreach (var key in pressedKeys)
        {
            // Skip if this key was already pressed last frame
            if (key == _lastTextInputKey)
                continue;

            if (key == Keys.Enter)
            {
                if (_selectedCard != null)
                    _selectedCard.Name = _keywordInputBuffer;
                _mode = EditMode.StatEdit;
                _lastTextInputKey = null;
                return;
            }
            else if (key == Keys.Back)
            {
                if (_keywordInputBuffer.Length > 0)
                {
                    _keywordInputBuffer = _keywordInputBuffer.Substring(0, _keywordInputBuffer.Length - 1);
                }
            }
            else if (key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)(key - Keys.A + 'A');
                if (keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift))
                    _keywordInputBuffer += c;
                else
                    _keywordInputBuffer += char.ToLower(c);
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                _keywordInputBuffer += (char)(key - Keys.D0 + '0');
            }
            else if (key == Keys.Space)
            {
                _keywordInputBuffer += " ";
            }
        }

        // Track the last pressed key for repeat prevention
        _lastTextInputKey = pressedKeys.Length > 0 ? pressedKeys[0] : null;

        if (IsPressed(keys, _prevKeys, Keys.Escape))
        {
            _mode = EditMode.StatEdit;
            _lastTextInputKey = null;
        }
    }

    private void UpdateKeywordEdit(KeyboardState keys)
    {
        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
        {
            _keywordIndex--;
            if (_keywordIndex < -1) _keywordIndex = _selectedCard?.Keywords.Count ?? 0;
        }

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
        {
            _keywordIndex++;
            if (_keywordIndex > (_selectedCard?.Keywords.Count ?? 0)) _keywordIndex = -1;
        }

        // Add new keyword
        if (IsPressed(keys, _prevKeys, Keys.Enter) && _keywordIndex == -1)
        {
            _mode = EditMode.KeywordValueEdit;
            _keywordValueEdit = 1;
            _editingKeyword = new CardKeywordValue(CardKeyword.Bleed, 1);
        }

        // Edit existing keyword
        if (IsPressed(keys, _prevKeys, Keys.Enter) && _keywordIndex >= 0 && _selectedCard != null)
        {
            _editingKeyword = _selectedCard.Keywords[_keywordIndex];
            _keywordValueEdit = _editingKeyword.Value;
            _mode = EditMode.KeywordValueEdit;
        }

        // Remove keyword
        if (IsPressed(keys, _prevKeys, Keys.Delete) && _keywordIndex >= 0 && _selectedCard != null)
        {
            _selectedCard.Keywords.RemoveAt(_keywordIndex);
            _keywordIndex = Math.Max(-1, _keywordIndex - 1);
        }

        if (IsPressed(keys, _prevKeys, Keys.Back))
            _mode = EditMode.StatEdit;
    }

    private void UpdateKeywordValueEdit(KeyboardState keys)
    {
        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
        {
            var nextKeyword = (int)_editingKeyword!.Keyword + 1;
            if (nextKeyword > 16) nextKeyword = 1;
            _editingKeyword.Keyword = (CardKeyword)nextKeyword;
        }

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
        {
            var prevKeyword = (int)_editingKeyword!.Keyword - 1;
            if (prevKeyword < 1) prevKeyword = 16;
            _editingKeyword.Keyword = (CardKeyword)prevKeyword;
        }

        if (IsPressed(keys, _prevKeys, Keys.Right) || IsPressed(keys, _prevKeys, Keys.D))
            _editingKeyword!.Value = Math.Min(10, _editingKeyword.Value + 1);

        if (IsPressed(keys, _prevKeys, Keys.Left) || IsPressed(keys, _prevKeys, Keys.A))
            _editingKeyword!.Value = Math.Max(1, _editingKeyword.Value - 1);

        if (IsPressed(keys, _prevKeys, Keys.Enter))
        {
            if (_selectedCard != null && _editingKeyword != null && !_selectedCard.Keywords.Contains(_editingKeyword))
                _selectedCard.Keywords.Add(_editingKeyword);
            _mode = EditMode.KeywordEdit;
            _editingKeyword = null;
        }

        if (IsPressed(keys, _prevKeys, Keys.Back))
        {
            _mode = EditMode.KeywordEdit;
            _editingKeyword = null;
        }
    }

    private FighterDefinition CreateNewFighterFromTemplate(string fighterName)
    {
        // Clone the Grants fighter as a template
        var template = GrantsFighter.CreateDefinition();
        
        return new FighterDefinition
        {
            Id = $"custom_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Name = fighterName,
            Description = "A custom fighter created in the Character Builder.",
            GenericCards = new List<GenericCard>(template.GenericCards),
            UniqueCards = new List<UniqueCard>(template.UniqueCards),
            SpecialCards = new List<SpecialCard>(template.SpecialCards),
            CriticalLocations = new List<BodyLocation>(template.CriticalLocations),
            KOThreshold = template.KOThreshold,
            RankedUnlockWins = template.RankedUnlockWins,
        };
    }

    private void LoadCardsForType()
    {
        if (_selectedFighter == null) return;

        _currentCards.Clear();
        switch (_cardTypeIndex)
        {
            case 0: // Generic
                _currentCards.AddRange(_selectedFighter.GenericCards.Cast<CardBase>());
                break;
            case 1: // Unique
                _currentCards.AddRange(_selectedFighter.UniqueCards.Cast<CardBase>());
                break;
            case 2: // Special
                _currentCards.AddRange(_selectedFighter.SpecialCards.Cast<CardBase>());
                break;
        }
    }

    private void ModifyStat(int delta)
    {
        if (_selectedCard == null) return;

        switch (_statIndex)
        {
            case 0: _selectedCard.BasePower = Math.Max(0, _selectedCard.BasePower + delta); break;
            case 1: _selectedCard.BaseDefense = Math.Max(0, _selectedCard.BaseDefense + delta); break;
            case 2: _selectedCard.BaseSpeed = _selectedCard.BaseSpeed + delta; break;
            case 3: _selectedCard.BaseMovement = Math.Max(0, _selectedCard.BaseMovement + delta); break;
            case 4: _selectedCard.BaseCooldown = Math.Max(1, _selectedCard.BaseCooldown + delta); break;
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        int cx = Game.GraphicsDevice.Viewport.Width / 2;

        sb.DrawString(_font, "Character Builder", new Vector2(cx - _font.MeasureString("Character Builder").X / 2, 30), Color.White);

        switch (_mode)
        {
            case EditMode.FighterSelect:
                DrawFighterSelect(sb);
                break;
            case EditMode.CreateCharacter:
                DrawCreateCharacter(sb);
                break;
            case EditMode.CardTypeSelect:
                DrawCardTypeSelect(sb);
                break;
            case EditMode.CardSelect:
                DrawCardSelect(sb);
                break;
            case EditMode.StatEdit:
                DrawStatEdit(sb);
                break;
            case EditMode.CardNameEdit:
                DrawCardNameEdit(sb);
                break;
            case EditMode.KeywordEdit:
                DrawKeywordEdit(sb);
                break;
            case EditMode.KeywordValueEdit:
                DrawKeywordValueEdit(sb);
                break;
            case EditMode.UpgradeSelect:
                DrawUpgradeSelect(sb);
                break;
        }

        sb.DrawString(_smallFont, "[Backspace] Main Menu", new Vector2(20, Game.GraphicsDevice.Viewport.Height - 30), Color.DimGray);

        sb.End();
    }

    private void DrawFighterSelect(SpriteBatch sb)
    {
        int x = 100, y = 100;
        sb.DrawString(_smallFont, "Select Fighter:", new Vector2(x, y), Color.White);
        y += 30;

        for (int i = 0; i < _fighters.Count; i++)
        {
            bool sel = i == _fighterIndex;
            Color c = sel ? Color.Yellow : Color.LightGray;
            string label = $"{(sel ? ">" : " ")} {_fighters[i].Name}";
            sb.DrawString(_smallFont, label, new Vector2(x, y + i * 20), c);
        }

        // Show "Create New Fighter" option
        bool createSel = _fighterIndex == _fighters.Count;
        Color createColor = createSel ? Color.Yellow : Color.LightGray;
        sb.DrawString(_smallFont, $"{(createSel ? ">" : " ")} [Create New Fighter]", new Vector2(x, y + _fighters.Count * 20), createColor);

        y += (_fighters.Count + 1) * 20 + 30;
        sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Select   [Backspace] Main Menu", new Vector2(x, y), Color.DimGray);
    }

    private void DrawCreateCharacter(SpriteBatch sb)
    {
        int x = 100, y = 150;
        sb.DrawString(_smallFont, "Create New Fighter:", new Vector2(x, y), Color.White);
        y += 30;
        sb.DrawString(_smallFont, "Enter name:", new Vector2(x, y), Color.LightGray);
        y += 25;
        sb.DrawString(_font, _newCharacterName + "|", new Vector2(x, y), Color.Yellow);
        y += 50;
        sb.DrawString(_smallFont, "[Type to name]  [Enter] Create  [Backspace] Back  [Escape] Cancel", new Vector2(x, y), Color.DimGray);
    }

    private void DrawCardTypeSelect(SpriteBatch sb)
    {
        int x = 100, y = 100;
        sb.DrawString(_smallFont, $"Fighter: {_selectedFighter?.Name}", new Vector2(x, y), Color.Yellow);
        y += 30;

        sb.DrawString(_smallFont, "Select Card Type:", new Vector2(x, y), Color.White);
        y += 30;

        for (int i = 0; i < _cardTypes.Length; i++)
        {
            bool sel = i == _cardTypeIndex;
            Color c = sel ? Color.Yellow : Color.LightGray;
            string label = $"{(sel ? ">" : " ")} {_cardTypes[i]}";
            sb.DrawString(_smallFont, label, new Vector2(x, y + i * 20), c);
        }

        y += _cardTypes.Length * 20 + 30;
        sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Select   [Backspace] Back", new Vector2(x, y), Color.DimGray);
    }

    private void DrawCardSelect(SpriteBatch sb)
    {
        int x = 100, y = 100;
        sb.DrawString(_smallFont, $"Fighter: {_selectedFighter?.Name} / {_cardTypes[_cardTypeIndex]} Cards", new Vector2(x, y), Color.Yellow);
        y += 30;

        for (int i = 0; i < _currentCards.Count; i++)
        {
            bool sel = i == _cardIndex;
            Color c = sel ? Color.Yellow : Color.LightGray;
            string label = $"{(sel ? ">" : " ")} {_currentCards[i].Name}";
            sb.DrawString(_smallFont, label, new Vector2(x, y + i * 20), c);
        }

        y += Math.Max(1, _currentCards.Count) * 20 + 30;
        sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Edit   [Backspace] Back", new Vector2(x, y), Color.DimGray);
    }

    private void DrawStatEdit(SpriteBatch sb)
    {
        if (_selectedCard == null) return;

        int x = 100, y = 100;
        sb.DrawString(_smallFont, $"Editing: {_selectedCard.Name}", new Vector2(x, y), Color.Yellow);
        y += 30;

        string upgradeSummary = _selectedFighter != null && _selectedFighter.CardUpgradeOptions.Count > 0
            ? "configured"
            : "not configured";

        var stats = new[]
        {
            ("Power", _selectedCard.BasePower.ToString()),
            ("Defense", _selectedCard.BaseDefense.ToString()),
            ("Speed", _selectedCard.BaseSpeed.ToString()),
            ("Movement", _selectedCard.BaseMovement.ToString()),
            ("Cooldown", _selectedCard.BaseCooldown.ToString()),
            ("Name", _selectedCard.Name),
            ("Keywords", _selectedCard.Keywords.Count + " added"),
            ("Upgrades", upgradeSummary),
        };

        for (int i = 0; i < stats.Length; i++)
        {
            bool sel = i == _statIndex;
            Color c = sel ? Color.Yellow : Color.LightGray;
            string arrow = (sel && i < 5) ? " <>" : "";
            string label = $"{(sel ? ">" : " ")} {stats[i].Item1}: {stats[i].Item2}{arrow}";
            sb.DrawString(_smallFont, label, new Vector2(x, y + i * 25), c);
        }

        y += stats.Length * 25 + 30;
        sb.DrawString(_smallFont, "[Up/Down] Select   [Left/Right] Adjust   [Enter] Edit   [Backspace] Back", new Vector2(x, y), Color.DimGray);
    }

    private void DrawCardNameEdit(SpriteBatch sb)
    {
        int x = 100, y = 150;
        sb.DrawString(_smallFont, "Edit Card Name:", new Vector2(x, y), Color.White);
        y += 30;
        sb.DrawString(_font, _keywordInputBuffer + "|", new Vector2(x, y), Color.Yellow);
        y += 50;
        sb.DrawString(_smallFont, "[Type to edit]  [Enter] Confirm  [Esc] Cancel", new Vector2(x, y), Color.DimGray);
    }

    private void DrawKeywordEdit(SpriteBatch sb)
    {
        if (_selectedCard == null) return;

        int x = 100, y = 100;
        sb.DrawString(_smallFont, "Keywords on Card:", new Vector2(x, y), Color.White);
        y += 30;

        // Show existing keywords
        for (int i = 0; i < _selectedCard.Keywords.Count; i++)
        {
            bool sel = i == _keywordIndex;
            Color c = sel ? Color.Yellow : Color.LightGray;
            string label = $"{(sel ? ">" : " ")} {_selectedCard.Keywords[i]}  [Del to remove]";
            sb.DrawString(_smallFont, label, new Vector2(x, y + i * 20), c);
        }

        y += Math.Max(1, _selectedCard.Keywords.Count) * 20;

        // Add new keyword option
        bool addSel = _keywordIndex == -1;
        Color addColor = addSel ? Color.Yellow : Color.LightGray;
        sb.DrawString(_smallFont, $"{(addSel ? ">" : " ")} [Add New Keyword]", new Vector2(x, y + 20), addColor);

        y += 50;
        sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Add/Edit   [Delete] Remove   [Backspace] Back", new Vector2(x, y), Color.DimGray);
    }

    private void DrawKeywordValueEdit(SpriteBatch sb)
    {
        if (_editingKeyword == null) return;

        int x = 100, y = 150;
        sb.DrawString(_smallFont, "Edit Keyword:", new Vector2(x, y), Color.White);
        y += 30;

        sb.DrawString(_smallFont, $"Type: {_editingKeyword.Keyword}", new Vector2(x, y), Color.Yellow);
        y += 25;
        sb.DrawString(_smallFont, "[Up/Down] Change Type", new Vector2(x, y), Color.DimGray);
        y += 25;

        sb.DrawString(_smallFont, $"Value: {_editingKeyword.Value}", new Vector2(x, y), Color.Yellow);
        y += 25;
        sb.DrawString(_smallFont, "[Left/Right] Change Value", new Vector2(x, y), Color.DimGray);
        y += 30;

        sb.DrawString(_smallFont, "[Enter] Confirm   [Backspace] Cancel", new Vector2(x, y), Color.DimGray);
    }

    private void UpdateUpgradeSelect(KeyboardState keys)
    {
        if (_selectedFighter == null) return;

        // Initialize cards needing upgrades on first entry
        if (_cardsNeedingUpgrades.Count == 0)
        {
            _cardsNeedingUpgrades.AddRange(_selectedFighter.AllCards.ToList());
        }

        // Navigate upgrade options with Up/Down
        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _upgradeIndex = Math.Max(0, _upgradeIndex - 1);

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
        {
            var currentCard = _upgradesSelected < _cardsNeedingUpgrades.Count ? _cardsNeedingUpgrades[_upgradesSelected] : null;
            if (currentCard != null && _selectedFighter.CardUpgradeOptions.TryGetValue(currentCard.Id, out var options))
            {
                _upgradeIndex = Math.Min(options.Count - 1, _upgradeIndex + 1);
            }
        }

        // Select upgrade with Enter
        if (IsPressed(keys, _prevKeys, Keys.Enter) && _upgradesSelected < _cardsNeedingUpgrades.Count)
        {
            var currentCard = _cardsNeedingUpgrades[_upgradesSelected];
            if (_selectedFighter.CardUpgradeOptions.TryGetValue(currentCard.Id, out var options) && _upgradeIndex < options.Count)
            {
                // Apply the selected upgrade to the card
                var selectedUpgrade = options[_upgradeIndex];
                ApplyUpgrade(currentCard, selectedUpgrade);

                _upgradesSelected++;
                _upgradeIndex = 0;

                // If all cards have upgrades, exit out
                if (_upgradesSelected >= _cardsNeedingUpgrades.Count)
                {
                    // All upgrades selected, return to fighter select
                    _mode = EditMode.FighterSelect;
                    _upgradeIndex = 0;
                    _upgradesSelected = 0;
                    _cardsNeedingUpgrades.Clear();
                }
            }
        }

        // Backspace cancels and goes back
        if (IsPressed(keys, _prevKeys, Keys.Back))
        {
            _mode = EditMode.StatEdit;
            _upgradeIndex = 0;
            _upgradesSelected = 0;
            _cardsNeedingUpgrades.Clear();
        }
    }

    private void ApplyUpgrade(CardBase card, CardUpgradeOption upgrade)
    {
        if (upgrade.Type == CardUpgradeType.PowerBonus)
            card.BasePower += upgrade.StatBonus;
        else if (upgrade.Type == CardUpgradeType.DefenseBonus)
            card.BaseDefense += upgrade.StatBonus;
        else if (upgrade.Type == CardUpgradeType.SpeedBonus)
            card.BaseSpeed += upgrade.StatBonus;
        else if (upgrade.Type == CardUpgradeType.MovementBonus)
            card.BaseMovement += upgrade.StatBonus;
        else if (upgrade.Type == CardUpgradeType.CooldownReduction)
            card.BaseCooldown = Math.Max(1, card.BaseCooldown + upgrade.StatBonus);
        else if (upgrade.Type == CardUpgradeType.AddKeyword && upgrade.KeywordToAdd != CardKeyword.None)
        {
            // Check if this keyword already exists
            var existingKeyword = card.Keywords.FirstOrDefault(kv => kv.Keyword == upgrade.KeywordToAdd);
            if (existingKeyword != null)
            {
                existingKeyword.Value += upgrade.KeywordValue;
            }
            else
            {
                card.Keywords.Add(new CardKeywordValue(upgrade.KeywordToAdd, upgrade.KeywordValue));
            }
        }
    }

    private void DrawUpgradeSelect(SpriteBatch sb)
    {
        if (_selectedFighter == null) return;

        int x = 100, y = 100;
        sb.DrawString(_smallFont, $"Upgrade Cards for {_selectedFighter.Name}", new Vector2(x, y), Color.White);
        y += 30;
        sb.DrawString(_smallFont, $"Progress: {_upgradesSelected} / {(_cardsNeedingUpgrades.Count > 0 ? _cardsNeedingUpgrades.Count : "...")}",
            new Vector2(x, y), Color.Yellow);
        y += 30;

        // Show current card
        if (_upgradesSelected < _cardsNeedingUpgrades.Count)
        {
            var currentCard = _cardsNeedingUpgrades[_upgradesSelected];
            sb.DrawString(_smallFont, $"Select upgrade for: {currentCard.Name}", new Vector2(x, y), Color.Yellow);
            y += 30;

            // Show upgrade options if available
            if (_selectedFighter.CardUpgradeOptions.TryGetValue(currentCard.Id, out var options))
            {
                for (int i = 0; i < options.Count; i++)
                {
                    bool sel = i == _upgradeIndex;
                    Color c = sel ? Color.Yellow : Color.LightGray;
                    string label = $"{(sel ? ">" : " ")} {options[i].Description}";
                    sb.DrawString(_smallFont, label, new Vector2(x, y + i * 20), c);
                }
            }
            else
            {
                sb.DrawString(_smallFont, "(No upgrades available)", new Vector2(x, y), Color.DimGray);
            }

            y += 50;
            sb.DrawString(_smallFont, "[Up/Down] Select Upgrade   [Enter] Choose   [Backspace] Back", new Vector2(x, y), Color.DimGray);
        }
        else
        {
            sb.DrawString(_smallFont, "All upgrades selected!", new Vector2(x, y), Color.Yellow);
            y += 30;
            sb.DrawString(_smallFont, "[Backspace] Return", new Vector2(x, y), Color.DimGray);
        }
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);
}

