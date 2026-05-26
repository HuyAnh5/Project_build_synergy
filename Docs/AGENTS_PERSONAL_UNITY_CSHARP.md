# AGENTS.md — Personal Unity/C# Coding Style & OOP Rules

> **Mục đích:** Quy tắc mặc định cho Codex khi viết, sửa hoặc refactor code trong các project Unity/C# cá nhân.  
> **Phạm vi:** Cách tổ chức file, đặt tên, trình bày C# script, OOP, refactor và an toàn dữ liệu Unity.  
> **Tính tái sử dụng:** File này không giả định chủ đề, tính năng, framework nội bộ hay cấu trúc riêng của một project cụ thể.

---

## 0. Quy tắc ưu tiên cho Codex

Khi nhận một task code, Codex phải làm theo thứ tự:

1. Đọc các script, asset, prefab/scene, assembly definition và tài liệu liên quan trực tiếp đến task.
2. Kiểm tra convention đang tồn tại trong repository trước khi tạo file hoặc đổi cấu trúc.
3. Dùng quy tắc trong file này làm mặc định cho code mới và phần code được chủ động refactor.
4. Thực hiện thay đổi nhỏ nhất nhưng giải quyết đúng yêu cầu.
5. Không tự ý đổi hành vi hiện có, rename hàng loạt, move asset hoặc rewrite cả hệ thống nếu task không yêu cầu.
6. Bảo vệ reference Unity đã serialize trong Prefab, Scene và ScriptableObject.
7. Báo rõ file đã sửa, rủi ro và validation sau khi hoàn thành.

### Khi convention hiện tại khác file này

- Nếu project đã có convention rõ ràng và việc đổi style gây rủi ro hoặc tạo code không nhất quán, giữ convention hiện có trong khu vực đó và báo lại.
- Nếu tạo module mới chưa có style cũ ràng buộc, áp dụng file này.
- Không làm một đợt “dọn style” toàn project nếu chưa được yêu cầu riêng.

---

## 1. Giới hạn kích thước file code

### Quy tắc bắt buộc

- Một file code do mình tự viết, đặc biệt là file `.cs`, **không được vượt quá 500 dòng**.
- Mục tiêu tốt hơn là giữ file đủ ngắn để đọc dễ dàng; không đợi đến dòng 500 mới tách.
- Không dùng `#region` để che việc một class đang quá lớn hoặc ôm quá nhiều trách nhiệm.
- Khi một thay đổi sẽ làm file gần/vượt 500 dòng, Codex phải cân nhắc tách class hoặc tách responsibility trước khi thêm code.

### Khi gặp file đã vượt 500 dòng

- Không tiếp tục nhồi thêm một khối logic lớn vào file đó.
- Nếu tách an toàn trong phạm vi task, tách phần trách nhiệm liên quan thành file/class rõ ràng.
- Nếu tách có nguy cơ phá behaviour hoặc serialized references, thực hiện thay đổi tối thiểu cần thiết rồi báo: file đang vượt giới hạn và cần một task refactor riêng.

### Không áp dụng cưỡng ép cho

- File auto-generated.
- Package/plugin bên thứ ba.
- Code vendor/imported không thuộc phạm vi sửa.
- File cấu hình hoặc dữ liệu không phải source code, trừ khi task quy định khác.

---

## 2. Folder Structure

### 2.1 Nguyên tắc

- Ưu tiên cấu trúc folder đã tồn tại trong project.
- Asset/code mới phải nằm đúng khu vực của project, không rải file tùy tiện trực tiếp dưới `Assets/`.
- Tổ chức theo loại asset và theo feature khi số lượng file đủ lớn.

### 2.2 Cấu trúc gợi ý cho project mới

Thay `<ProjectName>` bằng tên thật của project.

```text
Assets/
├── Plugins/                       # Package/plugin yêu cầu đặt ở đây
├── ExternalPackages/              # Asset/package bên ngoài nếu cần lưu trong Assets
└── <ProjectName>/
    ├── Animations/
    ├── Art/
    │   ├── Materials/
    │   ├── Sprites/
    │   └── Textures/
    ├── Audio/
    ├── Prefabs/
    ├── Scenes/
    ├── ScriptableObjects/
    └── Scripts/
        ├── Core/
        ├── Features/
        ├── UI/
        ├── Utilities/
        └── Editor/
```

