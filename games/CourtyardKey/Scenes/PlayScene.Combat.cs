using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Model;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Scenes;

namespace CourtyardKey.Scenes;

/// <summary>Hit resolution between the hero, enemies, projectiles, pickups and breakables.</summary>
public sealed partial class PlayScene
{
    public const string BossFlag = "boss:warden";

    private void ResolveCombat()
    {
        if (Hero.SwordBox is { } sword) ResolveSword(sword);
        var hurtbox = Hero.Hurtbox;
        foreach (var a in _actors)
        {
            switch (a)
            {
                case Enemy e when e.Harmful && e.Hurtbox.Intersects(hurtbox):
                    Hero.TakeDamage(e.ContactDamage, e.Center);
                    break;
                case Projectile p when p.Bounds.Intersects(hurtbox):
                    if (Hero.TakeDamage(p.Damage, p.Position)) p.Pop();
                    break;
                case Pickup pk when !pk.Carried && pk.Collectable && pk.Bounds.Intersects(hurtbox):
                    Collect(pk);
                    break;
            }
        }
    }

    private void ResolveSword(RectF sword)
    {
        var from = Hero.Center;
        foreach (var a in _actors)
        {
            if (a is IHittable h && h.Hurtbox.Intersects(sword) && Hero.MarkStruck(a))
                h.OnHit(new Hit(Combat.SwordDamage, from, HitKind.Sword));
            else if (a is Projectile p && p.Bounds.Intersects(sword))
            {
                p.Pop();
                Audio.Play("deflect", 0.7f);
            }
        }
        // Bushes are tiles: cut every cuttable cell the blade touches.
        var (x0, y0) = Room.WorldToTile(sword.Position);
        var (x1, y1) = Room.WorldToTile(new Vector2(sword.Right - 0.01f, sword.Bottom - 0.01f));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (Room.Tiles.InBounds(x, y) && (TileFlagsTable.For(Room.TileAt(x, y)) & TileFlagsTable.Cuttable) != 0)
                    CutBush(x, y);
    }

    private void CutBush(int x, int y)
    {
        Room.SetTile(x, y, Tile.Grass);
        var center = Room.TileToWorld(x + 0.5f, y + 0.5f);
        Burst(center, Fx.Leaves, 12);
        Audio.Play("cut", 0.8f, 1f, 0.1f);
        DropLoot(center + new Vector2(0, 4), 0.3f);
    }

    public void OnSwordSwing(Hero hero) => Audio.Play("sword", 0.8f, 1f, 0.08f);

    public bool TryThrowBoomerang(Hero hero)
    {
        if (ActiveBoomerang is not null) return false;
        var dir = hero.InputDirection != Vector2.Zero ? hero.InputDirection : hero.Facing.ToVector();
        Spawn(new Boomerang(this, hero.Position + new Vector2(0, -7) + hero.Facing.ToVector() * 6, dir));
        return true;
    }

    /// <summary>Applies the boomerang to whatever it touches. Returns true when it should turn back.</summary>
    public bool BoomerangStrike(Boomerang b)
    {
        bool turn = false;
        foreach (var a in _actors)
        {
            if (a is Pickup pk && pk.Bounds.Intersects(b.Bounds)) b.Carry(pk);
            else if (a is IHittable h and not Pot && h.Hurtbox.Intersects(b.Bounds) && b.TryStrike(a))
            {
                var outcome = h.OnHit(new Hit(0, b.Position, HitKind.Boomerang));
                if (outcome != HitOutcome.Ignored) turn = true;
                Burst(b.Position, Fx.SpiritMote, 6, additive: true);
            }
        }
        return turn;
    }

    public void OnHeroHurt(Hero hero)
    {
        Audio.Play("hurt");
        ShakeCamera(0.35f);
        Hitstop(0.07f);
        Burst(hero.Center, Fx.HitSpark, 8, additive: true);
        if (hero.Health.IsDead) Coroutines.Start(DeathSequence());
    }

    public void OnShieldBlock(Enemy enemy, in Hit hit)
    {
        Audio.Play("shield", 0.9f, 1f, 0.05f);
        var between = Vector2.Lerp(enemy.Center, hit.From, 0.45f);
        Burst(between, Fx.HitSpark, 7, additive: true);
        if (hit.Kind == HitKind.Sword) Hero.Knockback(enemy.Center, 140);
    }

    public void OnEnemyDamaged(Enemy enemy)
    {
        Audio.Play("hit", 0.9f, 1f, 0.08f);
        Hitstop(0.05f);
        Burst(enemy.Center, Fx.HitSpark, 6, additive: true);
    }

    public void OnEnemyKilled(Enemy enemy)
    {
        Audio.Play("enemy_die", 0.9f, 1f, 0.08f);
        Puff(enemy.Center);
        Burst(enemy.Center, Fx.Sparkle, 6, additive: true);
        DropLoot(enemy.Position, 0.55f);
        G.Achievements.Unlock(CourtyardAchievements.FirstFoe);
        G.Achievements.AddProgress(CourtyardAchievements.Defender);
    }

