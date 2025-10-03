## Developer & Contributions
**Alerica** (Game Designer) <br>
**adxze** (Game Programmer) <br>
**Albert** (Game Artist)

## About
Zumi The Slime is a 3D action adventure game where players control a slime character with unique shooting mechanics inspired by Zuma gameplay. Navigate through challenging levels, defeat enemies using ball matching mechanics along spline paths, and face off against powerful bosses. Master movement abilities like jumping, dodging, and precise aiming while strategically shooting colored balls to create matches and damage enemies.

<br>

## Key Features

**Dynamic Ball Shooting System**: Aim and shoot colored balls with realistic physics, trajectory prediction, and gravity control. Match three or more balls of the same color along enemy spline paths to deal massive damage through chain reactions and combos.

**Advanced Movement Mechanics**: Fluid character control with jumping, air control, dodge rolls with i frames, squash and stretch animations, and responsive ground detection

**Epic Boss Battles**: Face challenging bosses with multiple attack patterns including dash attacks, jump slams, and laser beams. Learn attack telegraphs through visual indicators and precise timing to survive intense encounters.

<table>
<tr>
<td align="center" width="50%">
<strong>Ball Matching Mechanics</strong><br>
Shoot colored balls to create matches along spline paths
<img width="100%" alt="gif1" src="https://github.com/adxze/adxze/blob/main/ZumiGif/ZumiMain.gif">
</td>
<td align="center" width="50%">
<strong>Boss Combat</strong><br>
Dynamic boss fights with telegraphed attacks
<img width="100%" alt="gif1" src="https://github.com/adxze/adxze/blob/main/ZumiGif/1003.gif">
</td>
</tr>
</table>

## Scene Flow
```mermaid
flowchart LR
  mm[Main Menu]
  gp[Gameplay]
  boss[Boss Fight]
  ending[Ending Scene]

  mm -- "Start Game" --> gp
  gp -- "Reach Boss" --> boss
  boss -- "Defeat Boss" --> ending
  boss -- "Death" --> gp
  ending -- "Credits" --> mm
```

## Layer / Module Design
```mermaid
---
config:
  theme: neutral
  look: neo
---
graph TD
    subgraph "Core Systems"
        GM[GameManager]
        IM[InputManager]
        SaveSys[Save System]
    end
    
    subgraph "Player Systems"
        PC[Player Controller]
        Movement[Movement System]
        Shooter[Frog Shooter]
        Health[Slime Health]
        Camera[Camera Controller]
    end
    
    subgraph "Enemy Systems"
        ZumaEnemy[Zuma Enemy]
        TotemEnemy[Totem Enemy]
        Boss[Boss Controller]
        EnemyHealth[Enemy Health]
    end
    
    subgraph "Ball Mechanics"
        BallSpawner[Spline Ball Spawner]
        BallBehavior[Ball Behavior]
        ChainSystem[Chain Matching]
    end
    
    subgraph "UI Systems"
        HealthUI[Health UI]
        BallUI[Ball UI]
        EnemyUI[Enemy Health Bar]
    end
    
    GM --> Health
    GM --> Movement
    IM --> PC
    IM --> Camera
    
    PC --> Movement
    PC --> Shooter
    PC --> Camera
    
    Shooter --> BallBehavior
    BallBehavior --> BallSpawner
    BallSpawner --> ZumaEnemy
    BallSpawner --> Boss
    
    Movement --> Health
    Health --> HealthUI
    Shooter --> BallUI
    
    ZumaEnemy --> EnemyHealth
    TotemEnemy --> EnemyHealth
    Boss --> EnemyUI
    
    BallSpawner --> ChainSystem
    ChainSystem --> EnemyHealth
    
    classDef coreStyle fill:#e1f5fe,stroke:#01579b,stroke-width:2px
    classDef playerStyle fill:#e8f5e8,stroke:#1b5e20,stroke-width:2px
    classDef enemyStyle fill:#ffebee,stroke:#b71c1c,stroke-width:2px
    classDef ballStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef uiStyle fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    
    class GM,IM,SaveSys coreStyle
    class PC,Movement,Shooter,Health,Camera playerStyle
    class ZumaEnemy,TotemEnemy,Boss,EnemyHealth enemyStyle
    class BallSpawner,BallBehavior,ChainSystem ballStyle
    class HealthUI,BallUI,EnemyUI uiStyle
```

## Modules and Features

The 3D action-adventure gameplay with Zuma-inspired ball-matching mechanics, advanced movement system, boss battles, and comprehensive health management is powered by an extensive Unity C# scripting system.

