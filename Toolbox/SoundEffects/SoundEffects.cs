using Toolbox.SoundEffects.Effects;

namespace Toolbox.SoundEffects;

public static class SoundEffects
{
    public static SoundEffect Click = new Click();
    public static SoundEffect Build = new BuildSound();

    public static void Play(SoundEffect soundEffect)
    {
        soundEffect.Play();
    }

    //public static SoundEffect Clank = new Clank();
}