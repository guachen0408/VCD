# VacuumDryer 系統架構圖

> 最後更新: 2026-02-09

---

## 軟體架構圖

```mermaid
graph TB
    subgraph UI["VacuumDryer.UI (人機介面)"]
        MainWindow["MainWindow<br/>主畫面"]
        JogDialog["JogDialog<br/>JOG 手動控制"]
        ManualDialog["ManualControlDialog<br/>手動控制"]
    end
    
    subgraph Core["VacuumDryer.Core (核心邏輯)"]
        subgraph Process["Process 流程控制"]
            VPC["VacuumProcessController<br/>流程控制器"]
            PS["ProcessState<br/>狀態/參數"]
        end
        subgraph Motion["Motion 運動控制"]
            DZC["DualZController<br/>雙Z軸+蝶閥"]
        end
        subgraph Data["Data 資料"]
            DL["DataLogger<br/>資料記錄"]
        end
    end
    
    subgraph Hardware["VacuumDryer.Hardware (硬體抽象)"]
        IAxis["IAxis<br/>軸介面"]
        IMotionCard["IMotionCard<br/>控制卡介面"]
        IDigitalIO["IDigitalIO<br/>數位IO介面"]
        
        subgraph Simulation["Simulation 模擬"]
            SimAxis["SimulatedAxis"]
            SimCard["SimulatedMotionCard3Axis"]
        end
        
        subgraph Delta["Delta EtherCAT 驅動"]
            DeltaAxis["DeltaEtherCatAxis"]
            DeltaCard["DeltaPciL221MotionCard"]
            EcatDll["EtherCatDll<br/>P/Invoke"]
        end
    end
    
    subgraph External["外部"]
        DLL["MasterEcat.dll<br/>台達官方 DLL"]
        HW["PCI-L221B1D0<br/>EtherCAT 控制卡"]
    end
    
    MainWindow --> VPC
    MainWindow --> DZC
    MainWindow --> JogDialog
    JogDialog --> DZC
    
    VPC --> DZC
    VPC --> IDigitalIO
    VPC --> PS
    
    DZC --> IAxis
    
    IAxis -.-> SimAxis
    IAxis -.-> DeltaAxis
    IMotionCard -.-> SimCard
    IMotionCard -.-> DeltaCard
    
    DeltaAxis --> EcatDll
    DeltaCard --> EcatDll
    EcatDll --> DLL
    DLL --> HW
```

---

## 類別關係圖

```mermaid
classDiagram
    class IMotionCard {
        <<interface>>
        +Name: string
        +IsConnected: bool
        +InitializeAsync()
        +GetAxis(axisId)
        +GetDigitalIO()
        +EmergencyStopAllAsync()
    }
    
    class IAxis {
        <<interface>>
        +Position: double
        +IsMoving: bool
        +EnableAsync()
        +HomeAsync()
        +MoveAbsoluteAsync()
        +JogAsync()
        +StopAsync()
    }
    
    class IDigitalIO {
        <<interface>>
        +ReadInput()
        +WriteOutput()
    }
    
    class DualZController {
        -_z1Axis: IAxis
        -_z2Axis: IAxis
        -_valveAxis: IAxis
        +MoveSyncAsync()
        +SetValveAsync()
        +HomeAllAsync()
    }
    
    class VacuumProcessController {
        -_motionController: DualZController
        -_digitalIO: IDigitalIO
        -_currentState: ProcessState
        +StartAsync()
        +Pause()
        +StopAsync()
        +OnStateChanged: event
    }
    
    IMotionCard <|.. SimulatedMotionCard3Axis
    IMotionCard <|.. DeltaPciL221MotionCard
    IAxis <|.. SimulatedAxis
    IAxis <|.. DeltaEtherCatAxis
    
    DualZController --> IAxis
    VacuumProcessController --> DualZController
    VacuumProcessController --> IDigitalIO
```

---

## VCD 真空乾燥流程圖

