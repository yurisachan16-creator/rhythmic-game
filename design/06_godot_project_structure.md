# Godot 项目结构设计

> Godot 4.6 / GDScript / GL Compatibility

---

## 一、Unity → Godot 核心概念映射

| Unity | Godot | 说明 |
|-------|-------|------|
| Scene (.unity) | Scene (.tscn) | Godot 的 Scene 既是场景也是 Prefab |
| Prefab | Scene (.tscn) | 可实例化的独立场景，完全等同 Prefab |
| GameObject | Node | 一切皆节点 |
| MonoBehaviour | 挂载在 Node 上的 Script (.gd) | |
| `[SerializeField]` | `@export` | 在 Inspector 中暴露变量 |
| `Start()` | `_ready()` | 节点进入场景树时调用 |
| `Update()` | `_process(delta)` | 每帧调用 |
| `FixedUpdate()` | `_physics_process(delta)` | 固定帧率调用 |
| `DontDestroyOnLoad` | **Autoload（单例）** | 跨场景持久存在的节点/脚本 |
| `ScriptableObject` | **Resource (.tres / .gd)** | 数据容器，可序列化 |
| UnityEvent / C# Event | **Signal（信号）** | 观察者模式的内置实现 |
| `GetComponent<T>()` | `$NodePath` 或 `get_node()` | 获取子节点 |
| `Instantiate()` | `scene.instantiate()` | 实例化场景 |
| `Destroy()` | `queue_free()` | 销毁节点 |
| Layer & Tag | **Group** | 节点可加入多个 Group |
| `Resources.Load()` | `load("res://path")` | 运行时加载资源 |
| `Application.dataPath` | `user://` | 用户数据目录（存档、设置） |

---

## 二、目录结构

```
res://
├── autoloads/                  # 全局单例脚本（Autoload）
│   ├── GameManager.gd
│   ├── AudioManager.gd
│   ├── SaveManager.gd
│   └── SettingsManager.gd
│
├── scenes/                     # 所有场景文件
│   ├── gameplay/
│   │   ├── GameplayScene.tscn      # 主游玩场景
│   │   ├── Lane.tscn               # 单条轨道（可复用）
│   │   ├── NoteBase.tscn           # 音符基础场景
│   │   ├── NoteTap.tscn
│   │   ├── NoteHold.tscn
│   │   └── NoteSlide.tscn
│   ├── ui/
│   │   ├── MainMenu.tscn
│   │   ├── SongSelect.tscn
│   │   ├── ResultScreen.tscn
│   │   ├── PauseMenu.tscn
│   │   └── SettingsScreen.tscn
│   └── tutorial/
│       └── TutorialScene.tscn
│
├── scripts/                    # 逻辑脚本（与场景分离的纯逻辑）
│   ├── gameplay/
│   │   ├── GameplayController.gd   # 游玩总控
│   │   ├── NoteSpawner.gd          # 音符生成调度
│   │   ├── NotePool.gd             # 对象池
│   │   ├── Lane.gd                 # 轨道逻辑
│   │   ├── notes/
│   │   │   ├── NoteBase.gd
│   │   │   ├── NoteTap.gd
│   │   │   └── NoteHold.gd
│   │   ├── JudgmentSystem.gd       # 判定计算
│   │   ├── ScoreTracker.gd         # 分数/连击/ACC追踪
│   │   └── InputHandler.gd         # N轨输入处理
│   ├── chart/
│   │   ├── ChartLoader.gd          # 读取JSON谱面
│   │   ├── BeatCalculator.gd       # 拍位↔时间转换
│   │   └── ChartValidator.gd       # UGC谱面合法性检查
│   ├── ui/
│   │   ├── GameplayHUD.gd
│   │   ├── SongSelectUI.gd
│   │   └── ResultScreenUI.gd
│   └── utils/
│       ├── Constants.gd            # 全局常量
│       └── MathUtils.gd
│
├── resources/                  # Resource 数据类定义
│   ├── ChartData.gd                # 谱面数据结构（Resource子类）
│   ├── NoteData.gd                 # 单个音符数据
│   ├── SongMeta.gd                 # 歌曲元数据
│   └── PlayerSettings.gd          # 玩家设置数据
│
├── assets/
│   ├── audio/
│   │   ├── sfx/                    # 打击音效
│   │   └── bgm/                    # 内置曲目
│   ├── textures/
│   │   ├── notes/
│   │   ├── ui/
│   │   └── backgrounds/
│   ├── fonts/
│   └── shaders/
│
├── songs/                      # 内置谱面目录
│   └── [song_name]/
│       ├── song.ogg
│       ├── cover.png
│       ├── meta.json
│       └── charts/
│
└── design/                     # 设计文档（不打包进游戏）
```

