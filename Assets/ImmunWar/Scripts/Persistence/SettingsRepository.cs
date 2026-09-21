namespace ImmunWar.Persistence
{
    public sealed class SettingsRepository
    {
        private readonly SaveRepository _saves;
        public float PreviewMusic { get; private set; }
        public float PreviewSfx { get; private set; }
        public SettingsRepository(SaveRepository saves) { _saves = saves; Cancel(); }
        public void Preview(float music, float sfx) { PreviewMusic = Clamp(music); PreviewSfx = Clamp(sfx); }
        public SaveData Apply()
        {
            var data = _saves.Load();
            data.musicVolume = PreviewMusic;
            data.sfxVolume = PreviewSfx;
            return _saves.Save(data);
        }
        public void Cancel()
        {
            var data = _saves.Load();
            PreviewMusic = data.musicVolume;
            PreviewSfx = data.sfxVolume;
        }
        private static float Clamp(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
