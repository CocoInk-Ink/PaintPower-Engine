// VideoPlayer.cs

using PaintPower.Tools.Graphics;

namespace PaintPower.Vm.Runtime.Sprites
{
    public class VideoPlayer
    {
        private readonly string _path;

        // Placeholder frame (optional)
        private Graphic? _placeholderFrame;

        private VideoPlayer(string path)
        {
            _path = path;

            // Optional: load a placeholder image so videos show something
            // You can remove this if you prefer null.
            try
            {
                _placeholderFrame = GraphicLoader.LoadRaster(path) as Graphic;
            }
            catch
            {
                _placeholderFrame = null;
            }
        }

        public static VideoPlayer Load(string path)
        {
            return new VideoPlayer(path);
        }

        // Called every frame by Sprite.RenderSnapshot()
        public Graphic? GetCurrentFrame()
        {
            // TODO: real video decoding later
            return _placeholderFrame;
        }
    }
}
