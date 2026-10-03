# Immune War – Logic game hiện tại (nhánh Map)

Cập nhật: 2026-10-03 · Đọc từ code nhánh `Map`, commit `b8675ab`

## Tổng quan

Game đang chơi được là một tower defense đơn giản: đặt 6 loại tế bào miễn dịch lên các ô cố định, chặn virus đi theo đường mạch máu về cơ quan ở giữa màn hình. Nhiều hệ thống trong spec (Fever, nhiễm trùng, đột biến, boss, kỹ năng) đã có code và test nhưng **chưa được nối vào màn chơi thật**.

Tài liệu này mô tả logic đọc từ code nhánh `Map`, commit `b8675ab`. Mọi con số lấy từ file data trong `Assets/ImmunWar/Data` và code, không lấy từ spec.

**Cách đọc:** mỗi mục ghi "Đang chạy" (người chơi thấy trong game) hoặc "Có code, chưa dùng" (có class/test, nhưng màn chơi không gọi tới). Đây là điểm cần chú ý nhất khi đối chiếu spec.

### Ba lớp code

| Lớp | File chính | Vai trò |
| --- | --- | --- |
| Màn chơi thực tế | `Scripts/UI/PlayableBattleView.cs`, `PlayableMenuView.cs` | Tự dựng UI lúc chạy, chứa vòng lặp tick, va chạm, bắn, thưởng ATP, thắng/thua. Đây là logic người chơi trải nghiệm. |
| Hệ thống lõi | `Scripts/Battle`, `Combat`, `Economy`, `StatusEffects`, `Progression`, `Persistence` | Các class thuần logic có test: state machine, placement, wave, targeting, Fever, boss, mutation, infection, save. Màn chơi chỉ dùng một phần. |
| Thử nghiệm / demo | `Battle/Placement/GridSystem`, `StrategicNodeManager`, `Economy/EnhancedATPSystem`, `Tutorial`, `Performance` | Hệ lưới 12×8, ô chiến lược, ATP nâng cao, tutorial. Không màn chơi nào gọi tới. |

### Luồng scene

1. `Bootstrap` nạp `GameCatalog` (Resources/ImmuneWar) và file save, rồi chuyển sang `MainMenu`.
2. Khi scene `MainMenu` nạp, `PlayableSceneStartup` gắn `PlayableMenuView` (chọn cơ quan, Play, Quit).
3. Khi scene `Battle` nạp, nó gắn `PlayableBattleView`. Chỉ có một scene `Battle` dùng chung cho cả 3 map; map lấy từ lựa chọn ở menu.

## Vòng đời trận đấu

Một trận chạy tự động từ wave đầu đến wave cuối, không có nút gọi wave sớm. Thắng khi dọn hết mọi wave; thua khi Vitality cơ quan về 0. **Đang chạy.**

### Trình tự một trận

1. Vào scene Battle: trạng thái `Preparing`, ATP = `startingAtp` của map + 250 thưởng cố định (hard-code trong `PlayableBattleView.Start`).
2. Sau 6 giây, wave 1 tự bắt đầu, trạng thái chuyển `Running`. Trong 6 giây này người chơi đặt quân được.
3. Wave sinh quái theo từng nhóm (xem mục Enemy & wave). Wave kết thúc khi mọi quái đã sinh ra và đã chết hoặc tới cơ quan.
4. Hết wave nhưng còn wave sau: hiện "Wave clear!", chờ 6 giây rồi tự mở wave kế.
5. Hết wave cuối: `Victory`, bảng "ORGAN DEFENDED", lưu tiến độ mở map kế.
6. Vitality về 0 bất cứ lúc nào đang Running: `Defeat`, bảng "ORGAN OVERRUN".

### Trạng thái và điều khiển

