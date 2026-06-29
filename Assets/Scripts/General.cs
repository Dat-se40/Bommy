/// <summary>
/// Hằng số gameplay / auth / player — chỉnh tại đây để tinh chỉnh toàn game.
/// </summary>
public static class General
{
    #region Score

    public const int SCORE_DESTROY_OBSTACLE = 2;
    public const int SCORE_ATTACK_PLAYER = 10;
    public const int SCORE_KILL_BONUS = 100;
    public const int SCORE_SURVIVE_BONUS = 150;

    #endregion

    #region Auth — Nakama & session

    public const string AUTH_NAKAMA_SCHEME = "https";
    public const string AUTH_NAKAMA_HOST = "bommy-services-prod.up.railway.app";
    public const int AUTH_NAKAMA_PORT = 443;
    public const string AUTH_NAKAMA_SERVER_KEY = "defaultkey";
    public const string AUTH_NAKAMA_HTTP_KEY = "defaulthttpkey";

    public const string AUTH_SESSION_KEY_AUTH_TOKEN = "NK_AUTH_TOKEN";
    public const string AUTH_SESSION_KEY_REFRESH_TOKEN = "NK_REFRESH_TOKEN";
    public const string AUTH_SESSION_KEY_USER_ID = "NK_USER_ID";

    public const int AUTH_MIN_PASSWORD_LENGTH = 8;
    public const int AUTH_MIN_NAME_LENGTH = 1;
    public const int AUTH_MAX_NAME_LENGTH = 12;
    public const int AUTH_SESSION_REFRESH_BUFFER_MINUTES = 1;
    public const int AUTH_ERROR_MESSAGE_MAX_LENGTH = 180;

    public const string AUTH_DEFAULT_DISPLAY_NAME = "Player";

    #endregion

    #region Player Info — runtime stats (PlayerInfor)

    public const string PLAYER_DEFAULT_NAME = "Player";

    public const float PLAYER_MOVE_SPEED = 4f;
    public const float PLAYER_MAX_MOVE_SPEED = 8f;

    public const int PLAYER_MAX_HP = 4;
    public const int PLAYER_MAX_LIVES = 1;
    public const int PLAYER_MAX_BOMBS = 1;
    public const int PLAYER_BOMB_RANGE = 2;

    public const float PLAYER_HIT_INVINCIBILITY_DURATION = 0.45f;
    public const float PLAYER_DEATH_RESOLVE_DURATION = 0.55f;

    public const int PLAYER_GOLD_TO_SCORE_RATE = 5;
    public const int PLAYER_SHIELD_MAX_HP_DIVISOR = 2;

    #endregion

    #region Player State — board sync (PlayerBoardState)

    public const int PLAYER_STATE_MATCH_MAX_LIVES = 3;
    public const int PLAYER_STATE_MIN_BOMBS = 1;
    public const int PLAYER_STATE_MIN_TRAP_SLOTS = 1;

    #endregion
}
