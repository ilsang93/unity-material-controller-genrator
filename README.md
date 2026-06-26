# Material Controller

유니티용 범용 머터리얼 컨트롤러 패키지입니다.
머터리얼을 가진 모든 오브젝트 — `SpriteRenderer`, `MeshRenderer`, UI `Image` /
`RawImage`, `TextMeshProUGUI` 등 — 에 대해, 해당 머터리얼의 셰이더에 맞는 전용
컨트롤러 컴포넌트를 **자동 생성**합니다. 셰이더의 모든 프로퍼티가 **이름이 붙은
애니메이션 가능 필드**로 노출되며, 런타임에 머터리얼을 복제하여 원본 에셋을
훼손하지 않습니다.

---

## 이 패키지가 해결하는 문제

하나의 범용 컴포넌트로 임의의 머터리얼을 미러링하려면 보통 `List<>`나 배열에
프로퍼티를 담게 되는데, **Unity Animation 창의 "Add Property"는 `List<>`·배열
원소 안으로 들어가지 못합니다.** 즉, 동적 리스트 방식은 표준 애니메이션 워크플로로
키를 찍을 수 없습니다.

그래서 이 패키지는 **대상 오브젝트의 셰이더에 맞춘 전용 컨트롤러를 코드로 생성**합니다.
각 셰이더 프로퍼티가 구체적인 이름을 가진 필드로 만들어지므로, Animation 창에
이름 그대로 표시되고 레코드 모드에서 정상적으로 기록됩니다.

---

## 설치

### Git URL로 설치 (권장)

Unity 에디터에서 **Window → Package Manager → + → Add package from git URL...** 를
선택하고 아래 URL을 입력합니다.

```
https://github.com/ilsang93/unity-material-controller-generator.git
```

### manifest.json에 직접 추가

`Packages/manifest.json` 의 `dependencies` 에 추가합니다.

```json
"com.ilsang.mcg": "https://github.com/ilsang93/unity-material-controller-generator.git"
```

---

## 컨트롤러 생성 방법

다음 진입점 중 아무거나 사용하면 됩니다.

- **컴포넌트 헤더 우클릭**: `Image` / `SpriteRenderer` 등 컴포넌트의 헤더(`⋮`)를
  우클릭 → **Generate Material Controller**
- **GameObject 메뉴**: Hierarchy에서 오브젝트 우클릭 →
  **Material Controller → Generate Controller**
- **머터리얼 에셋 우클릭**: Project 창에서 머터리얼 우클릭 →
  **Material Controller → Generate Controller Script**
- **머터리얼 인스펙터 헤더 우클릭**: 머터리얼 인스펙터의 헤더(`⋮`) →
  **Generate Controller Script**

오브젝트 기반(앞의 두 가지)은 스크립트 생성 후 **컴파일이 끝나면 자동으로 해당
오브젝트에 컴포넌트로 부착**합니다. 머터리얼 기반(뒤의 두 가지)은 부착할 대상이
없으므로 **스크립트만 생성**하며, 이후 원하는 오브젝트에 붙여 Direct Material
모드로 바인딩하면 됩니다.

생성된 스크립트는 `MatCtrl_<셰이더이름>.cs` 로 `Assets/MaterialControllerGenerated/`
에 저장됩니다. 이후 **Animation 창 → Add Property** 를 열면 각 셰이더 프로퍼티가
이름 그대로 나타나며, 그대로 키프레임을 찍을 수 있습니다.

---

## 타겟 지정 방식 (Target Mode)

컨트롤러는 두 가지 방식으로 제어할 머터리얼을 정합니다.

- **Auto** (기본값)
  같은 오브젝트의 `Renderer` 또는 `Graphic` 을 자동 탐지합니다. 편리하지만,
  머터리얼을 **표준 방식으로 노출하는 컴포넌트**에만 동작합니다.