| Trạng thái | Vào khi | Người chơi làm được |
| --- | --- | --- |
| Preparing | Mở scene Battle | Đặt quân |
| Running | Wave 1 bắt đầu; Resume | Đặt quân, mở menu pause |
| Paused | Bấm nút Menu (góc phải trên) | Resume, Restart, Main Menu. Không đặt quân được. |
| Victory | Dọn xong wave cuối | Play Again, Main Menu |
| Defeat | Vitality = 0 | Play Again, Main Menu |

Các trạng thái `Restarting` và `Exited` có trong enum nhưng màn chơi không dùng: Restart và Main Menu chỉ nạp lại scene.

### Nhịp mô phỏng

- Logic chạy theo tick cố định **30 tick/giây**, tối đa 5 tick mỗi frame để đuổi kịp khi giật.
- Thứ tự trong một tick: sinh quái → quái di chuyển hoặc đánh quân chặn → xóa quân đã chết → quân hồi chiêu, sinh ATP/hồi máu, bắn → kiểm tra thua → kiểm tra hết wave.
- Seed cố định 94721, nhưng hiện không có yếu tố ngẫu nhiên nào trong trận.

**Có code, chưa dùng:** hàng đợi lệnh `BattleCommandQueue`/`BattleCommandProcessor` (lệnh có ID, chống trùng, sắp theo tick) và luồng `BattleEvent`. Màn chơi gọi thẳng hàm thay vì đi qua lệnh.

## Kinh tế ATP & Vitality

Người chơi bắt đầu với 350 ATP ở Lung (370 ở Brain/Stomach) và 100 Vitality. ATP chỉ dùng để đặt quân; không có nâng cấp, bán hay hoàn tiền. **Đang chạy.**

### ATP

| Nguồn / chi | Giá trị | Ghi chú |
| --- | --- | --- |
| Khởi đầu theo map | Lung 100 · Brain 120 · Stomach 120 | `startingAtp` trong file map |
| Thưởng cố định đầu trận | +250 | Hard-code, cộng cho mọi map |
| Diệt quái | +`atpReward` của quái (virus 6) | Chỉ khi quân bắn chết; quái tới cơ quan không cho ATP |
| Energy Cell (vai Economy) | +5 mỗi 90 tick (3 giây) | Hard-code; tính theo tick của wave, dừng giữa hai wave |
| Đặt quân | −`atpCost` | Thiếu ATP thì báo "insufficient atp" |

Không có ATP tự tăng theo thời gian, không có thưởng cuối wave, không có hoàn tiền khi quân chết.

### Vitality cơ quan

- Tối đa 100 ở cả 3 map.
- Mỗi quái đi hết đường trừ `organDamage` = 20. **Chỉ 5 quái lọt là thua.**
- Platelet (vai Repair) hồi +3 Vitality mỗi 120 tick (4 giây), không vượt quá 100.

**Có code, chưa dùng:** `AtpEconomySystem` (đăng ký generator theo từng quân, phát sự kiện đổi ATP) và `EnhancedATPSystem` (ATP theo lưới, ô PowerNode +50%). Màn chơi cộng/trừ ATP trực tiếp.

## Map, đường đi và ô đặt quân

Mỗi map có 3 đường đi hội tụ về cơ quan ở tâm (0,0) và 5–6 ô đặt quân cố định. Mỗi ô chứa tối đa 1 quân, nhận mọi loại quân. **Đang chạy.**

| Map | Thứ tự | Đường đi | Ô đặt quân | Số wave |
| --- | --- | --- | --- | --- |
| Lung | 1 (mở sẵn) | 3: trên, giữa, dưới, đều vào từ bên trái | 6 (2 ô mỗi đường) | 2 |
| Brain | 2 (mở khi thắng Lung) | 3: từ trên, trái, dưới | 5 | 1 |
| Stomach | 3 (mở khi thắng Brain) | 3: từ trên-phải, trái, dưới-trái | 5 | 1 |

### Quy tắc đặt quân