---

## 三、Autoload（全局单例）设计

在 Godot 中，Autoload 等同于不会被销毁的全局单例，注册路径：
`Project → Project Settings → Autoload`

### 3.1 GameManager
**职责：** 全局状态机，管理场景切换

```
状态：MAIN_MENU / SONG_SELECT / GAMEPLAY / RESULT / SETTINGS / TUTORIAL

信号（Signals）：
  game_state_changed(new_state: GameState)

主要方法：
  start_song(meta: SongMeta, difficulty: String)
  go_to_song_select()
  go_to_main_menu()
  quit_game()
```

### 3.2 AudioManager
**职责：** 音乐播放 + 精确时间戳

```
核心变量：
  _audio_player: AudioStreamPlayer
  _song_offset: float        # 谱面定义的音频起始偏移

主要方法：
  play_song(stream: AudioStream, offset_sec: float)
  get_song_position() -> float    # 见下方【时序系统】
  stop_song()

打击音效：
  play_hit_sfx(type: String)      # 播放对应档位的音效
```

### 3.3 SaveManager
**职责：** 本地存档读写（`user://` 目录）

```
存档内容：
  - 各谱面最佳记录（分数/ACC/COMBO/评级）
  - 成就解锁状态
  - 解锁的曲目列表
  - 游玩统计（总次数/总时长）

主要方法：
  get_best_record(chart_id: String) -> Dictionary
  save_record(chart_id: String, record: Dictionary)
  get_achievement_status(ach_id: String) -> bool
  unlock_achievement(ach_id: String)
  save_all()
  load_all()
```

### 3.4 SettingsManager
**职责：** 玩家设置的读写与广播

```
设置项（PlayerSettings Resource）：
  key_bindings: Array[Array]   # [[key1, key2], ...] 每轨道两个绑定键
  global_offset: float         # 全局判定偏移（ms）
  scroll_speed: float          # 滚速倍率
  music_volume: float
  sfx_volume: float
  default_fail_mode: String    # "NORMAL"/"EASY"/"HARD"/"NO_FAIL"

信号：
  settings_changed(key: String, value: Variant)

主要方法：
  get(key: String) -> Variant
  set_value(key: String, value: Variant)  # 自动保存并发送信号
```

---

## 四、核心 Scene 节点树

### 4.1 GameplayScene.tscn

```
GameplayScene (Node2D)
├── GameplayController (Node)          ← GameplayController.gd
│
├── FieldArea (Node2D)                 # 游玩区域容器
│   ├── Lane_0 (Node2D)               ← Lane.gd
│   ├── Lane_1 (Node2D)
│   ├── Lane_2 (Node2D)
│   └── Lane_3 (Node2D)
│       ├── JudgmentLine (Line2D)      # 判定线视觉
│       └── NoteContainer (Node2D)    # 音符放置于此
│
├── InputHandler (Node)                ← InputHandler.gd
├── NoteSpawner (Node)                 ← NoteSpawner.gd
├── NotePool (Node)                    ← NotePool.gd
├── JudgmentSystem (Node)              ← JudgmentSystem.gd
├── ScoreTracker (Node)                ← ScoreTracker.gd
│
└── HUD (CanvasLayer)                  # 永远在画面最上层
    ├── ScoreLabel (Label)
    ├── ComboLabel (Label)
    ├── AccuracyLabel (Label)
    ├── HealthBar (ProgressBar)
    ├── JudgmentDisplay (Node2D)       # 判定文字弹出效果
    └── PauseButton (Button)
```

### 4.2 Lane.tscn（单轨道，实例化4份）

