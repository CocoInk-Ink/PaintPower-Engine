namespace PaintPower.Vm.Runtime.Sprites
{
    public class RuntimeVideoElement : RuntimeSkinElement
    {
        public VideoPlayer Player { get; set; }
        public bool Loop { get; set; }
        public bool AutoPlay { get; set; }
    }
}