### 2.3 Folder rules

- Code runtime chỉ nằm trong runtime folders.
- Custom inspector/editor tool phải nằm trong `Editor/` folder hoặc Editor-only assembly.
- Third-party asset không trộn với code/asset do project tự tạo.
- Không tự ý di chuyển asset hiện có nếu việc move không cần cho task hoặc có thể làm phức tạp reference/version control.

---

## 3. File và Asset Naming

| Loại | Quy tắc mặc định | Ví dụ |
|---|---|---|
| Folder | `PascalCase` | `Scripts`, `SettingsMenu`, `Characters` |
| C# script | `PascalCase` | `SettingsController.cs`, `AudioService.cs` |
| Scene | `PascalCase` | `MainMenu.unity`, `LoadingScene.unity` |
| ScriptableObject asset | `PascalCase` | `DefaultSettings.asset`, `LevelConfig01.asset` |
| Texture / Sprite | lowercase `snake_case` | `button_close_icon`, `background_main_menu` |
| Prefab | `PascalCase`, có thể thêm prefix nhóm rõ nghĩa | `UI_SettingsPopup`, `PlayerAvatar` |

### Tránh

```text
NewScript.cs
Test123.cs
Manager.cs
Stuff.cs
FinalFinal.prefab
```

Tên file/class phải giúp hiểu trách nhiệm mà không cần mở file.

---

## 4. Namespace

### 4.1 Quy tắc

- Codex phải kiểm tra namespace hiện có trong project trước khi tạo class mới.
- Nếu project đã dùng namespace, code mới phải đi theo namespace đó.
- Nếu đây là module mới trong project có namespace gốc, dùng namespace con theo feature khi hợp lý.
- Không ghi placeholder vào production code.

Ví dụ minh hoạ:

```csharp
namespace MyProject.UI
{
    public class SettingsView : MonoBehaviour
    {
    }
}
```

Trong đó `MyProject` phải được thay bằng namespace thật của repository.

### 4.2 Project chưa có namespace

Nếu project hiện tại chưa dùng namespace:

- Không tự bọc toàn bộ code cũ vào namespace chỉ trong một task nhỏ.
- Với code mới lớn hoặc khi được yêu cầu chuẩn hoá kiến trúc, đề xuất namespace dựa trên tên project và nêu phạm vi migration cần thiết.

---

## 5. Class Naming và vai trò

Tên class phải thể hiện đúng trách nhiệm.

| Pattern | Dùng cho | Ví dụ |
|---|---|---|
| `XxxController` | Điều khiển behaviour/runtime flow của một object hoặc feature | `MenuController`, `CharacterController` |
| `XxxView` | Hiển thị UI/visual state, nhận dữ liệu để render | `SettingsView`, `LoadingView` |
| `XxxData` / `XxxModel` | Dữ liệu hoặc state object | `UserSettingsData`, `LevelModel` |
| `XxxDefinition` / `XxxConfig` | Dữ liệu cấu hình/authoring | `AudioConfig`, `LevelDefinition` |
| `XxxDatabase` | Tập dữ liệu hoặc lookup nhiều entry | `ItemDatabase` |
| `XxxService` | Dịch vụ xử lý nghiệp vụ không phụ thuộc trực tiếp vào scene object | `SaveService`, `AudioService` |
| `XxxManager` | Điều phối hệ thống cấp cao có lifecycle/ownership rõ ràng | `SceneManager`, `InputManager` |
| `XxxEditor` | Custom inspector/tool chạy trong Editor | `LevelDefinitionEditor` |
| `IXxx` | Interface có mục đích thay thế/đa implementation rõ ràng | `ISaveProvider` |

### Không lạm dụng `Manager`

Không dùng `Manager` chỉ vì class chứa nhiều method. Nếu class điều khiển một object/UI cụ thể, thường nên là `Controller` hoặc `View`. Nếu class chứa data, không nên là `Manager`.

---

## 6. Naming trong C#

### 6.1 Fields, properties, locals và methods

