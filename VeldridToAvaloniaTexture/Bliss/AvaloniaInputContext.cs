using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using Bliss.CSharp.Interact.Contexts;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice.Cursors;
using MouseButton = Bliss.CSharp.Interact.Mice.MouseButton;

namespace Bliss.Test;

public class AvaloniaInputContext : IInputContext
{
    private readonly Control _control;
    private Vector2 _mouseDelta;
    private Vector2 _lastMousePos;
    private Vector2 _mouseScrolling;

    private readonly HashSet<MouseButton> _mouseDown = new();
    private readonly HashSet<KeyboardKey> _keyDown = new();

    public AvaloniaInputContext(Control control)
    {
        _control = control;
        var topLevel = TopLevel.GetTopLevel(_control);

        _control.PointerMoved += OnPointerMoved;
        _control.PointerPressed += OnPointerPressed;
        _control.PointerReleased += OnPointerReleased;
        topLevel.KeyDown += OnKeyDown;
        topLevel.KeyUp += OnKeyUp;
        _control.PointerWheelChanged += OnWheel;
    }

    public void Begin() { }
    public void End()
    {
        _mouseDelta = Vector2.Zero;
        _mouseScrolling = Vector2.Zero;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_mouseDown.Contains(MouseButton.Left))
        {
            return;
        }
        var pos = e.GetPosition(_control);
        var vec = new Vector2((float)pos.X, (float)pos.Y);
        _mouseDelta = (_lastMousePos -vec);
        _lastMousePos = vec;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _mouseDown.Add(ConvertMouseButton(e.GetCurrentPoint(_control).Properties.PointerUpdateKind));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _mouseDown.Remove(ConvertMouseButton(e.GetCurrentPoint(_control).Properties.PointerUpdateKind));

        if (!_mouseDown.Contains(MouseButton.Left))
        {
            _mouseDelta = Vector2.Zero;
        }
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        _mouseScrolling = new Vector2((float)e.Delta.X, (float)e.Delta.Y);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _keyDown.Add(ConvertKey(e.Key));
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        _keyDown.Remove(ConvertKey(e.Key));
    }

    public bool IsMouseButtonDown(MouseButton button) => _mouseDown.Contains(button);
    public bool IsMouseButtonUp(MouseButton button) => !_mouseDown.Contains(button);
    public Vector2 GetMouseDelta() => _mouseDelta;
    public void DisableRelativeMouseMode()
    {
    }

    public Vector2 GetMousePosition() => _lastMousePos;
    public bool IsMouseScrolling(out Vector2 scroll)
    {
        scroll = _mouseScrolling;
        return scroll != Vector2.Zero;
    }

    public bool IsKeyPressed(KeyboardKey key, bool enableRepeat = false)
    {
        return false;
    }

    public bool IsKeyDown(KeyboardKey key) => _keyDown.Contains(key);
    public bool IsKeyReleased(KeyboardKey key)
    {
        return false;
    }

    public bool IsKeyUp(KeyboardKey key) => !_keyDown.Contains(key);

    // Stubs
    public void ShowCursor() { }
    public void HideCursor() { }
    public bool IsCursorShown() => true;
    public ICursor GetMouseCursor() => null!;
    public void SetMouseCursor(ICursor cursor) { }
    public bool IsRelativeMouseModeEnabled()
    {
        return false;
    }

    public void EnableRelativeMouseMode()
    {
    }

    public void SetMousePosition(Vector2 position) { }
    public bool IsMouseButtonPressed(MouseButton button) => false;
    public bool IsMouseButtonReleased(MouseButton button) => false;
    public bool IsMouseButtonDoubleClicked(MouseButton button) => false;
    public bool IsMouseMoving(out Vector2 pos) { pos = _lastMousePos; return true; }
    public bool IsTextInputActive() => false;
    public void EnableTextInput() { }
    public void DisableTextInput() { }
    public bool GetTypedText(out string text) { text = ""; return false; }
    public string GetClipboardText() => "";
    public void SetClipboardText(string text) { }

    // Gamepad handling – optional
    public uint GetAvailableGamepadCount() => 0;
    public bool IsGamepadAvailable(uint gamepad) => false;
    public string GetGamepadName(uint gamepad) => "";
    public void RumbleGamepad(uint gamepad, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs) { }
    public float GetGamepadAxisMovement(uint gamepad, GamepadAxis axis) => 0;
    public bool IsGamepadButtonPressed(uint gamepad, GamepadButton button) => false;
    public bool IsGamepadButtonDown(uint gamepad, GamepadButton button) => false;
    public bool IsGamepadButtonReleased(uint gamepad, GamepadButton button) => false;
    public bool IsGamepadButtonUp(uint gamepad, GamepadButton button) => false;
    public bool IsFileDragDropped(out string path) { path = ""; return false; }

    // Helpers for mapping
    private MouseButton ConvertMouseButton(PointerUpdateKind kind) =>
        kind switch
        {
            PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => MouseButton.Left,
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => MouseButton.Right,
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => MouseButton.Middle,
            _ => MouseButton.X1
        };

    private KeyboardKey ConvertKey(Key key) =>
        key switch
        {
            Key.W => KeyboardKey.W,
            Key.A => KeyboardKey.A,
            Key.S => KeyboardKey.S,
            Key.D => KeyboardKey.D,
            Key.Space => KeyboardKey.Space,
            Key.LeftShift => KeyboardKey.ShiftLeft,
            _ => KeyboardKey.Unknown
        };

    public void Dispose()
    {
    }
}