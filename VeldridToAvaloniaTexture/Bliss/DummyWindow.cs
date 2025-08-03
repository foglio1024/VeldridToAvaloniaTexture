using System;
using System.Numerics;
using Bliss.CSharp.Images;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Bliss.CSharp.Windowing.Events;
using Veldrid;
using Veldrid.OpenGL;

namespace Bliss.Test;

public class DummyWindow : IWindow
{
    private int _width;
    private int _height;

    public void Dispose()
    {
    }

    public string GetTitle()
    {
        return null;
    }

    public void SetTitle(string title)
    {
    }

    public (int, int) GetSize()
    {
        return (_width, _height);
    }

    public void SetSize(int width, int height)
    {
        _width = width;
        _height = height;
        this.Resized?.Invoke();
    }

    public int GetWidth()
    {
        return _width;
    }

    public void SetWidth(int width)
    {
        _width = width;
        this.Resized?.Invoke();
    }

    public int GetHeight()
    {
        return _height;
    }

    public (int, int) GetPosition()
    {
        return (0, 0);
    }

    public void SetPosition(int x, int y)
    {
    }

    public int GetX()
    {
        return 0;
    }

    public void SetX(int x)
    {
    }

    public int GetY()
    {
        return 0;
    }

    public void SetY(int y)
    {
    }

    public void SetHeight(int height)
    {
        _height = height;
        this.Resized?.Invoke();
    }

    public WindowState GetState()
    {
        return WindowState.None;
    }

    public bool HasState(WindowState state)
    {
        return false;
    }

    public void SetResizable(bool resizable)
    {
    }

    public void SetFullscreen(bool fullscreen)
    {
    }

    public void SetBordered(bool bordered)
    {
    }

    public void Maximize()
    {
    }

    public void Minimize()
    {
    }

    public void Hide()
    {
    }

    public void Show()
    {
    }

    public void CaptureMouse(bool enabled)
    {
    }

    public void SetWindowAlwaysOnTop(bool alwaysOnTop)
    {
    }

    public void SetIcon(Image image)
    {
    }

    public void PumpEvents()
    {
    }

    public Point ClientToScreen(Point point)
    {
        return default;
    }

    public Point ScreenToClient(Point point)
    {
        return default;
    }

    public OpenGLPlatformInfo GetOrCreateOpenGlPlatformInfo(GraphicsDeviceOptions options, GraphicsBackend backend)
    {
        return null;
    }

    public IntPtr Handle { get; }
    public uint Id { get; }
    public SwapchainSource SwapchainSource { get; }
    public bool Exists { get; }
    public bool IsFocused { get; }
    public event Action? Resized;
    public event Action? Closed;
    public event Action? FocusGained;
    public event Action? FocusLost;
    public event Action? Shown;
    public event Action? Hidden;
    public event Action? Exposed;
    public event Action<Point>? Moved;
    public event Action? MouseEntered;
    public event Action? MouseLeft;
    public event Action<Vector2>? MouseWheel;
    public event Action<Vector2>? MouseMove;
    public event Action<MouseEvent>? MouseButtonDown;
    public event Action<MouseEvent>? MouseButtonUp;
    public event Action<KeyEvent>? KeyDown;
    public event Action<KeyEvent>? KeyUp;
    public event Action<string>? TextInput;
    public event Action<uint>? GamepadAdded;
    public event Action<uint>? GamepadRemoved;
    public event Action<uint, GamepadAxis, short>? GamepadAxisMoved;
    public event Action<uint, GamepadButton>? GamepadButtonDown;
    public event Action<uint, GamepadButton>? GamepadButtonUp;
    public event Action<string>? DragDrop;
}