| Thành phần | Quy tắc | Ví dụ |
|---|---|---|
| Private field | `m_` + `camelCase` | `m_currentIndex`, `m_isVisible` |
| Serialized private field | `[SerializeField] private` + `m_` + `camelCase` | `m_btnConfirm` |
| Public property | `PascalCase` | `CurrentIndex`, `IsVisible` |
| Local variable / parameter | `camelCase` | `targetIndex`, `isEnabled` |
| Method | `PascalCase`, bắt đầu bằng động từ | `RefreshView()`, `ApplySettings()` |
| Interface | `I` + `PascalCase` | `ILoadable` |
| Constant/static readonly | `UPPER_SNAKE_CASE` | `MAX_ITEM_COUNT` |
| Event | Việc đã xảy ra, không prefix `On` | `SettingsChanged`, `LoadingCompleted` |

### 6.2 Encapsulation mặc định

Ưu tiên field private và expose qua property/method thay vì public mutable field.

```csharp
[SerializeField] private int m_initialValue;

private int m_currentValue;

public int CurrentValue
{
    get { return m_currentValue; }
}
```

Chỉ dùng public mutable field khi cấu trúc project hiện tại hoặc yêu cầu kỹ thuật thực sự cần.

### 6.3 Boolean

Bool phải đọc như trạng thái hoặc câu hỏi.

```csharp
private bool m_isInitialized;
private bool m_hasSelection;

public bool IsVisible { get; private set; }
public bool CanSubmit { get; private set; }
```

Tránh tên mơ hồ:

```csharp
private bool m_flag;
private bool m_check;
private bool m_temp;
```

### 6.4 Collections

| Kiểu | Quy tắc | Ví dụ |
|---|---|---|
| Array | Tên số nhiều rõ nghĩa | `m_pageIds` |
| `List<T>` | Có `list` trong tên | `m_listEntries` |
| `Dictionary<TKey, TValue>` | Có `dict` trong tên | `m_dictEntryById` |

```csharp
private int[] m_pageIds;
private List<EntryData> m_listEntries;
private Dictionary<string, EntryData> m_dictEntryById;
```

### 6.5 Unity references

Serialized reference tới Unity component nên cho biết loại component và mục đích.

| Loại | Prefix gợi ý | Ví dụ |
|---|---|---|
| `Button` | `btn` | `m_btnConfirm` |
| TMP/Text | `txt` | `m_txtTitle` |
| `Image` | `img` | `m_imgIcon` |
| `Animator` | `anim` | `m_animPanel` |
| `GameObject` | `obj` | `m_objContainer` |
| `Transform` | `transform` hoặc tên vai trò rõ | `m_transformContent` |
| `CanvasGroup` | `canvasGroup` | `m_canvasGroupPopup` |
| `ScrollRect` | `scroll` | `m_scrollContent` |

Dùng prefix đã nhất quán trong repository nếu project hiện tại có quy tắc khác rõ ràng.

---

## 7. Constants, IDs và hard-coded values

### Quy tắc

- `const` và `static readonly` dùng `UPPER_SNAKE_CASE`.
- Không rải string key, scene name, resource path hoặc identifier quan trọng trực tiếp ở nhiều nơi.
- Tìm class constants/definitions hiện có trước khi tạo class mới.

```csharp
private const int MAX_RETRY_COUNT = 3;
private const string SELECTED_PROFILE_KEY = "SelectedProfile";
```

Không nên lặp lại:

```csharp
PlayerPrefs.GetString("SelectedProfile");
PlayerPrefs.SetString("SelectedProfile", profileId);
```

Nên dùng cùng một constant/definition rõ ràng.

---

## 8. Function Naming và trách nhiệm

### 8.1 Method thông thường

Method dùng `PascalCase` và bắt đầu bằng động từ.

```csharp
private void RefreshContent()
{
}

private bool ValidateInput()
{
    return true;
}

private void SaveSettings()
{
}
```

### 8.2 Coroutine

Coroutine dùng prefix `IE`.

```csharp
private IEnumerator IEFadeIn()
{
    yield break;
}

private IEnumerator IELoadContent()
{
    yield break;
}
```

### 8.3 UI callback

Callback của button dùng prefix `OnButton`.

```csharp
private void OnButtonConfirm()
{
}

private void OnButtonCancel()
{
}
```

### 8.4 Event callback

Callback phản ứng với event đã xảy ra dùng `OnNounVerbed`.

```csharp
private void OnSettingsChanged()
{
}

private void OnLoadingCompleted()
{
}
```

### 8.5 Một method chỉ có một nhiệm vụ chính

