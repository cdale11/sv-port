using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

Console.WriteLine("Framework resources: " + string.Join(",", typeof(Game).Assembly.GetManifestResourceNames()));
var game = new RenderFixture();
game.RunOneFrame();
if (!game.Passed) throw new Exception("Fixture did not draw");
var assembly = typeof(Game).Assembly;
var dispatcher = assembly.GetType("Microsoft.Xna.Framework.FrameworkDispatcher")!;
var worker = (Thread)dispatcher.GetField("_updateWorkerThread", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
var controller = assembly.GetType("Microsoft.Xna.Framework.Audio.OpenALSoundController")!;
var streamer = controller.GetField("_oggstreamer", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
var streamWorker = (Thread)streamer.GetType().GetField("underlyingThread", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(streamer)!;
game.Dispose();
if (worker.IsAlive || streamWorker.IsAlive) throw new Exception("Audio workers outlived game/audio disposal");
Console.WriteLine("PASS: full/partial texture upload ordering, 8192-sprite batch, pool sizing, audio worker shutdown");

sealed class RenderFixture : Game
{
    private readonly GraphicsDeviceManager graphics;
    private Texture2D? texture;
    private RenderTarget2D? target;
    private SpriteBatch? batch;
    internal bool Passed;

    internal RenderFixture()
    {
        graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 64, PreferredBackBufferHeight = 64 };
    }

    protected override void LoadContent()
    {
        texture = new Texture2D(GraphicsDevice, 4, 4);
        texture.SetData(Enumerable.Repeat(Color.Red, 16).ToArray());
        texture.SetData(0, new Rectangle(0, 0, 1, 1), new[] { Color.Green }, 0, 1);
        batch = new SpriteBatch(GraphicsDevice);
        var batcher = typeof(SpriteBatch).GetField("_batcher", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(batch)!;
        var pool = (Array)batcher.GetType().GetField("_batchItemList", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(batcher)!;
        if (pool.Length != 2048) throw new Exception("Configured sprite pool was not applied: " + pool.Length);
        target = new RenderTarget2D(GraphicsDevice, 4, 4);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(Color.Black);
        batch!.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        for (int n = 0; n < 8192; ++n)
            batch.Draw(texture!, new Rectangle(0, 0, 4, 4), new Rectangle(0, 0, 1, 1), Color.White);
        batch.End();
        GraphicsDevice.SetRenderTarget(null);
        var pixels = new Color[16]; target!.GetData(pixels);
        if (pixels.Any(color => color != Color.Green)) throw new Exception("Upload/draw ordering changed pixel output");
        Passed = true;
    }

    protected override void UnloadContent()
    {
        batch?.Dispose(); target?.Dispose(); texture?.Dispose();
    }
}