```mermaid
flowchart TD
    Start([開始]) --> Init[初始化]
    Init --> CheckHome{已原點復歸?}
    CheckHome -->|否| Home[執行原點復歸]
    Home --> CheckHome
    CheckHome -->|是| Close[1. 關腔<br/>Z軸下降]
    
    Close --> Rough[2. 粗抽<br/>粗抽閥開啟]
    Rough --> CheckRough{壓力 < 目標?}
    CheckRough -->|否| CheckTimeout1{逾時?}
    CheckTimeout1 -->|否| CheckRough
    CheckTimeout1 -->|是| Alarm1[異常: 粗抽逾時<br/>-110401]
    CheckRough -->|是| High1[3. 細抽 I<br/>蝶閥 90°]
    
    High1 --> High2[4. 細抽 II<br/>蝶閥 70°]
    High2 --> High3[5. 細抽 III<br/>蝶閥 50°]
    High3 --> High4[6. 細抽 IV<br/>蝶閥 30°]
    High4 --> High5[7. 細抽 V<br/>蝶閥 10°]
    
    High5 --> Hold[8. 持壓<br/>維持真空]
    Hold --> Break[9. 破真空<br/>破真空閥開啟]
    Break --> Open[10. 開腔<br/>Z軸上升]
    Open --> Complete([流程完成])
    
    Alarm1 --> Error([異常終止])
    
    style Start fill:#4CAF50,color:white
    style Complete fill:#4CAF50,color:white
    style Error fill:#F44336,color:white
    style Alarm1 fill:#FF9800,color:white
```

---

## 指令下達流程

```mermaid
sequenceDiagram
    participant UI as MainWindow
    participant PC as VacuumProcessController
    participant DZ as DualZController
    participant Axis as IAxis
    participant DLL as EtherCatDll
    participant HW as 控制卡硬體
    
    UI->>PC: StartAsync()
    activate PC
    
    PC->>PC: SetState(ClosingChamber)
    PC->>DZ: MoveSyncAsync(300, 100)
    activate DZ
    
    DZ->>Axis: MoveAbsoluteAsync(300, 100)
    activate Axis
    
    Axis->>DLL: Ecat_AbsMove(0, 300, 100)
    DLL->>HW: EtherCAT 封包
    HW-->>DLL: 完成
    DLL-->>Axis: 成功
    
    deactivate Axis
    Axis-->>DZ: true
    deactivate DZ
    DZ-->>PC: true
    
    PC->>PC: SetState(RoughVacuum)
    PC->>PC: WriteOutput(0, true)
    Note over PC: 開啟粗抽閥
    
    loop 等待壓力降低
        PC->>PC: 檢查壓力
    end
    
    PC-->>UI: OnStateChanged
    deactivate PC
```

---

## IO 通道定義

| DO 通道 | 功能 | 說明 |
|---------|------|------|
| 0 | 粗抽閥 | 粗抽階段開啟 |
| 1 | 細抽閥 | 細抽階段開啟 |
| 2 | 破真空小閥 | 破真空初期開啟 |
| 3 | 破真空大閥 | 破真空加速開啟 |

| 軸號 | 名稱 | 功能 |
|------|------|------|
| 0 | Z1 | 龍門左側 Z 軸 |
| 1 | Z2 | 龍門右側 Z 軸 |
| 2 | Valve | 蝶閥控制軸 |

---

## 檔案結構

```
d:\git\VacuumDryer\
├── VacuumDryer.sln
│
├── VacuumDryer.Core\              # 核心邏輯層
│   ├── Motion\
│   │   └── DualZController.cs     # 雙Z軸控制器
│   ├── Process\
│   │   ├── ProcessState.cs        # 流程狀態/參數
│   │   └── VacuumProcessController.cs  # 流程控制器
│   └── Data\
│       └── DataLogger.cs          # 資料記錄
│
├── VacuumDryer.Hardware\          # 硬體抽象層
│   ├── IAxis.cs                   # 軸介面
│   ├── IMotionCard.cs             # 控制卡介面
│   ├── IDigitalIO.cs              # 數位IO介面
│   ├── Simulation\                # 模擬實作
│   │   ├── SimulatedAxis.cs
│   │   └── SimulatedMotionCard3Axis.cs
│   └── Delta\                     # 台達驅動
│       ├── EtherCatDll.cs         # P/Invoke
│       ├── DeltaEtherCatAxis.cs
│       └── DeltaPciL221MotionCard.cs
│
└── VacuumDryer.UI\                # 人機介面層
    ├── App.xaml
    └── Views\
        ├── MainWindow.xaml        # 主畫面
        ├── JogDialog.xaml         # JOG 對話框
        └── ManualControlDialog.xaml
```
