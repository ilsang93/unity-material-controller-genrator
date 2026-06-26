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
https://github.com/ilsang93/unity-material-controller-genrator.git
```

### manifest.json에 직접 추가

`Packages/manifest.json` 의 `dependencies` 에 추가합니다.

```json
"com.witchslounge.material-controller": "https://github.com/ilsang93/unity-material-controller-genrator.git"
```

---

## 사용 방법

1. 머터리얼을 가진 `Renderer` 또는 `Graphic` 컴포넌트가 있는 GameObject를
   준비합니다.
2. 컨트롤러를 생성합니다. 다음 두 가지 진입점 중 아무거나 사용하면 됩니다.
   - **컴포넌트 헤더 우클릭**: `Image` / `SpriteRenderer` 등 컴포넌트의 헤더(`⋮`)를
     우클릭 → **Generate Material Controller**
   - **GameObject 메뉴**: Hierarchy에서 오브젝트 우클릭 →
     **Material Controller → Generate Controller**
3. 도구가 머터리얼의 셰이더를 읽어 `MatCtrl_<셰이더이름>.cs` 스크립트를
   `Assets/MaterialControllerGenerated/` 에 생성하고, **컴파일이 끝나면 자동으로
   해당 오브젝트에 컴포넌트로 부착**합니다.
4. **Animation 창 → Add Property** 를 열면 각 셰이더 프로퍼티가 이름 그대로
   나타나며, 그대로 키프레임을 찍을 수 있습니다.

---

## 동작 방식

- **Instance 토글** (컴포넌트 최상단, 기본값 **체크**)
  런타임 실행 시 머터리얼을 복제하여 원본 에셋을 훼손하지 않습니다. 컴포넌트가
  비활성화될 때 복제본은 자동으로 정리됩니다.
- **Explicit Target**
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