| 📂 Name | 🎬 Scene | 📋 Responsibility |
|---------|----------|-------------------|
| **GameManager** | **All Scenes** | - Manage game state and checkpoint system<br/>- Handle player death and respawn<br/>- Control fade effects and scene transitions<br/>- Track death/revive statistics |
| **InputManager** | **Gameplay** | - Handle keyboard input for debug functions<br/>- Toggle UI visibility (F12)<br/>- Quick kill/heal commands (F11/F10)<br/>- Manage pause menu and cursor lock |
| **FrogShooter** | **Gameplay** | - Control ball shooting mechanics<br/>- Calculate trajectory with gravity<br/>- Manage current and next ball queue<br/>- Handle aim reticle and visual feedback<br/>- Play shooting and reload sound effects |
| **ImprovedFrogMovement** | **Gameplay** | - Handle player movement with hopping mechanics<br/>- Implement jump with coyote time and jump buffering<br/>- Execute dodge roll with immunity frames<br/>- Apply squash and stretch animations<br/>- Manage ground detection and air control |
| **NewCamera** | **Gameplay** | - Control third-person camera with Cinemachine<br/>- Switch between normal and aim camera modes<br/>- Handle mouse look and camera rotation<br/>- Manage camera distance with scroll wheel<br/>- Rotate player when aiming or shooting |
| **SlimeHealth** | **Gameplay** | - Track player health (current/max)<br/>- Handle damage and healing<br/>- Trigger death and revive events<br/>- Update health UI in real-time<br/>- Manage player death state |
| **ZumaEnemy** | **Gameplay** | - Generate spiral spline paths for ball chain<br/>- Spawn and manage colored ball chain<br/>- Handle ball insertion from player shots<br/>- Detect color matches (3+ balls)<br/>- Apply damage to enemy when matches occur<br/>- Destroy matched balls with effects |
| **SplineBallSpawner** | **Gameplay** | - Advanced spline-based ball chain system<br/>- Handle ball hit detection and insertion<br/>- Process chain reactions and combos<br/>- Manage knockback and snap-back animations<br/>- Calculate match detection along splines<br/>- Report damage to enemy health |
| **BossController1** | **Boss Fight** | - Control boss AI state machine<br/>- Execute dash attack with windup and indicators<br/>- Perform jump slam attack with AOE damage<br/>- Fire laser beam with charge time<br/>- Handle boss awakening sequence<br/>- Manage boss health and death<br/>- Play attack sounds and effects |
| **BossAwakeTrigger** | **Boss Fight** | - Detect player entry to boss arena<br/>- Activate boss awakening sequence<br/>- Enable boundary objects<br/>- Trigger one-time boss encounter |
| **BossContainer** | **Boss Fight** | - Constrain boss within arena bounds<br/>- Prevent boss from leaving battle area<br/>- Display arena boundaries in editor |
| **TotemEnemy** | **Gameplay** | - Execute jump attacks toward player<br/>- Perform smash attack with ground AOE<br/>- Display smash indicators for telegraphing<br/>- Shoot projectiles when player is far<br/>- Rotate to face player target |
| **EnemyHealth** | **Gameplay** | - Track enemy health points<br/>- Handle damage from ball matches<br/>- Update enemy health bar UI<br/>- Trigger death and cleanup on zero health |
| **NewMovement** | **Gameplay** | - Alternative movement controller<br/>- Camera-relative directional input<br/>- Jump mechanics with buffering<br/>- Squash/stretch visual feedback<br/>- Ground check and air control |
| **CameraIdleJiggle** | **Gameplay** | - Add subtle camera movement<br/>- Create living camera feel<br/>- Use Perlin noise for smoothness |

<br>

## Game Flow Chart
```mermaid
---
config:
  theme: redux
  look: neo
---
flowchart TD
  start([Game Start])
  start --> move{Player Input}
  
  move -->|WASD| walk[Move Character]
  move -->|Space| jump[Jump with Buffer]
  move -->|Left Shift| dodge[Dodge Roll + i-frames]
  move -->|Mouse| aim[Aim Camera]
  move -->|Left Click| shoot[Shoot Ball]
  move -->|Right Click| aimMode[Enter Aim Mode]
  
  shoot --> trajectory[Calculate Trajectory]
  trajectory --> fire[Fire Ball with Physics]
  fire --> ballHit{Ball Hits Enemy?}
  
  ballHit -->|Yes| insert[Insert Ball in Chain]
  ballHit -->|No| miss[Ball Continues]
  
  insert --> match{3+ Same Color?}
  match -->|Yes| destroy[Destroy Matched Balls]
  match -->|No| continue[Continue Chain]
  
  destroy --> combo{Chain Reaction?}
  combo -->|Yes| destroy
  combo -->|No| damage[Deal Damage to Enemy]
  
  damage --> enemyDead{Enemy Health = 0?}
  enemyDead -->|Yes| defeat[Enemy Defeated]
  enemyDead -->|No| continue
  
  walk --> hazard{Take Damage?}
  dodge --> hazard
  jump --> hazard
  
  hazard -->|Yes| checkHealth{Health > 0?}
  hazard -->|No| move
  
  checkHealth -->|No| death[Player Death]
  checkHealth -->|Yes| continue
  
  death --> respawn[Respawn at Checkpoint]
  respawn --> start
  
  defeat --> bossTrigger{Boss Trigger?}
  bossTrigger -->|Yes| bossFight[Boss Battle]
  bossTrigger -->|No| continue
  
  bossFight --> bossPhase{Boss Attack Phase}
  bossPhase -->|Dash| dodgeAttack[Dodge Dash Attack]
  bossPhase -->|Jump| avoidSlam[Avoid Jump Slam]
  bossPhase -->|Laser| moveLaser[Move from Laser]
  
  dodgeAttack --> shootBoss[Shoot Boss Chain]
  avoidSlam --> shootBoss
  moveLaser --> shootBoss
  
  shootBoss --> bossMatch[Match Balls on Boss]
  bossMatch --> bossDamage[Boss Takes Damage]
  bossDamage --> bossDefeated{Boss Health = 0?}
  
  bossDefeated -->|Yes| victory[Victory!]
  bossDefeated -->|No| bossPhase
  
  victory --> ending[Ending Scene]
```