1. Chọn quân ở thanh bên trái (bấm chuột; số 1–6 trên thẻ chỉ là nhãn, **phím tắt chưa hoạt động**).
2. Bấm vào ô "+" trên map.
3. Bị từ chối nếu: đang Pause, trận đã kết thúc, ô đã có quân, vai trò không được phép ở ô đó, hoặc thiếu ATP.
4. Quân chết thì ô được giải phóng, đặt lại được (trả đủ giá mới).

Lựa chọn quân giữ nguyên sau mỗi lần đặt, nên bấm liên tiếp nhiều ô sẽ đặt cùng loại. Không có xem trước tầm bắn, không hủy chọn, không bán/thu hồi quân.

### Di chuyển và bị chặn

- Quái đi thẳng theo các điểm mốc của đường (4 điểm mỗi đường), tốc độ = `moveSpeed` đơn vị/giây. 1 đơn vị = 76 px trên màn 1920×1080.
- **Mọi quân còn sống** (không riêng Macrophage) trong bán kính 1,3 đơn vị đều chặn quái lại. Quái bị chặn đứng yên và cắn quân đó mỗi 30 tick (1 giây).
- Sát thương cắn = max(2, `organDamage` × 0,5) = 10 với cả 4 loại quái hiện tại.
- Không giới hạn số quái một quân chặn được.

Trường `routeId`, `routeProgress` của ô đặt quân có trong data nhưng màn chơi không dùng; chỉ dùng tọa độ `position`.

**Có code, chưa dùng:** hệ lưới 12×8 (`GridSystem`, `GridConfiguration`) với 4 loại ô chiến lược (HighGround +50% tầm, Chokepoint, PowerNode +50% ATP, Amplifier +20% sát thương, giá ×1,25), vùng cấm đặt, `PathSystem`, `PlacementController`. Không scene nào dùng.

## Defender (quân mình)

Cả 6 loại quân đều chọn được ngay từ map đầu. Trong game thực tế chúng khác nhau chỉ ở chỉ số và hai hiệu ứng thụ động (sinh ATP, hồi Vitality); **kỹ năng riêng của B Cell, NK Cell, Platelet chưa chạy**.

| Quân (ID) | Vai | Giá ATP | Máu | Sát thương | Chu kỳ bắn | DPS thực | Tầm (đơn vị) | Hiệu ứng đang chạy |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Macrophage (`def_macrophage`) | Blocker | 30 | 240 | 8 | 1,1 s | 7,3 | 1,2 | — |
| T Cell (`def_tcell`) | Damage | 25 | 90 | 18 | 0,7 s | 25,7 | 3,2 | — |
| Energy Cell (`def_energy`) | Economy | 35 | 80 | 0 | — | 0 | 0 | +5 ATP mỗi 3 s |
| B Cell (`def_bcell`) | Support | 30 | 95 | 6 | 1,1 s | 5,5 | 3,0 | — |
| NK Cell (`def_nk`) | Burst | 45 | 105 | 24 | 1,3 s | 18,5 | 2,5 | — |
| Platelet (`def_platelet`) | Repair | 35 | 130 | 4 | 1,4 s | 2,9 | 2,2 | +3 Vitality mỗi 4 s |

DPS thực = sát thương ÷ (số tick hồi chiêu làm tròn lên ÷ 30). Ví dụ T Cell: 0,7 s × 30 = 21 tick, 18 ÷ 0,7 = 25,7.

### Cách quân chiến đấu

- **Chọn mục tiêu:** trong các quái nằm trong tầm, bắn con đi xa nhất theo % quãng đường; hòa thì theo ID. % tính riêng trên từng đường, nên quái trên đường ngắn và dài được so theo tỉ lệ chứ không theo khoảng cách tới cơ quan.
- **Đánh đơn mục tiêu, trúng ngay:** không có đạn bay, không sát thương lan, không trượt.
- **Chặn quái:** mọi quân, kể cả Energy Cell không tấn công, đều chặn và bị cắn 10 máu/giây mỗi quái (xem mục Map).
- **Máu quân:** không tự hồi, Platelet không chữa quân. Về 0 thì quân biến mất, ô trống.
- Vai "Blocker" của Macrophage chỉ là nhãn; nó không có quy tắc chặn riêng.