Một method điều phối có thể gọi nhiều bước, nhưng chi tiết của các bước khác nhau nên nằm ở method/class riêng.

```csharp
public void Submit()
{
    if (!ValidateInput())
    {
        return;
    }

    ApplyInput();
    SaveState();
    RefreshView();
}
```

Nếu method trở nên dài hoặc trộn validation, persistence, UI và animation phức tạp, hãy tách trách nhiệm.

---

## 9. Member Ordering Trong Một C# Class

Sắp xếp member theo thứ tự sau:

```csharp
// Constants

// Static fields

// Serialized fields / Inspector-assigned references

// Other private fields

// Properties

// Events

// Unity lifecycle methods

// Public methods

// Private helper methods

// UI/Event callbacks

// Cleanup lifecycle methods
```

### Thứ tự Unity lifecycle methods

```csharp
Awake();
OnEnable();
Start();
Update();
FixedUpdate();
LateUpdate();

// ... custom/public/private/callback methods ...

OnDisable();
OnDestroy();
```

Không chèn method mới ngẫu nhiên vào giữa các nhóm làm file khó đọc.

---

## 10. Formatting

### 10.1 Braces bắt buộc

Luôn dùng `{ }` cho `if`, `else`, `switch`, `for`, `foreach`, `while` và các control statement khác, kể cả khi chỉ có một dòng.

Đúng:

```csharp
if (m_isVisible)
{
    RefreshView();
}
else
{
    HideView();
}
```

Sai:

```csharp
if (m_isVisible)
    RefreshView();
```

### 10.2 Property đơn giản

Property ngắn có thể trình bày gọn nếu vẫn rõ ràng. Mặc định ưu tiên form có braces để đồng nhất và dễ thêm logic debug.

```csharp
public bool IsVisible
{
    get { return m_isVisible; }
}
```

### 10.3 `#region`

Chỉ dùng `#region` khi nó nhóm một trách nhiệm rõ trong một class có độ dài hợp lý.

- Không tạo region cho mỗi method.
- Không dùng region thay cho việc tách class đang quá lớn.
- Khi class gần 500 dòng hoặc có nhiều nhóm region không liên quan, cân nhắc tách file/class.

---

## 11. OOP — Quy tắc chia trách nhiệm

### 11.1 Single Responsibility

Mỗi class chỉ nên có một trách nhiệm chính và một lý do chính để thay đổi.

Một class không nên đồng thời:

- Lưu cấu hình/data.
- Xử lý runtime logic phức tạp.
- Render UI.
- Lưu/đọc dữ liệu bền vững.
- Chạy animation.
- Chứa Editor tooling.

Tách thành các class có tên và mục đích rõ khi các trách nhiệm thay đổi độc lập.

### 11.2 Separation of Concerns

| Concern | Nên chịu trách nhiệm | Không nên chịu trách nhiệm |
|---|---|---|
| Data/Config/Definition | Lưu dữ liệu và cấu hình | Trực tiếp render UI hoặc điều phối scene phức tạp |
| Model/Runtime State | Lưu trạng thái runtime cần kiểm soát | Editor layout |
| Controller | Nhận input/điều phối behaviour của feature/object | Trở thành database hoặc view khổng lồ |
| Service | Xử lý nghiệp vụ/dịch vụ dùng lại được | Phụ thuộc ngầm vào nhiều scene object |
| View | Hiển thị và cập nhật visual/UI | Tự quyết định toàn bộ business rule |
| Editor | Cải thiện authoring/Inspector | Là nơi duy nhất chứa logic runtime cần thiết |

### 11.3 Encapsulation

- Giữ state nội bộ là `private`.
- Cho phép truy cập thông qua property read-only hoặc method có kiểm soát.
- Không expose public mutable state chỉ để class khác sửa cho nhanh.
- Validation của state phải nằm ở class sở hữu state hoặc service chịu trách nhiệm rõ ràng.

### 11.4 Composition trước inheritance sâu

Ưu tiên component/object cộng tác có trách nhiệm rõ ràng thay vì cây kế thừa sâu.

Dùng inheritance khi có quan hệ thực sự ổn định và class con tuân thủ hợp đồng của class cha. Dùng interface khi có nhiều implementation thực tế hoặc cần tách dependency để kiểm thử/thay thế.

Không tạo abstraction chỉ vì “có thể sau này cần”.