<br>

## Event Signal Diagram
```mermaid
classDiagram
    %% --- Player Systems ---
    class FrogShooter {
        +OnBallShoot(colorIndex: int)
        +OnReload()
        +OnAimStart()
        +OnAimEnd()
    }

    class ImprovedFrogMovement {
        +OnJump()
        +OnLand()
        +OnDodge()
        +OnImmunityStart()
        +OnImmunityEnd()
    }

    class SlimeHealth {
        +OnHealthChanged(current: int, max: int)
        +OnDeath()
        +OnRevive()
        +OnDamageTaken(amount: int)
        +OnHealed(amount: int)
    }

    class NewCamera {
        +OnAimModeEnter()
        +OnAimModeExit()
        +OnCameraDistanceChanged(distance: float)
    }

    %% --- Ball System ---
    class SplineBallSpawner {
        +OnBallHit(projectile: GameObject, colorIndex: int)
        +OnMatchFound(matchCount: int)
        +OnComboTriggered(comboCount: int)
        +OnShotReport(destroyed: int, combos: int, types: List)
    }

    class BallBehavior {
        +OnCollisionEnter()
        +OnTriggerEnter()
    }

    %% --- Enemy Systems ---
    class ZumaEnemy {
        +OnBallInserted(colorIndex: int, position: float)
        +OnMatchDestroyed(count: int)
        +OnDamageTaken(damage: float)
        +OnDeath()
    }

    class BossController1 {
        +OnAwaken()
        +OnAttackStart(attackType: string)
        +OnAttackEnd()
        +OnPhaseChange(phase: int)
        +OnDeath()
    }

    class TotemEnemy {
        +OnJumpAttack()
        +OnSmashAttack()
        +OnProjectileShot()
    }

    class EnemyHealth {
        +OnHealthChanged(current: int, max: int)
        +OnDeath()
    }

    %% --- Game Management ---
    class GameManager {
        +OnCheckpointSet(checkpoint: Transform)
        +OnPlayerDeath()
        +OnPlayerRevive()
        +OnFadeStart()
        +OnFadeComplete()
    }

    class InputManager {
        +OnPauseToggle()
        +OnUIToggle()
        +OnDebugKill()
        +OnDebugHeal()
    }

    %% --- Relations ---
    FrogShooter --> BallBehavior : creates
    BallBehavior --> SplineBallSpawner : triggers
    SplineBallSpawner --> ZumaEnemy : damages
    SplineBallSpawner --> BossController1 : damages
    
    ImprovedFrogMovement --> SlimeHealth : affects
    SlimeHealth --> GameManager : notifies
    GameManager --> SlimeHealth : revives
    
    NewCamera --> FrogShooter : provides aim
    InputManager --> GameManager : controls
    
    TotemEnemy --> SlimeHealth : damages
    BossController1 --> SlimeHealth : damages
    EnemyHealth --> ZumaEnemy : manages
    EnemyHealth --> BossController1 : manages
```

<br>

## Play The Game
<a href="https://alerica.itch.io/127-liminal-collective-student-zumi-the-slime">Play Now on itch.io</a>

<br>

## Installation & Setup
1. Clone this repository
2. Open the project in Unity (6 or later recommended)
3. Open the main gameplay scene
4. Press Play to start testing

## Controls
- **WASD** - Movement
- **Space** - Jump
- **Left Shift** - Dodge Roll
- **Mouse** - Look/Aim
- **Left Click** - Shoot Ball
- **Right Click** - Aim Mode
- **Scroll Wheel** - Adjust Camera Distance
- **Escape** - Pause Menu
- **F10** - Heal (Debug)
- **F11** - Kill Player (Debug)
- **F12** - Toggle UI (Debug)

## Credits
Game developed as a student project by the 127 Liminal Collective team.
