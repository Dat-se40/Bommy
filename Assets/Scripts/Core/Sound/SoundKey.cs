/// <summary>
/// Key chuẩn cho SoundManager / SoundLibrary.
/// Xem README mục "Network" — key Synced vs Local.
/// </summary>
public static class SoundKey
{
    #region Synced — phát trong handler replicate (mọi client)

    public const string SfxPlaceBomb = "sfx.place_bomb";
    public const string SfxExplosion = "sfx.explosion";
    public const string SfxMatchEnd = "sfx.match_end";
    public const string SfxShrink = "sfx.shrink"; 
    #endregion

    #region Local — chỉ gọi khi isOwner / UI máy mình

    public const string SfxPickup = "sfx.pickup";
    public const string SfxPlayerHurt = "sfx.player_hurt";
    public const string SfxPlayerTakenTrap = "sfx.player_taken_trap";  
    #endregion

    #region BGM — thường mỗi client tự phát cùng key khi vào scene / sự kiện sync

    public const string BgmMenu = "bgm.menu";
    public const string BgmInGame = "bgm.ingame";
    public const string BgmClassGarden = "bgm.classic";
    public const string BgmSummer = "bgm.summer"; 
    public const string BgmDungeon = "bgm.dungeon"; 
    public const string BgmWinter = "bgm.winter"; 

    // Local
    public const string SfxClick       = "sfx.click";
    public const string SfxMove        = "sfx.move";
    public const string SfxCountdown   = "sfx.countdown";
    public const string SfxOpenDialog = "sfx.open_dialog";
    public const string SfxEnterGameState = "sfx.enter_game";
    public const string SfxEnterShrinkState = "sfx.enter_shrink";
    // Synced
    public const string SfxVictory     = "sfx.victory";

    #endregion
}
