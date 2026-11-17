# THE LAST OATH — Codex Project Instructions

## 1. Core working rule

Before modifying code, inspect the current repository state and the actual files involved.

Do not rely on assumptions from previous conversations, old commits, or remembered project structure when the current source can be inspected.

Always prefer the smallest change that satisfies the requested behavior.

Do not perform unrelated cleanup or refactoring unless explicitly requested.

---

## 2. Git safety

Before starting a task:

- Check the current branch.
- Run `git status`.
- Identify existing modified or untracked files.
- Never overwrite or discard user changes.

Never run destructive Git operations unless the user explicitly asks for them.

Forbidden by default:

- `git reset --hard`
- `git checkout .`
- `git restore .`
- `git clean -fd`
- force push
- history rewriting
- deleting uncommitted user work

Do not commit or push unless explicitly requested.

After modifications, review the diff and report what changed.

---

## 3. Unity asset safety

This is a Unity project.

Preserve all existing `.meta` files and GUIDs.

Do not delete or regenerate `.meta` files.

Do not rename or move Unity assets unless the task explicitly requires it.

Avoid manually editing Unity serialized YAML files such as:

- `.unity`
- `.prefab`
- `.controller`
- animation clips

unless the task specifically requires that change and there is no safer alternative.

Prefer C# changes plus explicit Inspector setup instructions when serialized references are involved.

Do not modify generated Unity folders such as:

- `Library`
- `Temp`
- `Logs`
- `obj`

---

## 4. Serialized reference safety

Many fields are connected through the Unity Inspector.

Do not casually rename or remove `[SerializeField]` fields.

Before renaming serialized fields, consider whether existing Inspector references or serialized values will be lost.

Do not replace existing components or references merely to simplify code.

Preserve existing scene and prefab connections whenever possible.

---

## 5. Animation Event safety

Several combat and presentation systems depend on Unity Animation Events.

Do not rename, delete, or change the signature of Animation Event receiver methods without first finding all usages and verifying the related clips.

Examples of sensitive event-driven behavior include:

- Charge attack hit timing
- Boss death completion
- First encounter fear timing
- Attack cue SFX
- Hit events
- Ready / attack animation transitions

An Animation Event method that appears unused in C# may still be required by an Animation Clip.

---

## 6. Game progression invariants

The intended story progression is:

`FirstEncounter`
→ `Act1`
→ `Act1Cleared`
→ `Act2Adaptive`
→ `Completed`

Do not skip or merge these phases unless explicitly requested.

`Act1Cleared` is intentionally separate from `Act2Adaptive`.

It exists so the game can perform:

Act1 victory  
→ boss identity reveal  
→ fake clear  
→ Afterlife  
→ failed return attempt  
→ Act2Adaptive

Do not automatically transition directly from Act1 victory to Act2Adaptive.

---

## 7. Growth system invariants

Defeat progression currently uses a pending reward flow.

Conceptually:

Player dies  
→ pending Growth Point is created  
→ player reaches Afterlife  
→ NPC dialogue claims the pending point  
→ actual Growth Point becomes spendable

Do not grant the same Growth Point twice.

Be careful when adding special Afterlife dialogue routes: if they bypass the normal dialogue flow, verify that pending Growth Points are still claimed exactly once.

Do not change stat caps, formulas, damage values, or progression balance unless the user explicitly requests a balance change.

---

## 8. Presentation vs combat separation

Narrative presentation should be implemented in presentation-oriented systems whenever possible.

Prefer modifying or adding code around systems such as:

- `BossBattleIntroController`
- `Act1ClearPresentationController`
- `PlayerDeathPresentationController`
- `AdaptivePresentationController`
- `AfterlifeNPC`
- `CharacterSpeechBubble`
- `CameraBossFollow`
- `BossIdentityPresentationController`

Do not modify core combat or adaptive decision logic merely to implement a cutscene or dialogue.

Avoid changing these without explicit need:

- `BossController`
- `BossAction`
- `BossAdaptiveDecisionSource`
- `PlayerBehaviorModel`
- adaptive scoring logic
- `CombatLogger`

The adaptive combat system is portfolio-critical and should remain stable unless the task specifically concerns adaptive combat.

