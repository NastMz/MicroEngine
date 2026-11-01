using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>A save slot: what is saved, loading it back, saving, deleting, and where the file lives.</summary>
public sealed class SaveExample(string? saveDirectory) : Scene
{
    #region data
    /// <summary>The saved data: any class System.Text.Json can write (public properties with getters and setters).</summary>
    public sealed class Progress
    {
        public int Coins { get; set; }
        public List<string> Visited { get; set; } = [];
        public DateTime SavedAt { get; set; }
    }
    #endregion

    #region use
    private SaveStore _store = null!;
    private Progress _progress = null!;
    private string _status = "";

    public override void Enter()
    {
        // One store per game: the id names the save folder (or the browser's storage keys).
        _store = new SaveStore("MicroEngineExamples", saveDirectory);
        _progress = _store.Load<Progress>("example") ?? new Progress();   // null: no save yet, or unreadable
        _status = _store.Exists("example") ? "LOADED" : "NO SAVE YET";
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("right")) _progress.Coins++;
        if (Input.Pressed("confirm"))
        {
            _progress.SavedAt = DateTime.UtcNow;
            _progress.Visited.Add($"room {_progress.Visited.Count + 1}");
            // Save writes the whole slot at once; false means the disk or the browser refused (keep playing).
            _status = _store.Save("example", _progress) ? "SAVED" : "COULD NOT SAVE";
        }
        if (Input.Pressed("cancel"))
        {
            _store.Delete("example");
            _progress = new Progress();
            _status = "DELETED";
        }
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        Font.Draw(r, $"COINS {_progress.Coins}   ROOMS {_progress.Visited.Count}", new Vector2(16, 30), Color.White);
        Font.Draw(r, $"STATUS: {_status}", new Vector2(16, 44), UiTheme.Default.Accent);
        Font.Draw(r, "RIGHT: +1 COIN   CONFIRM: SAVE   CANCEL: DELETE", new Vector2(16, 64), Color.White);
        var folder = _store.Directory.Length > 56 ? "..." + _store.Directory[^53..] : _store.Directory;
        Font.Draw(r, "SAVED IN:", new Vector2(16, 90), UiTheme.Default.Muted);
        Font.Draw(r, folder, new Vector2(16, 100), UiTheme.Default.Muted);
        Heading.Draw(r, Font, "SAVING");
    }
}
