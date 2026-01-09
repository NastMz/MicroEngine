using MicroEngine.Core.Graphics;

namespace MicroEngine.Game.Scenes.Demos.Zelda;

/// <summary>
/// Centralized configuration for the Mini-Zelda prototype.
/// All magic numbers and strings should be defined here for maintainability.
/// </summary>
public static class ZeldaConstants
{
    // --- Asset Manifest & Manifest Tags ---
    /// <summary> Path to the unified asset manifest JSON. </summary>
    public const string ASSET_MANIFEST_PATH = "assets/textures/AssetManifest.json";
    /// <summary> Name of the tileset asset in the manifest. </summary>
    public const string TILES_ASSET_NAME = "dungeon_tiles";

    // --- Asset Paths ---
    public const string HERO_TEXTURE_PATH = "assets/textures/hero.png";
    public const string BLOB_TEXTURE_PATH = "assets/textures/blob.png";
    public const string TILES_TEXTURE_PATH = "assets/textures/dungeon_tiles.png";
    public const string SWORD_SFX_PATH = "assets/audio/sfx/sword.wav";
    public const string HIT_SFX_PATH = "assets/audio/sfx/hit.wav";

    // --- Map Settings ---
    /// <summary> Path to the static map layout JSON. </summary>
    public const string MAP_JSON_PATH = "assets/maps/zelda_map.json";

    // --- Tile IDs (Map Data) ---
    public const int TILE_ID_FLOOR_STONE_VAR1 = 0;
    public const int TILE_ID_FLOOR_STONE_VAR2 = 1;
    public const int TILE_ID_WALL_TOP = 2;
    public const int TILE_ID_PILLAR = 3;
    public const int TILE_ID_HOLE_CENTER = 4;
    public const int TILE_ID_FLOOR_CRACKED = 5;
    public const int TILE_ID_WALL_MID = 6;
    public const int TILE_ID_WALL_BASE = 7;
    public const int TILE_ID_WALL_TOP_LEFT = 8;
    public const int TILE_ID_WALL_TOP_RIGHT = 9;
    public const int TILE_ID_HOLE_TOP = 10;
    public const int TILE_ID_HOLE_BOT = 11;
    public const int TILE_ID_HOLE_LEFT = 12;
    public const int TILE_ID_HOLE_RIGHT = 13;

    // --- World & Map ---
    /// <summary> Width of the tilemap in tiles. </summary>
    public const int MAP_WIDTH = 40;
    /// <summary> Height of the tilemap in tiles. </summary>
    public const int MAP_HEIGHT = 40;
    /// <summary> Pixel size of a single square tile. </summary>
    public const int TILE_SIZE = 32;
    /// <summary> Total number of enemies to spawn. </summary>
    public const int SLIME_COUNT = 12;
    /// <summary> Max attempts to find a safe spawn spot. </summary>
    public const int SPAWN_MAX_ATTEMPTS = 100;
    /// <summary> Default spawn X tile if search fails. </summary>
    public const int SPAWN_FALLBACK_TILE_X = 4;
    /// <summary> Default spawn Y tile if search fails. </summary>
    public const int SPAWN_FALLBACK_TILE_Y = 4;

    // --- Entity Names ---
    public const string ENTITY_PLAYER = "Link";
    public const string ENTITY_MAP = "Map";
    public const string ENTITY_ENEMY_PREFIX = "Slime_";

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
    /// <summary> Vertical origin multiplier (pivot at feet). </summary>
    public const float PLAYER_PIVOT_Y_FACTOR = 0.9f;
    /// <summary> Physics collision radius for the player. </summary>
    public const float PLAYER_COLLISION_RADIUS = 8f;

    // --- Enemy Stats ---
    /// <summary> Horizontal and vertical movement speed of enemies. </summary>
    public const float ENEMY_SPEED = 60f;
    /// <summary> Distance at which an enemy starts chasing the player. </summary>
    public const float ENEMY_DETECTION_RADIUS = 300f;
    /// <summary> Maximum health points for enemies. </summary>
    public const int ENEMY_MAX_HEALTH = 30;
    /// <summary> Uniform scale factor for drawing enemies. </summary>
    public const float ENEMY_SCALE = 1.2f;
    /// <summary> Vertical origin multiplier (pivot at base). </summary>
    public const float ENEMY_PIVOT_Y_FACTOR = 0.85f;
    /// <summary> Offset for enemy body center calculation (relative to draw size). </summary>
    public const float ENEMY_BODY_CENTER_Y_FACTOR = 0.4f;
    /// <summary> Physics collision radius for enemies. </summary>
    public const float ENEMY_COLLISION_RADIUS = 8f;
    /// <summary> Hitbox visual radius for enemies. </summary>
    public const float ENEMY_HITBOX_RADIUS = 16f;

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

    // --- Camera Settings ---
    public const float CAMERA_INITIAL_X = 400f;
    public const float CAMERA_INITIAL_Y = 300f;
    public const float CAMERA_LERP_FACTOR = 8.0f;

    // --- Colors ---
    public static readonly Color COLOR_BACKGROUND = new Color(20, 25, 20, 255);
    public static readonly Color COLOR_UI_OVERLAY = new Color(0, 0, 0, 150);
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
    public const float ANIM_FRAME_TIME_HERO = 0.12f;
    public const float ANIM_FRAME_TIME_ENEMY = 0.15f;
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

    // --- UI Layout ---
    public const int UI_MARGIN = 20;
    public const int UI_LINE_HEIGHT = 25;
    public const int UI_HEART_CONTAINER_Y = 550;
    public const int FONT_SIZE_TITLE = 28;
    public const int FONT_SIZE_GUIDE = 16;
    public const int FONT_SIZE_HUD = 20;
    public const int FONT_SIZE_END_SCREEN_LARGE = 40;
    public const float UI_END_SCREEN_X = 320f;
    public const float UI_END_SCREEN_Y = 250f;
    public const float UI_RESTART_HINT_Y = 320f;

    // --- UI Strings ---
    /// <summary> Primary message for the player death screen. </summary>
    public const string MSG_GAME_OVER = "GAME OVER";
    /// <summary> Primary message for the screen after all enemies are dead. </summary>
    public const string MSG_VICTORY = "YOU WIN!";
    /// <summary> Instructional text shown on end screens. </summary>
    public const string MSG_RESTART_HINT = "PRESS [R] TO RESTART";
    /// <summary> Key substring to identify a victory message. </summary>
    public const string MSG_VICTORY_KEY = "WIN";
    public const string MSG_LOADING_MAP = "Loaded static map: ";
    public const string MSG_ERROR_LOADING_MAP = "Error loading map: ";
    public const string MSG_SCENE_READY = "Scene loaded successfully. Camera and Systems ready.";

    // --- Logging ---
    public const string LOG_ZELDA = "Zelda";
    public const string LOG_COMBAT = "Combat";
    public const string LOG_PLAYER = "Player";
}