---

## 9. Current boss identity presentation

Current narrative visual rule:

### FirstEncounter
Boss uses black silhouette presentation.

### Act1
Boss uses black silhouette presentation.

### Act1Cleared
Boss remains silhouette by default, but the clear presentation may temporarily reveal the normal human appearance.

### Act2Adaptive
Boss uses the normal colored appearance.

### Completed
Normal appearance unless the ending presentation explicitly overrides it.

Do not change this rule without explicit instruction.

---

## 10. Act1 clear sequence

The intended Act1 clear sequence is:

Boss reaches 0 HP  
→ death animation completes  
→ black silhouette is removed  
→ human appearance is revealed  
→ camera focuses and zooms toward the boss  
→ short silent hold  
→ player says `"...인간인 건가?"`  
→ camera returns  
→ fake clear UI appears  
→ confirmation transitions to Afterlife

Do not show the Fake Clear before the identity-reveal presentation has completed.

---

## 11. Act2 first encounter

After the failed return attempt in Afterlife:

`BeginAct2Adaptive()` transitions the game into `Act2Adaptive`.

On the first Act2 battle only:

- Boss appears in normal color.
- Player control is temporarily locked.
- Boss says `"또 왔군."`
- Ready animation plays.
- Player control is restored.
- Battle begins.

This introductory line must not repeat on every Act2 retry.

Use persistent progression/presentation state for one-time events rather than scene-local booleans when the state must survive scene loads.

---

## 12. Camera and input safety

Cutscenes frequently change input and camera state.

Whenever input is locked, verify that it is restored on every successful exit path.

Relevant systems may include:

- `PlayerMovement.SetInputLocked`
- `PlayerInput.DeactivateInput`
- `PlayerInput.ActivateInput`

Whenever camera presentation changes:

- focus weight
- camera position
- orthographic size
- zoom

restore the previous state when the presentation completes.

Avoid leaving presentation state active when a coroutine exits early.

---

## 13. Time scale safety

Combat uses timing-sensitive systems including HitStop and slow motion.

If code changes `Time.timeScale`, save the previous value when appropriate and restore it afterward.

Do not leave the game paused or slowed after:

- Adaptive presentation
- death presentation
- cutscenes
- interrupted coroutines

Prefer unscaled time for presentation transitions that must continue during slow motion or HitStop.

---

## 14. Combat design constraints

Do not silently change combat design.

The following require explicit user approval:

- boss HP
- player damage
- attack damage
- attack startup / active / recovery timing
- parry windows
- dodge windows
- movement speeds
- adaptive thresholds
- habit scoring
- response pools
- cooldowns
- stun duration

Presentation changes must not unintentionally alter combat timing.

---

## 15. Adaptive AI design constraints

The adaptive boss should respond to accumulated player behavior, not directly read the player's current input and instantly counter it.

Adaptive behavior must remain fallible.

The player must be able to change habits and cause the boss's prediction to become wrong.

Do not convert the system into deterministic one-habit → one-counter behavior.

Do not add machine learning, neural networks, LSTM, or other ML systems unless explicitly requested.

---

## 16. Verification before finishing

After every coding task:

1. Review the modified files.
2. Review `git diff`.
3. Check for accidental unrelated changes.
4. Search for compile-risk references after method or field changes.
5. Check Animation Event compatibility when event methods were touched.
6. Check Inspector/serialized reference implications.
7. Check input, camera, and time-scale restoration if presentation code changed.
8. Report any manual Unity Inspector work still required.

Do not claim Unity compilation or runtime testing succeeded unless it was actually executed.

If runtime testing was not possible, state that only static verification was performed.

---

## 17. Reporting format

At the end of a task, report:

- Changed files
- What behavior was implemented
- Any existing behavior intentionally left untouched
- Validation performed
- Any manual Inspector steps required
- Any remaining risks or assumptions

Keep the report concise.

---

## 18. When uncertain

If a requested implementation conflicts with an existing system, preserve the existing system and explain the conflict.

If there are multiple implementation options, prefer:

minimal change  
→ existing architecture reuse  
→ presentation-layer solution  
→ new isolated component

over broad architectural rewrites.