- **Direct Material** (직접 바인딩)
  인스펙터에서 `Material` 을 직접 지정합니다. 어떤 컴포넌트가 그 머터리얼을
  소유하든 상관없이 제어하므로, **사전에 예상되지 않은 구조의 그래픽 컴포넌트에도
  대응**할 수 있습니다. 예를 들어 일반 `MonoBehaviour` 가 `public Material` 필드로
  머터리얼을 들고 있는 경우(VMG의 일부 컴포넌트 등)에도 동작합니다.

  Instance 가 켜져 있으면, 런타임에 머터리얼을 복제한 뒤 같은 오브젝트의 컴포넌트
  중 그 머터리얼을 참조하는 곳(표준 `Renderer`/`Graphic` 및 커스텀 `Material`
  필드)을 찾아 복제본을 **다시 주입**해, 화면에도 복제본이 반영되도록 합니다.

---

## 동작 방식

- **Instance 토글** (기본값 **체크**)
  런타임 실행 시 머터리얼을 복제하여 원본 에셋을 훼손하지 않습니다. 컴포넌트가
  비활성화될 때 복제본은 자동으로 정리됩니다.
- **Explicit Target** (Auto 모드)
  선택적 대상 지정 필드입니다. 비워 두면 같은 오브젝트의 `Renderer` 또는
  `Graphic` 을 자동으로 탐지합니다.
- **명명 규칙**
  생성되는 클래스는 셰이더 이름을 기준으로 만들어집니다
  (예: `MatCtrl_Sprites_Default`). 같은 셰이더를 쓰는 오브젝트들은 하나의
  컨트롤러를 재사용합니다.
- **이미 존재하면 재사용**
  동일한 컨트롤러 타입이 이미 있으면 새로 만들지 않고 그대로 부착합니다.
  기존에 생성된 파일을 덮어쓰지 않습니다.
- **Texture 프로퍼티**
  인스펙터 제어용으로 함께 생성되지만, 텍스처는 Unity Animation으로 키를 찍을 수
  없습니다(오브젝트 참조이기 때문). 인스펙터에서만 교체 가능합니다.

---

## 커스텀 컴포넌트 / VMG 연동

표준 `Renderer`/`Graphic` 으로 머터리얼을 노출하는 컴포넌트는 **Auto** 모드로
바로 동작합니다(예: `VectorImageGraphic`, `VMGVectorTextGraphic` 같은
`MaskableGraphic` 파생 컴포넌트).

반면 일반 `MonoBehaviour` 가 `public Material Material` 필드로 머터리얼을 들고,
매 프레임 자신의 `Update` 에서 렌더러에 다시 할당하는 컴포넌트
(예: `VectorSpriteRenderer`, `VMGVectorTextWorld`, `VMGVectorTextUGUI`)는
**Direct Material 모드**를 사용하세요.

- 이런 컴포넌트에 Auto 모드를 쓰면, 컨트롤러가 복제본을 렌더러에 주입해도 해당
  컴포넌트가 다음 `Update` 에서 원본 머터리얼을 다시 덮어써서 충돌합니다.
- Direct 모드는 복제본을 **컴포넌트의 `Material` 필드 자체에 주입**하므로,
  그 컴포넌트가 읽어 쓰는 값이 곧 복제본이 되어 충돌 없이 동작합니다. 컨트롤러는
  `LateUpdate`(컴포넌트의 `Update` 이후)에 프로퍼티를 기록합니다.

---

## 지원 프로퍼티 타입

| 셰이더 타입 | 필드 타입 | 애니메이션 |
|---|---|---|
| Color   | `Color`   | ✅ |
| Float   | `float`   | ✅ |
| Range   | `float` (`[Range]`) | ✅ |
| Vector  | `Vector4` | ✅ |
| Int     | `int`     | ✅ |
| Texture | `Texture` | ❌ (인스펙터 전용) |

---

## 요구 사항

- Unity 2022.3 이상 (Unity 6 / 6000.3 기준 개발)
- `com.unity.ugui` (UI `Image` / `Graphic` / TMP 지원에 필요)

---

## 라이선스

[MIT License](LICENSE.md)
