using MicroEngine.Core.Math;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Helper class for automatic text layout and positioning.
/// Eliminates manual coordinate tracking when rendering multiple lines of text.
/// </summary>
/// <remarks>
/// This helper maintains a current Y position and automatically advances it
/// after each text draw operation, making UI text rendering much more ergonomic.
/// </remarks>
public sealed class TextLayoutHelper
{
    private readonly IRenderer2D _renderer;
    private readonly float _startX;
    private readonly float _startY;
    private float _currentY;
    private float _defaultLineHeight;

    /// <summary>
    /// Gets the current X position for text rendering.
    /// </summary>
    public float CurrentX { get; private set; }

    /// <summary>
    /// Gets the current Y position for text rendering.
    /// </summary>
    public float CurrentY => _currentY;

    /// <summary>
    /// Gets or sets the default line height used for automatic spacing.
    /// When set to 0 or negative, line height is calculated as fontSize + 6.
    /// </summary>
    public float DefaultLineHeight
    {
        get => _defaultLineHeight;
        set => _defaultLineHeight = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextLayoutHelper"/> class.
    /// </summary>
    /// <param name="renderer">The render backend to use for drawing text.</param>
    /// <param name="startX">Starting X position for text.</param>
    /// <param name="startY">Starting Y position for text.</param>
    /// <param name="defaultLineHeight">Default line height. If 0 or negative, calculated automatically.</param>
    public TextLayoutHelper(IRenderer2D renderer, float startX, float startY, float defaultLineHeight = 20f)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _startX = startX;
        _startY = startY;
        _currentY = startY;
        CurrentX = startX;
        _defaultLineHeight = defaultLineHeight;
    }

    /// <summary>
    /// Draws text at the current position and automatically advances Y position.
    /// </summary>
    /// <param name="text">Text to draw.</param>
    /// <param name="fontSize">Font size.</param>
    /// <param name="color">Text color.</param>
    /// <param name="customLineHeight">Optional custom line height for this text. If null, uses DefaultLineHeight.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper DrawText(string text, int fontSize, Color color, float? customLineHeight = null)
    {
        _renderer.DrawText(text, new Vector2(CurrentX, _currentY), fontSize, color);
        
        var lineHeight = customLineHeight ?? (_defaultLineHeight > 0 ? _defaultLineHeight : fontSize + 6);
        _currentY += lineHeight;
        
        return this;
    }

    /// <summary>
    /// Adds vertical spacing without drawing any text.
    /// </summary>
    /// <param name="spacing">Amount of vertical space to add.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper AddSpacing(float spacing)
    {
        _currentY += spacing;
        return this;
    }

    /// <summary>
    /// Resets the Y position to the starting Y position.
    /// </summary>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper ResetY()
    {
        _currentY = _startY;
        return this;
    }

    /// <summary>
    /// Resets both X and Y positions to their starting values.
    /// </summary>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper Reset()
    {
        CurrentX = _startX;
        _currentY = _startY;
        return this;
    }

    /// <summary>
    /// Sets the current X position (useful for creating columns).
    /// </summary>
    /// <param name="x">New X position.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper SetX(float x)
    {
        CurrentX = x;
        return this;
    }

    /// <summary>
    /// Sets the current Y position.
    /// </summary>
    /// <param name="y">New Y position.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper SetY(float y)
    {
        _currentY = y;
        return this;
    }

    /// <summary>
    /// Sets both X and Y positions.
    /// </summary>
    /// <param name="x">New X position.</param>
    /// <param name="y">New Y position.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper SetPosition(float x, float y)
    {
        CurrentX = x;
        _currentY = y;
        return this;
    }

    /// <summary>
    /// Starts a new column at the specified X offset from the start position.
    /// Resets Y to the starting Y position.
    /// </summary>
    /// <param name="xOffset">X offset from the starting X position.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper NewColumn(float xOffset)
    {
        CurrentX = _startX + xOffset;
        _currentY = _startY;
        return this;
    }

    /// <summary>
    /// Draws a section with a title and optional spacing after.
    /// </summary>
    /// <param name="title">Section title.</param>
    /// <param name="titleFontSize">Title font size.</param>
    /// <param name="titleColor">Title color.</param>
    /// <param name="spacingAfter">Spacing to add after the title.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper DrawSection(string title, int titleFontSize, Color titleColor, float spacingAfter = 5f)
    {
        DrawText(title, titleFontSize, titleColor);
        if (spacingAfter > 0)
        {
            AddSpacing(spacingAfter);
        }
        return this;
    }

    /// <summary>
    /// Draws a key-value pair on the same line.
    /// </summary>
    /// <param name="key">Key text.</param>
    /// <param name="value">Value text.</param>
    /// <param name="fontSize">Font size.</param>
    /// <param name="keyColor">Key color.</param>
    /// <param name="valueColor">Value color.</param>
    /// <param name="keyValueSpacing">Spacing between key and value.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextLayoutHelper DrawKeyValue(
        string key, 
        string value, 
        int fontSize, 
        Color keyColor, 
        Color valueColor, 
        float keyValueSpacing = 10f)
    {
        _renderer.DrawText(key, new Vector2(CurrentX, _currentY), fontSize, keyColor);
        
        // Simple approximation: assume each character is roughly fontSize * 0.6 wide
        var keyWidth = key.Length * fontSize * 0.6f;
        var valueX = CurrentX + keyWidth + keyValueSpacing;
        
        _renderer.DrawText(value, new Vector2(valueX, _currentY), fontSize, valueColor);
        
        var lineHeight = _defaultLineHeight > 0 ? _defaultLineHeight : fontSize + 6;
        _currentY += lineHeight;
        
        return this;
    }
}