### Kỹ năng có trong data, chưa chạy

| Quân | Kỹ năng | Thông số trong data |
| --- | --- | --- |
| B Cell | `Cleanse` – gỡ nhiễm trùng khỏi ô | hồi chiêu 150 tick (5 s), gắn trạng thái `status_cleanse` |
| NK Cell | `MutantBurst` | hệ số 2,0, hồi chiêu 120 tick (4 s) |
| Platelet | `RepairAndSlow` | hệ số 15, hồi chiêu 180 tick (6 s) |

Không có code nào đọc `abilityType` để thực thi kỹ năng. Không có nâng cấp quân, cấp sao hay mở khóa quân theo tiến độ.

## Enemy & wave

Cả 3 map hiện chỉ sinh **Basic Virus**. Bacteria, Mutant và boss Super Pathogen có data đầy đủ nhưng không nằm trong wave nào, nên người chơi không bao giờ gặp.

### Chỉ số quái

| Quái (ID) | Máu | Tốc độ (đơn vị/s) | Trừ Vitality | Thưởng ATP | Có trong wave? |
| --- | --- | --- | --- | --- | --- |
| Basic Virus (`ene_virus`) | 45 | 0,45 | 20 | 6 | Có, cả 3 map |
| Bacteria (`ene_bacteria`) | 90 | 0,58 | 20 | 9 | Không |
| Mutant (`ene_mutant`) | 120 | 0,70 | 20 | 13 | Không |
| Super Pathogen (`ene_super_pathogen`, boss) | 1200 | 0,32 | 20 | 100 | Không |

Với tốc độ 0,45, một virus đi hết đường trong khoảng 14 s (đường dưới của Brain, dài 6,3) đến 25 s (đường trên của Lung, dài 11,2). T Cell cần 3 phát để diệt một virus.

### Cấu trúc wave

Một wave gồm nhiều nhóm. **Các nhóm sinh lần lượt, không song song**: nhóm sau chỉ bắt đầu khi nhóm trước sinh đủ số lượng. Trong nhóm, mỗi con sinh khi bộ đếm tick của wave chia hết cho khoảng cách của nhóm.

| Map | Wave | Các nhóm (đường · số con · cách nhau) | Tổng virus |
| --- | --- | --- | --- |
| Lung | 1 | trên · 6 · 35 tick → giữa · 7 · 40 tick | 13 |
| Lung | 2 | trên · 7 · 28 → giữa · 6 · 30 → dưới · 6 · 32 | 19 |
| Brain | 1 | trên · 4 · 30 → trái · 3 · 28 → dưới · 2 · 32 | 9 |
| Stomach | 1 | trên · 4 · 25 → trái · 5 · 24 → dưới · 3 · 30 | 12 |

Brain và Stomach ngắn hơn và ít quái hơn Lung, dù đứng sau Lung trong chiến dịch.

### Có trong data / code, chưa chạy

- **Boss Super Pathogen:** 3 pha theo % máu. Pha 1 (100%) dùng `SporeSpray` (×1,25, hồi 90 tick). Pha 2 (≤65%) thêm `MutationSurge` (×1,6), bất tử 20 tick khi chuyển pha. Pha 3 (≤30%) chỉ `MutationSurge`, bất tử 30 tick. `BossPhaseController` xử lý chuyển pha và bất tử, nhưng chưa có code thực thi hai kỹ năng.
- **Mutant:** gắn 2 đột biến (xem mục tiếp theo).
- Không có quái bay, quái tách đôi, quái tấn công từ xa, hay giáp/kháng.

## Cơ chế mở rộng: nhiễm trùng, đột biến, Fever

Cả ba cơ chế đều **có code, chưa chạy** trong màn chơi. Fever và nhiễm trùng được khởi tạo khi vào trận nhưng không có gì kích hoạt; đột biến thì không được khởi tạo. Tutorial tiếng Việt vẫn nhắc tới nhiễm trùng và Sốt.