```
Lane (Node2D)                          ← Lane.gd
├── Background (ColorRect)            # 轨道背景色
├── NoteContainer (Node2D)            # 音符父节点
├── JudgmentLine (Line2D / Sprite2D)  # 判定线
└── HitEffect (CPUParticles2D)        # 按键粒子效果
```

### 4.3 NoteTap.tscn

```
NoteTap (Node2D)                       ← NoteTap.gd
├── Body (ColorRect / Sprite2D)       # 音符主体
└── HitParticles (CPUParticles2D)     # 命中粒子（命中后播放）
```

### 4.4 NoteHold.tscn

```
NoteHold (Node2D)                      ← NoteHold.gd
├── Head (ColorRect / Sprite2D)       # 头部
├── Body (ColorRect)                  # 拉伸的长条（动态高度）
└── Tail (ColorRect / Sprite2D)       # 尾部
```

---

## 五、数据结构（Resource 类）

### 5.1 SongMeta（res://resources/SongMeta.gd）

```gdscript
class_name SongMeta
extends Resource

@export var title: String
@export var artist: String
@export var bpm: float
@export var audio_file: String      # 相对于歌曲目录的路径
@export var cover_file: String
@export var preview_start: float
@export var charts: Array[ChartInfo]  # 各难度信息
```

### 5.2 ChartData（res://resources/ChartData.gd）

```gdscript
class_name ChartData
extends Resource

@export var version: int = 1
@export var key_count: int = 4
@export var od: float = 8.0           # Overall Difficulty
@export var bpm_events: Array         # [{beat, bpm}, ...]
@export var scroll_events: Array      # [{beat, speed}, ...]
@export var notes: Array[NoteData]
@export var events: Array             # 谱面演出事件
```

### 5.3 NoteData（res://resources/NoteData.gd）

```gdscript
class_name NoteData
extends Resource

enum NoteType { TAP, HOLD, SLIDE, CHORD }

@export var type: NoteType
@export var lane: int
@export var beat: float               # 头部拍位
@export var end_beat: float           # 尾部拍位（TAP忽略）
@export var lane_end: int = -1        # SLIDE终点轨道
@export var time_ms: float            # 转换后的毫秒时间（运行时填入）
@export var end_time_ms: float
```

---

## 六、核心数据流与信号架构

```
InputHandler
  │  检测按键，发出信号
  │
  ▼  signal: lane_pressed(lane_index: int)
  │         lane_released(lane_index: int)
  │
JudgmentSystem
  │  接收输入事件 + 查询 AudioManager.get_song_position()
  │  找到最近的待判定音符 → 计算偏差ms → 得出判定档位
  │
  ▼  signal: note_judged(note: NoteData, judgment: JudgmentType, delta_ms: float)
  │
  ├──→ ScoreTracker         更新分数、连击、ACC
  │      ▼  signal: score_updated(score, combo, acc)
  │      └──→ GameplayHUD   刷新分数/连击/准确率显示
  │
  ├──→ Lane (对应轨道)      触发打击粒子效果
  │
  └──→ JudgmentDisplay      弹出判定文字（PERFECT/GREAT...）

GameplayController
  │  监听 ScoreTracker 的 health_changed 信号
  │  health == 0 → 触发 Stage Failed
  │
  │  监听 NoteSpawner 的 chart_finished 信号
  │  → 游玩结束 → 切换到 ResultScreen
```

---

## 七、时序系统（最关键部分）

> 音乐游戏的一切判定都依赖"当前精确歌曲位置（毫秒）"，
> 这是整个系统的基石，必须在第一天就做对。

### 7.1 问题所在

Godot 的 `AudioStreamPlayer.get_playback_position()` 返回值**不连续**，
它是按音频缓冲块更新的（约每 21ms 跳一次），直接用会导致判定抖动。

### 7.2 精确时间公式（固定写法）

```gdscript
# AudioManager.gd
func get_song_position() -> float:
    # 返回值：歌曲当前播放位置（秒）
    return (
        _audio_player.get_playback_position()
        + AudioServer.get_time_since_last_mix()
        - AudioServer.get_output_latency()
    )
```