### 11.5 Dependency rõ ràng

- Với `MonoBehaviour`, dùng Inspector reference hoặc initialization rõ ràng khi phù hợp.
- Với pure C# class, truyền dependency qua constructor khi phù hợp.
- Không dùng `Find...`, singleton hoặc global state tràn lan nếu có thể truyền reference rõ ràng.
- Không tạo dependency vòng tròn giữa các class.

### 11.6 Khi nào cần tách class/file

Codex phải đánh giá tách class khi có một hoặc nhiều dấu hiệu:

- File gần/vượt 500 dòng.
- Class chứa nhiều nhóm responsibility khác nhau.
- Một thay đổi nhỏ phải chỉnh nhiều vùng không liên quan trong cùng file.
- UI, state, persistence, runtime processing hoặc Editor code bị trộn lẫn.
- Method dài và không thể mô tả bằng một hành động chính.
- Class cần quá nhiều region để trông có vẻ dễ đọc.

Tách class phải giúp rõ trách nhiệm; không tách thành nhiều file vụn không có mục tiêu.

---

## 12. Unity Safety Rules

### 12.1 Serialized fields

Không tự ý rename hoặc xóa serialized field:

```csharp
[SerializeField] private GameObject m_objPanel;
```

Field có thể đã được lưu trong Prefab, Scene hoặc ScriptableObject.

Khi bắt buộc rename field, cân nhắc migration phù hợp:

```csharp
using UnityEngine.Serialization;

[FormerlySerializedAs("m_objOldPanel")]
[SerializeField] private GameObject m_objPanel;
```

### 12.2 Runtime và Editor code

- Runtime code không import `UnityEditor`.
- Editor tooling/custom inspector phải nằm trong `Editor/` folder hoặc Editor-only assembly.
- Logic runtime cần để ứng dụng hoạt động không được đặt duy nhất trong Editor script.

### 12.3 Unity event subscription

Khi subscribe trong `OnEnable`, unsubscribe tương ứng trong `OnDisable` nếu lifecycle yêu cầu.

```csharp
private void OnEnable()
{
    m_btnConfirm.onClick.AddListener(OnButtonConfirm);
}

private void OnDisable()
{
    m_btnConfirm.onClick.RemoveListener(OnButtonConfirm);
}
```

### 12.4 Asset và Prefab safety

- Không move/rename asset lớn hoặc prefab/scene ngoài yêu cầu.
- Không phá public API hoặc serialized schema khi chưa đánh giá usage/reference.
- Khi refactor ảnh hưởng asset hiện có, báo migration steps và rủi ro rõ ràng.

---

## 13. Refactor Rules

Khi được yêu cầu refactor, Codex phải:

1. Nêu mục tiêu refactor: vấn đề nào đang được giải quyết.
2. Giữ nguyên behaviour trừ phần được yêu cầu thay đổi.
3. Không trộn thêm feature hoặc thay đổi ngoài scope.
4. Bảo vệ Unity serialization/reference.
5. Đảm bảo file sau refactor không vượt 500 dòng.
6. Chia responsibility rõ ràng, không chỉ move code sang file khác mà vẫn coupling rối.
7. Báo validation thực sự đã chạy; không khẳng định code hoàn toàn hoạt động khi chưa kiểm chứng.

---

## 14. Template Script Chuẩn