### Fever (Sốt)

| Thông số | Giá trị trong data |
| --- | --- |
| Thanh nạp tối đa | 100 |
| Thời gian hiệu lực | 300 tick (10 s) |
| Hệ số sát thương | ×1,5 |
| Hệ số tốc độ | ×1,25 (code dự phòng dùng ×1,2 nếu thiếu data) |

Logic có sẵn: nạp đủ 100 → `Ready` → người chơi kích hoạt → `Active` 10 s → về 0, nạp lại. Không nạp thêm khi đang Active.

Chưa có: nguồn nạp thanh (không code nào gọi `AddCharge`), nút kích hoạt trên HUD, và việc áp hệ số vào sát thương/tốc độ. Chưa rõ "tốc độ" là tốc đánh của quân hay tốc độ quái.

### Nhiễm trùng và thanh tẩy

- Nhiễm trùng gắn lên **ô đặt quân**, cộng dồn tối đa 3 lớp, mỗi lần gắn làm mới thời gian 180 tick (6 s).
- Quân trên ô nhiễm bị giảm sát thương còn ×0,75, bất kể số lớp.
- B Cell `Cleanse` gỡ toàn bộ nhiễm trùng trên một ô.
- Chưa có: nguồn gây nhiễm (quái nào, khi nào), hiển thị ô bị nhiễm.
- **Lệch giữa data và code:** file `status_infection` ghi hệ số sát thương ×1,15 và tốc độ ×0,9; code lại hard-code ×0,75 và bỏ qua hai trường đó.

### Đột biến

| Đột biến | Trọng số | Máu | Tốc độ | Miễn nhiễm |
| --- | --- | --- | --- | --- |
| Resilient | 1 | ×1,6 | ×0,9 | `status_slow` (trạng thái này không tồn tại trong catalog) |
| Swift | 1 | ×0,85 | ×1,5 | — |

Logic có sẵn: chọn ngẫu nhiên theo trọng số, mỗi quái đột biến tối đa 1 lần. Code chỉ nhân máu, **bỏ qua hệ số tốc độ và miễn nhiễm**. Chưa có điều kiện kích hoạt đột biến (lúc sinh, theo thời gian, hay do boss).

## Chiến dịch, tiến độ và lưu game

Chiến dịch là chuỗi Lung → Brain → Stomach; thắng map này thì mở map kế. Tiến độ lưu vào file JSON trên máy. **Đang chạy.**

### Mở khóa map

- Lần đầu chơi chỉ mở Lung. Map chưa mở hiện chữ "LOCKED" và không bấm được.
- Thắng một map lần đầu: đánh dấu hoàn thành, mở map kế, ghi file. Thắng lại map đã xong thì không ghi gì.
- Thứ tự mở khóa lấy theo thứ tự map trong `GameCatalog`, không theo trường `nextMapId` của map (hai cái hiện trùng nhau).
- Thua không mất gì. Không có sao, điểm, phần thưởng, tiền tệ ngoài trận hay nâng cấp vĩnh viễn.
- Xong cả 3 map: code có cờ `CampaignFinished`, nhưng game không hiện màn kết thúc chiến dịch.

### File lưu

| Mục | Chi tiết |
| --- | --- |
| Vị trí | `Application.persistentDataPath/save.json`, kèm `save.backup.json` |
| Nội dung | Phiên bản schema (1), số thứ tự lần lưu, thời điểm lưu, map đã mở, map đã xong, map chọn gần nhất, âm lượng nhạc/hiệu ứng |
| Cách ghi | Ghi ra file tạm, đọc lại kiểm tra, chép bản cũ sang backup, rồi thay file chính |
| Cách đọc | Lấy bản có số thứ tự lớn hơn giữa file chính và backup; lọc bỏ ID map không hợp lệ; hỏng cả hai thì tạo mới |
| Lưu khi nào | Chỉ khi thắng map lần đầu |

