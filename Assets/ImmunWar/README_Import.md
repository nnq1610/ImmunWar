# ImmunWar asset import status

## Imported assets

- `Art/Enemies/StaticVirus`: 114 virus PNG sprites from `VirusBigPack.zip`.
- `Art/Enemies/AnimatedVirus`: Spine 3.7.94 atlas, JSON and texture.
- `Art/Enemies/AnimatedVirus/SpineSource`: original Spine source file and separated skin images.

## Unity import settings

- Static virus images are configured as `Sprite (2D and UI)`, Single, 100 PPU, mipmaps disabled and uncompressed on the default platform.
- `coronavirus.png` is configured as a Multiple sprite and has already been sliced into individual regions.

## Important animation note

The supplied `coronavirus_spine_v3794.zip` is a Spine skeletal-animation source pack, not a normal Unity frame-by-frame sprite animation. The files are imported safely, but playing the authored attack/death/hit/idle animations requires a compatible Spine-Unity runtime. No third-party runtime was added automatically.

For the first prototype, use one of the sliced regions from `coronavirus.png` as a static enemy sprite. Add the Spine runtime later only if the team decides to keep this asset for final animation.

## Empty project folders

The remaining folders are prepared for defenders, backgrounds, animations, audio, prefabs, scenes, scripts, UI and VFX. Unity will generate metadata for any newly created empty folders when the project is opened.