> Đây là ví dụ cách trình bày. Thay `MyProject` bằng namespace thật của project đang làm.

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    public sealed class SettingsView : MonoBehaviour
    {
        // Constants
        private const int MAX_LABEL_LENGTH = 32;

        // Serialized fields / Inspector-assigned references
        [SerializeField] private Button m_btnConfirm;
        [SerializeField] private TMP_Text m_txtTitle;
        [SerializeField] private GameObject m_objContent;

        // Other private fields
        private bool m_isVisible;
        private string m_currentTitle;

        // Properties
        public bool IsVisible
        {
            get { return m_isVisible; }
        }

        // Events
        public event Action Confirmed;

        // Unity lifecycle methods
        private void Awake()
        {
            m_isVisible = m_objContent.activeSelf;
        }

        private void OnEnable()
        {
            m_btnConfirm.onClick.AddListener(OnButtonConfirm);
        }

        private void Start()
        {
            RefreshView();
        }

        // Public methods
        public void SetTitle(string title)
        {
            m_currentTitle = title;
            RefreshView();
        }

        public void SetVisible(bool isVisible)
        {
            m_isVisible = isVisible;
            m_objContent.SetActive(m_isVisible);
        }

        // Private helper methods
        private void RefreshView()
        {
            string displayedTitle = m_currentTitle;

            if (displayedTitle.Length > MAX_LABEL_LENGTH)
            {
                displayedTitle = displayedTitle.Substring(0, MAX_LABEL_LENGTH);
            }

            m_txtTitle.text = displayedTitle;
        }

        // UI/Event callbacks
        private void OnButtonConfirm()
        {
            Confirmed?.Invoke();
        }

        // Cleanup lifecycle methods
        private void OnDisable()
        {
            m_btnConfirm.onClick.RemoveListener(OnButtonConfirm);
        }
    }
}
```

---

## 15. Checklist trước khi Codex hoàn thành task

### Scope và file size

- [ ] Chỉ thay đổi những gì cần cho yêu cầu.
- [ ] Không tạo hoặc làm file code tự viết vượt quá 500 dòng.
- [ ] Nếu chạm vào file đã quá 500 dòng, đã báo rõ hoặc tách an toàn trong scope.

### Folder, file và namespace

- [ ] File mới nằm trong folder đúng loại/feature của project.
- [ ] Tên file/asset/class nói rõ trách nhiệm.
- [ ] Code mới dùng namespace thực tế của repository, không dùng placeholder.
- [ ] Không tự ý thay đổi package/asset ngoài task.

### Naming và formatting

- [ ] Private field dùng `m_` + `camelCase`.
- [ ] Serialized field là private trừ khi có lý do rõ.
- [ ] Public property/method dùng `PascalCase`.
- [ ] Bool có tên rõ trạng thái: `is`, `has`, `can`.
- [ ] Collection và Unity reference có tên rõ loại/mục đích.
- [ ] Constants/static readonly dùng `UPPER_SNAKE_CASE`.
- [ ] Method bắt đầu bằng động từ.
- [ ] Coroutine dùng `IE`.
- [ ] Button callback dùng `OnButton`.
- [ ] Event callback dùng dạng `OnNounVerbed`.
- [ ] Luôn dùng braces cho control statements.
- [ ] Member và lifecycle methods được đặt theo đúng thứ tự.

### OOP và Unity safety

- [ ] Class/method có trách nhiệm chính rõ ràng.
- [ ] Không trộn tùy tiện data, runtime logic, UI, persistence và Editor tooling.
- [ ] Dependency rõ ràng, không thêm coupling/global access không cần thiết.
- [ ] Không làm mất serialized reference.
- [ ] Không import `UnityEditor` trong runtime code.
- [ ] Event subscription/unsubscription phù hợp lifecycle.

### Validation

- [ ] Đã nêu validation đã thực hiện.
- [ ] Không tuyên bố hoàn tất/hoạt động nếu chưa thực sự kiểm tra.

---

## 16. Format báo cáo sau khi sửa code

Codex phải kết thúc task bằng báo cáo ngắn theo format:

```markdown
## Files Changed
- `path/to/File.cs`: thay đổi chính.

## Implementation Summary
- Đã thực hiện điều gì.

## Coding & OOP Check
- Namespace đã dùng:
- File gần/vượt giới hạn 500 dòng:
- Responsibility đã tách/giữ như thế nào:
- Naming, ordering và bracing đã tuân thủ:
- Có thêm hard-coded identifier/key không:

## Unity Risk
- Serialized field / Prefab / Scene / ScriptableObject có bị ảnh hưởng không:
- Có cần migration hoặc thao tác Inspector thủ công không:

## Validation
- Đã kiểm tra:
- Chưa kiểm tra được:
```

---

## 17. Nguyên tắc cuối cùng

Codex phải viết code theo hướng:

- **Tên rõ nghĩa.**
- **File ngắn, không quá 500 dòng.**
- **Class và method có trách nhiệm rõ ràng.**
- **Data, runtime processing, UI và Editor tooling không trộn tùy tiện.**
- **Code trình bày thống nhất, có thứ tự, luôn dùng braces.**
- **Refactor không phá serialized reference hoặc thay đổi ngoài scope.**
- **Không giả định bất kỳ feature, framework hoặc cấu trúc riêng nào chưa có trong repository.**
