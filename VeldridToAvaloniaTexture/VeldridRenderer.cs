using SkiaSharp;
using System;
using Veldrid;
using MapMode = Veldrid.MapMode;

namespace VeldridToAvaloniaTexture;

/// <summary>
/// Provides functionality for rendering graphics using Veldrid and retrieving the rendered output as an SKBitmap.
/// </summary>
/// <remarks>This class manages the creation and disposal of Veldrid resources, including textures, framebuffers,
/// and command lists. It supports resizing the rendering target and provides a method to retrieve the rendered output
/// as a bitmap. The rendering process uses Vulkan as the underlying graphics API.</remarks>
public class VeldridRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly CommandList _commandList;
    private readonly SKBitmap _skBitmap;
    private Texture? _stagingTexture;
    private Texture? _colorTexture;
    private Framebuffer? _framebuffer;
    private int _width, _height;

    public VeldridRenderer(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);

        var options = new GraphicsDeviceOptions()
        {
            PreferStandardClipSpaceYDirection = true,
            SyncToVerticalBlank = false,
            SwapchainDepthFormat = null,
            ResourceBindingModel = ResourceBindingModel.Improved,
        };

        _graphicsDevice = GraphicsDevice.CreateVulkan(options);

        _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();

        _skBitmap = new SKBitmap(_width, _height, SKColorType.Rgba8888, SKAlphaType.Premul);

        CreateResources();
    }

    /// <summary>
    /// Creates and initializes the necessary graphics resources, including textures and a framebuffer,  for rendering
    /// operations.
    /// </summary>
    /// <remarks>This method sets up a color texture, a staging texture, and a framebuffer using the graphics
    /// device's  resource factory. The created resources are configured for rendering and sampling operations.  Ensure
    /// that the graphics device is properly initialized before calling this method.</remarks>
    private void CreateResources()
    {
        var textureDescription = TextureDescription.Texture2D(
            (uint)_width,
            (uint)_height,
            1,
            1,
            PixelFormat.R8G8B8A8UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled);

        _colorTexture = _graphicsDevice.ResourceFactory.CreateTexture(textureDescription);

        _stagingTexture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            _colorTexture.Width,
            _colorTexture.Height,
            mipLevels: 1,
            arrayLayers: 1,
            _colorTexture.Format,
            TextureUsage.Staging));

        _framebuffer = _graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(null, _colorTexture));
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

        _framebuffer?.Dispose();
        _colorTexture?.Dispose();

        CreateResources();
    }

    /// <summary>
    /// Renders a single frame by executing a series of graphics commands.
    /// </summary>
    /// <remarks>This method prepares the command list, sets the framebuffer, clears the color target,  and
    /// submits the commands to the graphics device. It ensures that the graphics device  completes all operations
    /// before returning. This method is typically used in rendering workflows to produce a visual frame.</remarks>
    private void RenderFrame()
    {
        _commandList.Begin();

        _commandList.SetFramebuffer(_framebuffer);

        _commandList.ClearColorTarget(0, RgbaFloat.RED);

        _commandList.End();

        _graphicsDevice.SubmitCommands(_commandList);
        _graphicsDevice.WaitForIdle();
    }

    /// <summary>
    /// Releases all resources used by the current instance of the object.
    /// </summary>
    /// <remarks>This method disposes of managed and unmanaged resources associated with the object, including
    /// graphics device, command list, framebuffer, textures, and bitmap.  Ensure that this method is called when the
    /// object is no longer needed to free resources and avoid memory leaks.</remarks>
    public void Dispose()
    {
        _graphicsDevice.WaitForIdle();
        _commandList.Dispose();
        _framebuffer?.Dispose();
        _colorTexture?.Dispose();
        _graphicsDevice.Dispose();
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
        RenderFrame();

        _commandList.Begin();

        _commandList.CopyTexture(
            source: _colorTexture,
            destination: _stagingTexture
            );

        _commandList.End();
        _graphicsDevice.SubmitCommands(_commandList);
        _graphicsDevice.WaitForIdle();

        var mapped = _graphicsDevice.Map(_stagingTexture, MapMode.Read);
        unsafe
        {
            void* src = mapped.Data.ToPointer();
            void* dst = _skBitmap.GetPixels().ToPointer();

            Buffer.MemoryCopy(src, dst, _skBitmap.ByteCount, _skBitmap.ByteCount);
        }
        _graphicsDevice.Unmap(_stagingTexture);

        return _skBitmap;
    }
}