# Furnace Rhythm — Implementation Plan

**Working title:** Furnace Rhythm  
**Target:** a finished, small 2D prototype in three focused days  
**Design rule:** one screen, one main mechanic, one-button timing

## One-sentence pitch

Forge a short order of parts by pressing on the beat as each workpiece moves through Heat, Hammer, and Quench.

## Core play loop

1. Start a five-order shift.
2. For each workpiece, watch a timing marker sweep across a fixed bar and press **Space** or **left mouse button** when it reaches the target band.
3. Complete the three beats—Heat, Hammer, Quench—and receive a workpiece quality result based on the three timing judgments.
4. Build a score streak, finish the fifth workpiece, see the shift score, and restart.

Each beat uses the same control and timing bar. Phase labels, color, and a short animation distinguish the actions. A missed window resolves automatically as a miss, so the run never stalls waiting for input.

## Three-day scope

### Day 1 — Playable timing loop

- Create one 2D gameplay scene and a simple start state.
- Implement the moving timing marker, target band, Space/click input, and timing judgment.
- Cycle through Heat, Hammer, and Quench for one workpiece.
- Add immediate hit, miss, and phase-change feedback.
- Brighten the target as the marker approaches; animate the billet's idle float and give Heat, Hammer, and Quench distinct hit effects.

**Day 1 checkpoint:** one workpiece can be forged from start to result using the real input and timing loop.

### Day 2 — Complete run and results

- Add the five-workpiece shift and advance between orders automatically.
- Add Perfect/Good/Miss scoring (100/60/0), a streak that resets on a miss, and a +10 combo bonus per consecutive hit capped at +50.
- Add title/start, in-run, and results states, with a restart action.
- Show current phase, workpiece progress, score, and streak without adding extra screens.

**Day 2 checkpoint:** a player can complete and restart a full run without editor interaction.

### Day 3 — Art, sound, and finish

- Import and place the hot billet/anvil concept asset; tune its size and sprite settings for the gameplay view.
- Tune the phase-specific effects and add a few clear sound cues if time allows.
- Refine spacing, contrast, timing-window readability, and the result screen.
- Make a playable build and check start, all 15 beats, miss handling, results, and restart.

**Day 3 checkpoint:** a polished single-scene build communicates the timing mechanic and completes one full shift reliably.

## Minimum feature list

- One fixed 2D screen and one short five-order run.
- Three timing beats per order: Heat, Hammer, Quench.
- Space or left click as the only gameplay input.
- Clear Perfect/Good/Miss feedback, score, and streak.
- Start, results, and restart states.
- The existing industrial palette and the sample hot billet/anvil art direction.

## Explicitly out of scope

- Player movement, platforming, combat, enemies, physics puzzles, or multiple rooms.
- Conveyor routing, factory construction, resource management, recipes, or inventory.
- Multiple levels, unlocks, upgrades, quests, procedural orders, or save/profile systems.
- Online play, leaderboards, settings menus, or a large content pipeline.

If a feature does not make the timing loop clearer or more satisfying, defer it until after the three-day build.

## Visual direction

Keep the industrial palette listed in the README: Midnight Industrial `#1D3039`, Slate Machinery `#526B70`, Oxidized Teal `#578F8C`, Industrial Cream `#C6C8B5`, Electric Cyan `#62E3DD`, Safety Orange `#E6A04F`, Factory Coral `#C96C70`, and Machine Violet `#796098`. Preserve its color balance: roughly 60% structural neutrals, 25% machine colors, 10% functional accents, and 5% emissive highlights. Use cool neutrals for most of the screen; reserve cyan for the timing target and active feedback, and orange/coral for heat and misses. Translate the existing industrial material language into clean 2D illustrations with chunky silhouettes, restrained bevel shading, and localized glow.

The initial concept asset is `Art/FurnaceRhythm_HotBillet_Concept.png` (transparent PNG, 1536×1024). Treat it as the visual target for the workpiece and anvil; adjust the in-game crop/scale during integration rather than expanding the asset list early.

## Completion checks

- A new player can understand when to press from the screen alone.
- A run contains exactly five workpieces and fifteen timing beats.
- Perfect, Good, and Miss outcomes are visibly distinct and update score/streak correctly.
- The game reaches results after the fifth workpiece and can restart immediately.
- The palette remains consistent and gameplay text/targets stay readable at the intended window size.
- The build can be launched and played without using Unity editor controls.

## Current project state

The previous factory prototype scripts and art have been removed from this workspace. Day 1 and Day 2 runtime logic are implemented in `FurnaceRhythmGame.cs`: five workpieces with three sweeping timing beats each, Space/left-click input, automatic late misses, a target that brightens as the marker nears, phase-specific Heat/Hammer/Quench animation, and Perfect/Good/Miss feedback. The HUD has separate score, streak/next-bonus, and workpiece panels, a five-part shift progress strip, and labeled beat indicators with small flame, hammer, and water glyphs. Score counts up, combo pips pulse, and a phase-colored trail follows the timing marker. Hits trigger a short grade-colored flash and expanding impact ring; result cards scale and fade during transitions. Workpiece result cards show each phase grade and points earned; the final shift card shows rank, score, grade totals, best streak, and restart prompt. Their rounded frames and grade chips use a small procedural, nine-sliced uGUI sprite in the existing palette, so no extra bitmap UI pack is required. Perfect scores 100, Good scores 60, misses score 0 and reset the streak; consecutive hits add 10 points each up to a 50-point bonus. `FurnaceRhythmDayOneSceneBuilder.cs` adds **Furnace Rhythm → Build Day 1 Scene** in Unity; it imports the sample PNG as a sprite, creates the 2D camera scene, and puts that scene first in Build Settings. The scene file is generated from that menu; it is not present until the builder runs.
