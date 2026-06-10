# Project Context

## 기획서 기준

- 장르: 탑뷰 스타일리쉬 액션 로그라이트.
- 핵심 컨셉: 하데스식 노드 진행 구조와 데빌 메이 크라이식 논스톱 액션을 결합한 고기동성 탑뷰 액션.
- 기본 루프: 로비에서 영구 강화/무기 선택 -> 던전 진입 -> 노드 선택/전투 -> 모듈 획득/장착 -> 사망 또는 클리어 -> 영구 재화 획득 후 로비 복귀.
- 주요 시스템: 전투 시뮬레이션 던전, 무기 스왑 전투, 회중시계 스킬/출력 게이지, 모듈 시스템, 영구 성장, JSON 저장, PlayerPrefs 설정 저장.
- 던전 노드: 일반 전투, 엘리트, 보스, 상점/정비, 이벤트. 마지막 노드는 보스 고정.
- 죽어도 남는 데이터: 영구 재화, 해금 무기, 영구 강화 수치, 스토리/진행도, 최고 도달 스테이지.
- 회차 초기화 대상: 현재 진행도, 장착 모듈, 회중시계 출력 게이지, 과부하 상태.
- ScriptableObject 대상: 무기, 모듈, 적, 스테이지/노드, 업그레이드성 데이터.

## 현재 코드 구조

- 플레이어 전투 축: `PlayerCombat`, `PlayerWatchSkill`, `PlayerDodge`, `PlayerParry`, `ClockOutputSystem`, `PlayerHealth`, `PlayerController`.
- 던전 진행 축: `DungeonRunManager`, `DungeonRoomController`, `DungeonRoomModule`, `DungeonEntranceDoor`, `DungeonChoiceDoor`, `DungeonExitDoor`.
- 로그라이트 성장/경제 축: `ModuleRewardManager`, `PlayerModuleInventory`, `ModuleEquipStation`, `ShopStation`, `PlayerWallet`, `PlayerPermanentProgress`, `SaveDataManager`, `GameSettingsManager`.
- 적 패턴 축: `ChaserEnemy`, `ShooterEnemy`, `ChargerEnemy`, `DashConeEnemy`, `LineStrikeEnemy`, `FixedBarrageEnemy`, `BombThrowerEnemy`, `EnemyProjectile`, `ExplodingEnemyProjectile`.
- 데이터 축: `WeaponData`, `ModuleData`, `EnemyData`, `StageData`, `NodeData`.
- 공통 전투 인터페이스: `IDamageable`, `IRoomEnemy`, `IEnemyStatusReceiver`, `IParryableEnemyAttack`, `IDodgeableEnemyAttack`.

## 구현 방향 메모

- 지금 코드는 에디터 배치가 덜 되어도 플레이가 되도록 런타임 fallback 생성 오브젝트가 많다. 과제 프로토타입 단계에서는 유지하되, 최종 제출 전에는 프리팹/ScriptableObject 연결을 우선 정리한다.
- 새 전투 기능은 기존 인터페이스를 우선 사용한다. 피해는 `IDamageable`, 방 클리어 판정은 `IRoomEnemy`, 시간정지/그로기는 `IEnemyStatusReceiver`, 패링/회피 판정은 `IParryableEnemyAttack`/`IDodgeableEnemyAttack`을 따른다.
- 회중시계 스킬은 무기별 `WatchSkillType`에 묶여 있다. 새 무기나 스킬을 추가할 때는 `WeaponData`와 `PlayerWatchSkill`의 분기를 함께 확인한다.
- 저장 정책은 기획서와 코드가 대체로 맞다. 영구 데이터는 `SaveDataManager`의 JSON, 설정 값은 `GameSettingsManager`의 PlayerPrefs에 둔다.
- 모듈의 "과부하"는 기획서에는 있으나 현재 코드에서는 아직 핵심 구현이 거의 없다. 이후 로그라이트 차별화 요소로 우선순위가 높다.

## 주의할 점

- 일부 PowerShell 출력에서 한글이 깨져 보일 수 있다. `EnemyData.cs` 자체의 Header 문자열은 UTF-8 기준 정상으로 확인했다.
- 슬로모션 효과는 `GameTimeScaleController`를 통해 요청 단위로 관리한다. 새 슬로모션 연출을 추가할 때는 `Time.timeScale`을 직접 바꾸지 말고 `RequestSlowMotion`/`CancelSlowMotion`을 사용한다.
- 탄막 적 피격 정책: `FixedBarrageEnemy`는 피격 시 그로기, `ShooterEnemy`와 `BombThrowerEnemy`는 피격 그로기 후 플레이어와 거리를 벌리고 다시 발사/투척한다.
- 적 뭉침 완화는 `EnemySeparationUtility`를 사용한다. 일반 추적/거리유지/후퇴 이동에는 분리 힘을 더하되, 돌진이나 공격 대시처럼 판정이 중요한 이동에는 적용하지 않는다.
- 외골격 패링 반격은 명중 순간 `PlayerWatchSkill`에서 짧은 색상 반전 플래시와 강한 카메라 흔들림으로 타격감을 보강한다. 반격 플래시는 패링 톤 피드백과 색상 복구가 겹치지 않도록 전용 배열을 사용하고, 반격 발동 시 기존 `CombatToneFeedback`은 `StopAndRestore`로 정리한다.
- `PlayerController.FixedUpdate`는 주석상 공격 중 이동 잠금 의도와 달리, 현재 `velocity`로 계속 이동한다. 공격 중 완전 고정을 원하면 `IsAttacking` 체크가 필요하다.
- `Time.timeScale`을 여러 시스템이 직접 조작한다. 회피, 패링, 시간정지 준비, 방 클리어 연출이 겹칠 수 있으므로 나중에는 중앙 슬로모션 매니저로 묶는 편이 안전하다.
- `FindObjectsByType`와 런타임 `new GameObject` 사용이 많다. 현재 규모에서는 괜찮지만 적/투사체가 늘면 캐싱 또는 매니저화가 필요하다.
- 디버그 UI가 `OnGUI` 기반으로 여러 곳에 흩어져 있다. 제출 빌드 전에는 표시 옵션을 끄거나 하나의 테스트 HUD로 정리한다.
- 일반 `dotnet build`는 현재 환경에서 NuGet 설정 폴더 접근 권한 때문에 실패했다. Unity Editor에서의 컴파일/Play Mode 검증이 별도로 필요하다.