Không lưu trận đang dở; thoát giữa trận là mất trận đó. Map chọn ở menu chỉ giữ trong phiên chơi, không ghi xuống file. Có class migration schema nhưng mới có phiên bản 1.

## UI, input, âm thanh

Toàn bộ UI màn chơi được dựng bằng code lúc chạy, chữ tiếng Anh hard-code, chỉ điều khiển bằng chuột. **Game hiện không phát âm thanh nào.**

### Màn hình đang có

| Màn | Thành phần |
| --- | --- |
| Menu chính | Tiêu đề IMMUNE WAR, 3 thẻ cơ quan (khóa/mở), dòng "SELECTED", nút PLAY, QUIT |
| Trận đấu – HUD trên | ATP, thanh máu cơ quan, "WAVE x/y", nút Menu |
| Trận đấu – cột trái | 6 thẻ quân (icon, giá ATP, số 1–6), tên quân đang chọn |
| Trận đấu – bản đồ | Ảnh nền map, cơ quan ở giữa, vệt đường đi mờ, ô "+", thanh máu trên quân và quái, hiệu ứng trúng đòn/nhận ATP |
| Trận đấu – dòng trạng thái | Thông báo dưới đáy (chuẩn bị, wave tới, lỗi đặt quân) |
| Pause | RESUME, RESTART, MAIN MENU |
| Kết quả | "ORGAN DEFENDED" / "ORGAN OVERRUN", PLAY AGAIN, MAIN MENU |

Không có: màn Settings, credits/NOTICE, cảnh báo y khoa, màn loading, nút tăng tốc, nút gọi wave sớm, thông tin chi tiết quân/quái, hiển thị tầm bắn.

### Có code, chưa chạy

- **Âm thanh:** `AudioService` có trong scene MainMenu và Battle (nhạc nền, hiệu ứng, mixer, fade) nhưng không code nào gọi phát nhạc hay hiệu ứng. `PlacementAudioManager` không được dùng.
- **Ngôn ngữ:** có file `en-US.json` và `vi-VN.json` (tutorial, cảnh báo y khoa) nhưng không có code nạp chúng.
- **Tutorial:** `TutorialPromptController` có trong scene Battle, `PlacementTutorialSystem` có code; không cái nào được gọi.
- **Settings:** `SettingsRepository` lưu âm lượng vào file save; `SettingsController` không nằm trong scene nào.
- **Trợ năng:** `BattleAccessibilityPresenter` có nhãn chữ + ký hiệu cho infection, mutation, fever, boss, victory, defeat; không ai gọi.
- **Input System:** dự án khai báo Input System nhưng màn chơi không đọc bàn phím (không phím tắt, không Esc để pause).
- Các HUD, menu dựng sẵn (`BattleHudController`, `TacticalHudController`, `MapSelectController`, `PauseMenuController`, `BattleResultController`, `SafeAreaController`) không nằm trong scene nào.

## Đối chiếu với spec

Theo code hiện tại, 10/24 yêu cầu gameplay (FR-001–023, FR-030) đạt, 8 đạt một phần, 6 chưa có. Phần thiếu lớn nhất là Fever, nhiễm trùng, đột biến, boss, âm thanh và Settings. Nguồn: `specs/001-immune-war-game/spec.md`; FR-024–029 là quy trình asset, không thuộc logic game nên không đánh giá ở đây.

