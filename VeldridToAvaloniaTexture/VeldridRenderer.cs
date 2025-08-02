using Avalonia.Controls;
using Bliss.Test;
using SkiaSharp;
using System;
using System.Threading.Tasks;
using Veldrid;

namespace VeldridToAvaloniaTexture;

/// <summary>
/// Provides functionality for rendering graphics using Veldrid and retrieving the rendered output as an SKBitmap.
/// </summary>
/// <remarks>This class manages the creation and disposal of Veldrid resources, including textures, framebuffers,
/// and command lists. It supports resizing the rendering target and provides a method to retrieve the rendered output
/// as a bitmap. The rendering process uses Vulkan as the underlying graphics API.</remarks>
public class VeldridRenderer : IDisposable
{
    private SKBitmap _skBitmap;
    private Texture? _stagingTexture;
    private Texture? _colorTexture;
    private int _width, _height;
    private Game _game;

    public VeldridRenderer(int width, int height, Control host)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);

        _skBitmap = new SKBitmap(_width, _height, SKColorType.Rgba8888, SKAlphaType.Premul);

        _game = new Game(new GameSettings
        {
            Width = 800,
            Height = 600,
            Backend = GraphicsBackend.Vulkan,
            FixedTimeStep = 10,
            TargetFps = 60,
            VSync = true,
            SampleCount = TextureSampleCount.Count1
        }, host);
        _game.Prepare();
        //Task.Run(_game.Run);
    }

    /// <summary>
    /// Resizes the current object to the specified width and height.
    /// </summary>
    /// <remarks>If the specified dimensions are the same as the current dimensions, the method performs no
    /// action. Otherwise, the method updates the dimensions and recreates associated resources.</remarks>
    /// <param name="width">The new width of the object. Must be a positive integer.</param>
    /// <param name="height">The new height of the object. Must be a positive integer.</param>
    public void Resize(int width, int height)
    {
        if (_width == width && _height == height) return;

        _width = width;
        _height = height;

        _game.ResizeTo(_width, _height);
        _skBitmap.Dispose();
        _skBitmap = new SKBitmap(_width, _height, SKColorType.Rgba8888, SKAlphaType.Premul);


    }

    /// <summary>
    /// Renders a single frame by executing a series of graphics commands.
    /// </summary>
    /// <remarks>This method prepares the command list, sets the framebuffer, clears the color target,  and
    /// submits the commands to the graphics device. It ensures that the graphics device  completes all operations
    /// before returning. This method is typically used in rendering workflows to produce a visual frame.</remarks>
    private void Draw()
    {
        _game.Tick();
    }

    /// <summary>
    /// Releases all resources used by the current instance of the object.
    /// </summary>
    /// <remarks>This method disposes of managed and unmanaged resources associated with the object, including
    /// graphics device, command list, framebuffer, textures, and bitmap.  Ensure that this method is called when the
    /// object is no longer needed to free resources and avoid memory leaks.</remarks>
    public void Dispose()
    {
        _colorTexture?.Dispose();
        _skBitmap.Dispose();
    }

    /// <summary>
    /// Renders the current frame and retrieves the resulting bitmap.
    /// </summary>
    /// <remarks>This method performs rendering operations and returns a bitmap representation of the rendered
    /// frame. The returned bitmap is suitable for further processing or display. The method ensures that all rendering
    /// commands are completed and the graphics device is idle before retrieving the bitmap.</remarks>
    /// <returns>An <see cref="SKBitmap"/> containing the rendered frame. The bitmap is fully populated with pixel data from the
    /// rendering process.</returns>
    public SKBitmap GetRenderedBitmap()
    {
        Draw();

        return _game.GetFrame();

        //_commandList.Begin();

        //_commandList.CopyTexture(
        //    source: _colorTexture,
        //    destination: _stagingTexture
        //    );

        //_commandList.End();
        //_graphicsDevice.SubmitCommands(_commandList);
        //_graphicsDevice.WaitForIdle();

        //var mapped = _graphicsDevice.Map(_stagingTexture, MapMode.Read);
        //unsafe
        //{
        //    void* src = mapped.Data.ToPointer();
        //    void* dst = _skBitmap.GetPixels().ToPointer();

        //    Buffer.MemoryCopy(src, dst, _skBitmap.ByteCount, _skBitmap.ByteCount);
        //}
        //_graphicsDevice.Unmap(_stagingTexture);

        return _skBitmap;
    }
}