| 部分 | 作用 |
|------|------|
| `get_playback_position()` | 上一次缓冲块的时间戳（粗略） |
| `+ get_time_since_last_mix()` | 补上从上次缓冲到现在的经过时间 |
| `- get_output_latency()` | 减去声卡/系统输出延迟 |

结合 **SettingsManager 的 global_offset**（玩家校正值），最终判定用：

```gdscript
var judged_time_ms: float = (
    AudioManager.get_song_position() * 1000.0
    + SettingsManager.get("global_offset")
)
```

### 7.3 拍位到毫秒转换（BeatCalculator.gd）

```
输入：beat（拍位浮点数）+ bpm_events 列表
输出：time_ms（毫秒）

算法：
  从第0拍开始，逐段计算：
  每段时长(ms) = 该段拍数 × (60000 / 该段BPM)
  累加直到覆盖目标 beat
```

> ChartLoader 加载谱面后立即调用 BeatCalculator，
> 将所有 NoteData.beat 转换为 NoteData.time_ms，
> 后续判定系统只用 time_ms，不再使用 beat。

### 7.4 音符可见时机计算

```
音符出现时间 = time_ms - 提前量

提前量(ms) = (判定线到顶部的距离px) / (基础滚速px/s × 玩家滚速倍率) × 1000
```

NoteSpawner 在每帧检查：
```
当 get_song_position() * 1000 >= note.time_ms - 提前量
→ 从 NotePool 取出对应音符节点并激活
```

---

## 八、对象池设计（NotePool.gd）

音符大量生成/销毁，必须使用对象池：

```
NotePool 维护三个池子：
  _tap_pool:   Array[NoteTap]
  _hold_pool:  Array[NoteHold]
  _slide_pool: Array[NoteSlide]

get_note(type: NoteData.NoteType) -> Node:
  从对应池取出第一个 visible=false 的节点
  若池子为空，instantiate 一个新的并加入池

release_note(note: Node):
  重置节点状态
  设为 visible = false
  归还到池中
```

---

## 九、输入系统设计（InputHandler.gd）

### 9.1 N轨抽象

不写死键位，从 SettingsManager 读取：

```gdscript
# 每帧检查
func _input(event: InputEvent):
    for lane_index in range(key_count):
        var bindings = SettingsManager.get("key_bindings")[lane_index]
        for key in bindings:
            if event.is_action_pressed(key):
                emit_signal("lane_pressed", lane_index)
            if event.is_action_released(key):
                emit_signal("lane_released", lane_index)
```

### 9.2 输入映射初始化

SettingsManager 在启动时将玩家设置的键位注册到 Godot 的 InputMap：

```
"lane_0_key0" → D
"lane_0_key1" → （空）
"lane_1_key0" → F
...
```

后续 InputHandler 只查询 InputMap，不硬编码具体键位。

---

## 十、场景切换流程

```
MainMenu
  ↓ 按"开始游戏"
SongSelect
  ↓ 选择歌曲+难度 → GameManager.start_song()
GameplayScene（加载谱面 → 倒计时 → 开始音乐 → 游玩 → 结算触发）
  ↓ 通关 / 清版
ResultScreen（展示数据 → 保存记录到 SaveManager）
  ↓ 返回
SongSelect
```

场景切换使用 `get_tree().change_scene_to_file("res://scenes/ui/SongSelect.tscn")`，
Autoload（GameManager/AudioManager 等）全程不销毁。

---

## 十一、下一步行动建议

| 顺序 | 任务 | 说明 |
|------|------|------|
| 1 | 建立目录结构 + 创建空脚本文件 | 先把架子搭起来 |
| 2 | 实现 AudioManager.get_song_position() | 时序基础，先做先稳 |
| 3 | 实现 BeatCalculator + ChartLoader | 能读谱面是一切的前提 |
| 4 | 实现 InputHandler | N轨抽象输入 |
| 5 | 实现基础 GameplayScene（只有TAP） | 跑通最小游玩循环 |
| 6 | 实现 JudgmentSystem + ScoreTracker | 核心判定逻辑 |
| 7 | 实现 NotePool + NoteSpawner | 性能优化 |
| 8 | 接入 HUD + ResultScreen | 完整体验闭环 |