    public void OnWardenCrash(Warden warden)
    {
        Audio.Play("stomp");
        ShakeCamera(0.65f);
        Burst(warden.Center + new Vector2(0, -10), Fx.Rubble, 18);
    }

    public void OnWardenLand(Warden warden)
    {
        Audio.Play("stomp");
        ShakeCamera(0.5f);
        Burst(warden.Position, Fx.Dust, 14);
        Spawn(new Shockwave(this, warden.Position, 110));
    }

    public void OnWardenDefeated(Warden warden) => Coroutines.Start(WardenDeath(warden));

    private System.Collections.IEnumerator WardenDeath(Warden warden)
    {
        Audio.StopMusic(0.3f);
        Progress.Set(BossFlag);
        G.Achievements.Unlock(CourtyardAchievements.Warden);
        foreach (var p in _actors.OfType<Projectile>()) p.Destroy();
        for (int i = 0; i < 12; i++)
        {
            var at = warden.Center + new Vector2(((float)Rng.NextDouble() - 0.5f) * 32, ((float)Rng.NextDouble() - 0.7f) * 32);
            Puff(at);
            Burst(at, Fx.Rubble, 6);
            Audio.Play(i % 3 == 0 ? "boss_hurt" : "enemy_die", 0.7f, 0.8f + i * 0.03f);
            ShakeCamera(0.3f);
            yield return Wait.Seconds(0.15f);
        }
        Audio.Play("boss_die");
        ShakeCamera(1f);
        _hud.Flash(Visuals.Pal.White, 0.6f);
        warden.FinishDying();
        Spawn(new Pickup(this, warden.Position, PickupKind.HeartContainer));
        yield return Wait.Seconds(1.0f);
        Audio.Play("victory");
        foreach (var d in Doors) if (d.Kind == DoorKind.Shutter) d.SetOpen(true);
        Progress.Set(SolvedFlag);
        foreach (var p in _actors.OfType<Pedestal>()) p.Reveal(instantly: false);
        Audio.Play("shutter", 0.6f);
        yield return Wait.Seconds(2.5f);
        Audio.PlayMusic("dungeon", 1f);
    }

    private System.Collections.IEnumerator BossIntro()
    {
        yield return Wait.Seconds(0.4f);
        Audio.Play("roar");
        ShakeCamera(0.6f);
        _hud.Banner("The Moss Warden");
    }

    /// <summary>Called every update the hero leans into an obstacle; opens locks and shoves blocks.</summary>
    public void OnHeroPushing(Hero hero, float pushTime)
    {
        var probe = hero.InteractProbe;
        foreach (var a in _actors)
        {
            if (a is Door { IsOpen: false } door && door.Bounds.Intersects(probe) && pushTime > 0.12f)
            {
                TryUnlock(door, hero);
                hero.ResetPush();
                return;
            }
            if (a is PushBlock block && block.Bounds.Intersects(probe) && pushTime > Hero.PushDelay)
            {
                var c = block.Bounds.Center;
                float misalign = hero.Facing.IsHorizontal() ? MathF.Abs(hero.Center.Y - c.Y) : MathF.Abs(hero.Center.X - c.X);
                if (misalign < 8 && block.TryPush(hero.Facing)) hero.ResetPush();
                return;
            }
        }
    }

    private void TryUnlock(Door door, Hero hero)
    {
        switch (door.Kind)
        {
            case DoorKind.Locked when Progress.TryUseSmallKey():
            case DoorKind.Big when Progress.HasBossKey:
                door.SetOpen(true);
                Progress.Set(Spawner.DoorFlag(Room, door.Side));
                Audio.Play("unlock");
                Burst(door.Bounds.Center, Fx.Sparkle, 12, additive: true);
                break;
            case DoorKind.Locked:
                _hud.Toast("Locked. A small key would open it.");
                break;
            case DoorKind.Big:
                _hud.Toast("A great lock. It needs the Warden's Key.");
                break;
        }
    }

    public void Collect(Pickup pickup)
    {
        if (!pickup.Alive) return;
        pickup.Destroy();
        if (pickup.Flag is { } flag) Progress.Set(flag);
        switch (pickup.Kind)
        {
            case PickupKind.Heart:
                Hero.Health.Heal(2);
                Audio.Play("heart");
                break;
            case PickupKind.Gem: Progress.AddGems(1); Audio.Play("gem"); break;
            case PickupKind.BlueGem: Progress.AddGems(5); Audio.Play("gem", 1, 0.9f); break;
            case PickupKind.RedGem:
                Progress.AddGems(20);
                Audio.Play("gem", 1, 0.8f);
                break;
            case PickupKind.HeartContainer:
                GiveItem(Item.HeartContainer, 1, null);
                return;
        }
        Burst(pickup.Position + new Vector2(0, -4), Fx.Sparkle, 5, additive: true);
    }
}
