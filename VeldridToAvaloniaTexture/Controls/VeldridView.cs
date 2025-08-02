using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using System;
using Avalonia.Platform;

namespace VeldridToAvaloniaTexture.Controls;

/// <summary>
/// Represents a custom control that integrates with the Veldrid rendering framework.
/// </summary>
/// <remarks>The <see cref="VeldridView"/> control is designed to render graphics using the Veldrid rendering
/// framework. It automatically manages rendering resources and updates the display at regular intervals. The control
/// initializes its renderer when attached to the visual tree and releases resources when detached.  This control
/// supports dynamic resizing and ensures that the renderer adapts to changes in size. It is suitable for scenarios
/// requiring high-performance graphics rendering within a UI framework.</remarks>
public class VeldridView : Control
{
    private readonly DispatcherTimer _timer;

    private VeldridRenderer? _renderer;
    private bool _isInitialized;
    private WriteableBitmap? _writeableBitmap;

    public static readonly StyledProperty<float> FrameRateProperty =
        AvaloniaProperty.Register<VeldridView, float>(nameof(FrameRate), 60f);

    public float FrameRate
    {
        get => GetValue(FrameRateProperty);
        set => SetValue(FrameRateProperty, value);
    }

    static VeldridView()
    {
        FrameRateProperty.Changed.AddClassHandler<VeldridView>(HandleFrameRateChanged);
    }

    private static void HandleFrameRateChanged(VeldridView sender, AvaloniaPropertyChangedEventArgs e)
    {
        sender.OnFrameRateChanged();
    }

    private void OnFrameRateChanged()
    {
        _timer.Interval = TimeSpan.FromSeconds(1 / FrameRate);
    }

    public VeldridView()
    {
        _timer = new DispatcherTimer(
            TimeSpan.FromSeconds(1 / FrameRate),
            DispatcherPriority.Render,
            (_, _) => InvalidateVisual()
        );
    }

    /// <summary>
    /// Handles the event when the control is attached to the visual tree.
    /// </summary>
    /// <remarks>This method starts an internal timer and initializes the renderer if it has not been
    /// initialized. It is called automatically during the control's lifecycle when it becomes part of the visual
    /// tree.</remarks>
    /// <param name="e">The event data associated with the visual tree attachment.</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _timer.Start();

        if (_isInitialized) return;

        _renderer = new VeldridRenderer((int)Bounds.Width, (int)Bounds.Height, this);
        _isInitialized = true;
    }

    /// <summary>
    /// Handles the event when the control is detached from the visual tree.
    /// </summary>
    /// <remarks>This method stops any ongoing timers and disposes of resources associated with rendering. It
    /// ensures that the control releases resources properly when it is removed from the visual tree.</remarks>
    /// <param name="e">The event data associated with the detachment from the visual tree.</param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _timer.Stop();

        _renderer?.Dispose();
        _writeableBitmap?.Dispose();
        _renderer = null;
    }

    /// <summary>
    /// Handles changes to the size of the control and updates the renderer accordingly.
    /// </summary>
    /// <remarks>This method is called automatically when the size of the control changes. If the control is
    /// not  initialized, the method exits without performing any actions. When initialized, the renderer is  resized to
    /// match the new dimensions of the control.</remarks>
    /// <param name="e">The event data containing the old and new size of the control.</param>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if (!_isInitialized) return;

        _renderer?.Resize((int)e.NewSize.Width, (int)e.NewSize.Height);
    }

    /// <summary>
    /// Renders the current visual content onto the specified <see cref="DrawingContext"/>.
    /// </summary>
    /// <remarks>This method uses a renderer to generate a bitmap representation of the visual content,  which
    /// is then copied into a writable bitmap and drawn onto the provided <see cref="DrawingContext"/>. Ensure that the
    /// renderer is properly initialized before calling this method.</remarks>
    /// <param name="context">The <see cref="DrawingContext"/> to render the visual content onto.</param>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_renderer == null) return;

        SKBitmap skBitmap = _renderer.GetRenderedBitmap();

        CreateWritableBitmapIfNeeded(skBitmap);

        using (ILockedFramebuffer fb = _writeableBitmap!.Lock())
        {
            unsafe
            {
                void* src = skBitmap.GetPixels().ToPointer();
                void* dst = fb.Address.ToPointer();
                Buffer.MemoryCopy(src, dst, fb.RowBytes * fb.Size.Height, skBitmap.ByteCount);
            }
        }

        var destRect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var sourceRect = new Rect(0, 0, skBitmap.Width, skBitmap.Height);
        context.DrawImage(_writeableBitmap, sourceRect, destRect);
    }

    /// <summary>
    /// Ensures that a writable bitmap is created or updated to match the dimensions of the specified <see
    /// cref="SKBitmap"/>.
    /// </summary>
    /// <remarks>If the existing writable bitmap matches the dimensions of the provided <see
    /// cref="SKBitmap"/>, no action is taken. Otherwise, the existing writable bitmap is disposed, and a new writable
    /// bitmap is created with the same dimensions and a default pixel format of RGBA8888 and premultiplied
    /// alpha.</remarks>
    /// <param name="skBitmap">The <see cref="SKBitmap"/> whose dimensions are used to create or validate the writable bitmap.</param>
    private void CreateWritableBitmapIfNeeded(SKBitmap skBitmap)
    {
        if (_writeableBitmap != null &&
            _writeableBitmap.PixelSize.Width == skBitmap.Width &&
            _writeableBitmap.PixelSize.Height == skBitmap.Height) return;

        _writeableBitmap?.Dispose();
        _writeableBitmap = new WriteableBitmap(
            new PixelSize(skBitmap.Width, skBitmap.Height),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Premul);
    }
}
