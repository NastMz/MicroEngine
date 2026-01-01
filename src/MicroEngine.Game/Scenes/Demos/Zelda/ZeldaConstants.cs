using MicroEngine.Core.Graphics;

namespace MicroEngine.Game.Scenes.Demos.Zelda;

/// <summary>
/// Centralized configuration for the Mini-Zelda prototype.
/// All magic numbers and strings should be defined here for maintainability.
/// </summary>
public static class ZeldaConstants
{
    // --- Asset Paths ---
    /// <summary> Path to the hero/link texture sheet. </summary>
    public const string HERO_TEXTURE_PATH = "assets/textures/hero.png";
    /// <summary> Path to the slime/blob texture sheet. </summary>
    public const string BLOB_TEXTURE_PATH = "assets/textures/blob.png";
    /// <summary> Path to the tileset texture for the dungeon. </summary>
    public const string TILES_TEXTURE_PATH = "assets/textures/dungeon_tiles.png";
    /// <summary> Path to the sword swing sound effect. </summary>
    public const string SWORD_SFX_PATH = "assets/audio/sfx/sword.wav";
    /// <summary> Path to the enemy hit sound effect. </summary>
    public const string HIT_SFX_PATH = "assets/audio/sfx/hit.wav";
    
    // --- Map Settings ---
    /// <summary> Path to the static map layout JSON. </summary>
    public const string MAP_JSON_PATH = "assets/maps/zelda_map.json";

    // --- World & Map ---
    /// <summary> Width of the tilemap in tiles. </summary>
    public const int MAP_WIDTH = 40;
    /// <summary> Height of the tilemap in tiles. </summary>
    public const int MAP_HEIGHT = 40;
    /// <summary> Pixel size of a single square tile. </summary>
    public const int TILE_SIZE = 32;
    /// <summary> Total number of enemies to spawn. </summary>
    public const int SLIME_COUNT = 12;

    // --- Player Stats ---
    /// <summary> Original pixel width/height of a hero frame in the source sheet. </summary>
    public const int HERO_SPRITE_ORIGINAL_SIZE = 214;
    /// <summary> Desired world-space size of the player for rendering. </summary>
    public const float HERO_DRAW_SIZE = 48f;
    /// <summary> Horizontal and vertical movement speed of the player. </summary>
    public const float PLAYER_SPEED = 220f;
    /// <summary> Duration (in seconds) that the player stays in the attacking state. </summary>
    public const float PLAYER_ATTACK_DURATION = 0.2f;
    /// <summary> Starting health containers for the player. </summary>
    public const int PLAYER_MAX_HEALTH = 5;

    // --- Enemy Stats ---
    /// <summary> Horizontal and vertical movement speed of enemies. </summary>
    public const float ENEMY_SPEED = 60f;
    /// <summary> Distance at which an enemy starts chasing the player. </summary>
    public const float ENEMY_DETECTION_RADIUS = 300f;
    /// <summary> Maximum health points for enemies. </summary>
    public const int ENEMY_MAX_HEALTH = 30;
    /// <summary> Uniform scale factor for drawing enemies. </summary>
    public const float ENEMY_SCALE = 1.2f;

    // --- Combat Mechanics ---
    /// <summary> Offset from player center where the sword hitbox is placed. </summary>
    public const float SWORD_REACH = 25f;
    /// <summary> Radius of the sword swing hitbox. </summary>
    public const float ATTACK_HIT_THRESHOLD = 20f;
    /// <summary> Radius of the body-to-body collision hitbox. </summary>
    public const float PROXIMITY_HIT_THRESHOLD = 15f;
    /// <summary> Duration of invulnerability after taking damage (seconds). </summary>
    public const float INVULNERABILITY_DURATION = 0.5f;
    /// <summary> Force applied to push entities away on hit. </summary>
    public const float KNOCKBACK_FORCE = 30f;
    /// <summary> Damage dealt by a player's sword strike. </summary>
    public const int SWORD_DAMAGE = 10;
    /// <summary> Damage dealt by an enemy on contact. </summary>
    public const int ENEMY_DAMAGE = 1;

    // --- Colors ---
    /// <summary> Color used for damage flashes. </summary>
    public static readonly Color COLOR_DAMAGE = Color.Red;
    /// <summary> Color for the health container text. </summary>
    public static readonly Color COLOR_UI_HEALTH = Color.Red;
    /// <summary> Default color for guide text. </summary>
    public static readonly Color COLOR_UI_TEXT = Color.White;
    /// <summary> Color for the enemy counter text. </summary>
    public static readonly Color COLOR_UI_COUNTER = Color.Yellow;
    /// <summary> Color for the victory screen message. </summary>
    public static readonly Color COLOR_UI_VICTORY = Color.Green;
    /// <summary> Color for the game over screen message. </summary>
    public static readonly Color COLOR_UI_GAMEOVER = Color.Red;
    /// <summary> Debug color for player's proximity circle. </summary>
    public static readonly Color COLOR_DEBUG_HITBOX_PLAYER = Color.Blue;
    /// <summary> Debug color for the active weapon hitbox. </summary>
    public static readonly Color COLOR_DEBUG_HITBOX_ATTACK = Color.Red;
    /// <summary> Debug color for an enemy's hitbox. </summary>
    public static readonly Color COLOR_DEBUG_HITBOX_ENEMY = Color.Yellow;

    // --- Animation Clips ---
    /// <summary> Animation clip name for walking down. </summary>
    public const string CLIP_WALK_DOWN = "walk_down";
    /// <summary> Animation clip name for walking up. </summary>
    public const string CLIP_WALK_UP = "walk_up";
    /// <summary> Animation clip name for walking left. </summary>
    public const string CLIP_WALK_LEFT = "walk_left";
    /// <summary> Animation clip name for walking right. </summary>
    public const string CLIP_WALK_RIGHT = "walk_right";
    /// <summary> Animation clip name for attacking down. </summary>
    public const string CLIP_ATTACK_DOWN = "attack_down";
    /// <summary> Animation clip name for attacking up. </summary>
    public const string CLIP_ATTACK_UP = "attack_up";
    /// <summary> Animation clip name for attacking left. </summary>
    public const string CLIP_ATTACK_LEFT = "attack_left";
    /// <summary> Animation clip name for attacking right. </summary>
    public const string CLIP_ATTACK_RIGHT = "attack_right";
    /// <summary> Animation clip name for enemy idle state. </summary>
    public const string CLIP_ENEMY_IDLE = "idle";

    // --- UI Strings ---
    /// <summary> Primary message for the player death screen. </summary>
    public const string MSG_GAME_OVER = "GAME OVER";
    /// <summary> Primary message for the screen after all enemies are dead. </summary>
    public const string MSG_VICTORY = "YOU WIN!";
    /// <summary> Instructional text shown on end screens. </summary>
    public const string MSG_RESTART_HINT = "PRESS [R] TO RESTART";
}