| FR | Yêu cầu (tóm tắt) | Hiện trạng | Ghi chú |
| --- | --- | --- | --- |
| FR-001 | Vào trận đầu không cần tài khoản/mạng | Đạt | Menu → Play |
| FR-002 | Trận có mục tiêu, đường, ô, ATP, wave, thắng/thua | Đạt | |
| FR-003 | Xem tên, vai trò, giá, vùng đặt trước khi đặt | Một phần | Chỉ có icon + giá; không hiện vai trò, tầm bắn |
| FR-004 | Đặt sai không mất ATP, có báo lỗi | Đạt | Báo bằng dòng chữ dưới đáy |
| FR-005 | Trừ ATP đúng 1 lần, giữ ô | Đạt | Ô trống lại khi quân chết (nếu spec yêu cầu nút thu hồi/bán quân thì hạ xuống "Một phần") |
| FR-006 | Quái theo wave, theo đường, bị chặn, chỉ trừ máu khi tới đích | Đạt | |
| FR-007 | Chọn mục tiêu hợp lệ, nhất quán | Đạt | Bắn con đi xa nhất trong tầm |
| FR-008 | Macrophage chặn, T Cell sát thương, Energy Cell ATP | Một phần | Mọi quân đều chặn, không riêng Macrophage |
| FR-009 | B Cell, NK, Platelet có vai hỗ trợ/khắc chế riêng | Một phần | Kỹ năng chưa chạy; B Cell, NK chỉ là quân bắn yếu/mạnh hơn |
| FR-010 | Virus (P0) + Bacteria, Mutant, Super Pathogen | Một phần | Chỉ virus xuất hiện trong wave |
| FR-011 | Thưởng ATP đúng 1 lần | Đạt | |
| FR-012 | Vitality luôn hiện, không dưới 0/quá max | Đạt | |
| FR-013 | Nhiễm trùng: điều kiện, hiệu ứng, thanh tẩy, nhìn thấy được | Chưa có | Có code, chưa nối |
| FR-014 | Đột biến: trigger, thay đổi, cách đối phó | Chưa có | Có code, chưa nối |
| FR-015 | Fever: thanh nạp, nút kích hoạt, thời hạn, hồi | Chưa có | Có code, chưa nối, không có UI |
| FR-016 | Pause/resume/restart/thắng/thua không nhân đôi | Đạt | Restart = nạp lại scene |
| FR-017 | 3 map khác nhau về cảnh, đường, thành phần wave | Một phần | Ảnh và đường khác; wave đều chỉ có virus |
| FR-018 | Thắng thì lưu + mở map kế; chơi lại không xóa tiến độ | Đạt | |
| FR-019 | Boss ít nhất 2 pha rõ ràng | Chưa có | Data 3 pha có, boss không xuất hiện |
| FR-020 | HUD: ATP, vitality, wave, quân, ô hợp lệ, Fever, pause | Một phần | Thiếu Fever và báo ô hợp lệ |
| FR-021 | Phản hồi riêng cho từng sự kiện chính | Một phần | Có hiệu ứng trúng đòn, ATP; không âm thanh, không infection/Fever/boss |
| FR-022 | Chỉnh âm lượng nhạc/hiệu ứng riêng, có lưu | Chưa có | Có lưu trữ, không có màn Settings |
| FR-023 | Thiếu asset phụ vẫn chơi được | Một phần | Thiếu icon thì hiện ô màu + chữ cái; chưa kiểm hết |
| FR-030 | Không tuyên bố y khoa; có cảnh báo | Chưa có | Câu cảnh báo chỉ nằm trong file ngôn ngữ, không hiện |

### Quy tắc code tự đặt, cần xác nhận với spec

Các điểm sau không trái spec nhưng spec không quy định số cụ thể; cần chốt là cố ý hay tạm.

- [ ] +250 ATP hard-code đầu mọi trận (nên đưa vào `startingAtp` của map?)
- [ ] Mọi quân đều chặn quái trong 1,3 đơn vị, không giới hạn số quái bị chặn
- [ ] Quái cắn quân = 50% `organDamage`, mỗi 1 giây
- [ ] Energy Cell +5 ATP/3 s và Platelet +3 Vitality/4 s hard-code, không lấy từ data
- [ ] Wave tự mở sau 6 giây, không có nút gọi sớm
- [ ] Các nhóm trong wave sinh lần lượt chứ không song song
- [ ] 5 quái lọt là thua (organDamage 20 / Vitality 100)
- [ ] Brain và Stomach chỉ có 1 wave, ít quái hơn Lung
- [ ] Hệ số nhiễm trùng: data ghi ×1,15 nhưng code dùng ×0,75
