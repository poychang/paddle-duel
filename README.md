# 1972 Arcade

一款受 1972 年早期投幣式電子桌球遊戲啟發的 Windows 遊戲。目前名稱與視覺皆為開發階段素材；本專案與 Atari 無關，也不使用原始遊戲的商標、程式、美術、字型或錄音。

## 目前進度

- 可調整大小的 WinUI 3 自訂標題列視窗，預設不進入全螢幕。
- 本機雙人模式：`W` / `S` 與方向鍵控制球拍。
- 單人模式：具反應延遲且受相同移動速度限制的電腦對手。
- 單人模式可選 Easy／Medium／Hard 難度；難度只調整 AI controller，不改變遊戲物理。
- 標題列齒輪可開啟設定，調整 AI 難度與 Windowed／Full Screen 顯示模式。
- 120 Hz 固定步進核心、八段球拍反射、回擊加速與 11 分勝負。
- 全螢幕由玩家透過標題列按鈕主動切換。
- 標題列資訊按鈕會顯示 Player 1／Player 2 按鍵，遊戲中開啟時會自動暫停。
- 遊戲中按 `Esc` 可暫停或繼續；放棄比賽返回主選單不會消耗遊玩次數。
- 視窗失焦或最小化時會自動暫停並清除按鍵，回到前景後需按「CONTINUE」才能繼續。
- 視窗大小、位置與玩家選擇的全螢幕模式會保存，還原時會依目前螢幕工作區校正。
- 開發版限制單一程序，避免多開視窗同時操作免費額度資料。
- 每日免費 3 次的純核心模型已完成，包含 UTC 重置、完成才扣與時鐘回撥保護。
- 額度狀態使用 Windows LocalState 路徑與原子 JSON 替換，損毀資料會隔離保留。
- 額度完成紀錄只保留最近 400 個 UTC 日，避免本機狀態無限成長。
- 單人與雙人開局皆會建立免費額度 session，只有完成 11 分比賽才實際扣除。
- 主選單顯示今日剩餘免費次數與下一個 00:00 UTC 重置日期。
- 額度耗盡時會停用開局並顯示 Store 占位入口，不顯示假價格或執行假交易。

Microsoft Store 消耗型商品、MSIX 封裝與正式商店素材尚未實作；目前 Store 畫面只說明尚未連線，不會執行任何購買流程。

Store commerce 目前已具備平台無關的商品、餘額、購買與 fulfillment gateway fake；真實 `StoreContext` adapter 與 Partner Center 商品尚未接入。
Entitlement coordinator 已實作免費優先與 1-play／10-play fallback；真實 Store fulfillment 尚未接入。
免費額度耗盡時，付費局開始前會先查詢 Store balance，並將選定商品 pool 保存於 entitlement session。
付費 fulfillment 會先寫入 pending journal；失敗時保留 tracking ID，供之後重試。
App 啟動與回到前景時會嘗試重試 pending fulfillment；Store 暫時不可用不會阻塞遊戲啟動。
Windows `StoreContext` adapter 已可編譯並映射 Store 結果；真實 runtime 驗證仍需 package identity 與 private flight。
Store UI 已接入商品 discovery 與購買協調器；沒有 package identity 或 Store 商品時，購買按鈕會保持停用，不會模擬購買。
Fake commerce tests 已覆蓋取消、餘額錯誤、pending retry、重啟 session 與重複 fulfillment。

## 建置與執行

需求：Windows 10 22H2 或 Windows 11，以及 .NET 10 SDK。

```powershell
dotnet restore pong-in-1972.sln
dotnet build pong-in-1972.sln -c Debug -p:Platform=x64
dotnet test tests/Arcade1972.Tests/Arcade1972.Tests.csproj -c Debug
dotnet run --project src/Arcade1972.App/Arcade1972.App.csproj -c Debug -p:Platform=x64
```

目前的 App 是供開發驗證使用的 unpackaged WinUI 3 應用。正式 MSIX/Store 建置仍需要安裝 Visual Studio 的 Windows App SDK 與 MSIX 工作負載，並與 Partner Center 的應用程式識別建立關聯。

## 專案結構

- `src/Arcade1972.Core`：不依賴 Windows UI 的 deterministic 遊戲規則。
- `src/Arcade1972.Infrastructure`：可測試的檔案持久化與平台邊界實作。
- `src/Arcade1972.App`：WinUI 3 視窗、輸入與 XAML 畫面。
- `tests/Arcade1972.Tests`：物理、勝負、固定步進與 AI 測試。
- `docs/game-reference.md`：歷史規則依據與目前 deterministic baseline。
- `docs/dev-knowledge.md`：開發中經驗證且可重用的技術知識，會隨專案演進汰舊更新。
