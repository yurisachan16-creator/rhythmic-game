using Godot;

namespace RhythmicGame;

/// <summary>全局状态机，管理场景切换。注册为 Autoload。</summary>
public partial class GameManager : Node
{
    public enum GameState { MainMenu, SongSelect, Gameplay, Result, Settings, Tutorial }

    public event Action<GameState>? GameStateChanged;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    /// <summary>当前选中的歌曲元数据，由 StartSong() 写入，GameplayScene 读取</summary>
    public SongMeta? CurrentSongMeta { get; private set; }
    public string CurrentDifficulty { get; private set; } = "";
    public string CurrentChartFile  { get; private set; } = "";
    public PlayerSettings.FailMode CurrentFailMode { get; private set; } = PlayerSettings.FailMode.Normal;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    public void StartSong(SongMeta meta, string difficulty, string chartFile,
        PlayerSettings.FailMode failMode = PlayerSettings.FailMode.Normal)
    {
        CurrentSongMeta   = meta;
        CurrentDifficulty = difficulty;
        CurrentChartFile  = chartFile;
        CurrentFailMode   = failMode;
        ChangeState(GameState.Gameplay);
        GetTree().ChangeSceneToFile(Constants.SceneGameplay);
    }

    public void GoToSongSelect()
    {
        ChangeState(GameState.SongSelect);
        GetTree().ChangeSceneToFile(Constants.SceneSongSelect);
    }

    public void GoToMainMenu()
    {
        ChangeState(GameState.MainMenu);
        GetTree().ChangeSceneToFile(Constants.SceneMainMenu);
    }

    public void GoToResult()
    {
        ChangeState(GameState.Result);
        GetTree().ChangeSceneToFile(Constants.SceneResult);
    }

    public void QuitGame() => GetTree().Quit();

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        GameStateChanged?.Invoke(newState);
    }
}
