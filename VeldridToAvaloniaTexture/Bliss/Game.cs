using Avalonia.Controls;
using Bliss.CSharp;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics.Rendering.Batches.Sprites;
using Bliss.CSharp.Graphics.Rendering.Passes;
using Bliss.CSharp.Graphics.Rendering.Renderers;
using Bliss.CSharp.Images;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Textures.Cubemaps;
using Bliss.CSharp.Transformations;
using MiniAudioEx;
using SkiaSharp;
using System;
using System.Numerics;
using System.Threading;
using Veldrid;
using Color = Bliss.CSharp.Colors.Color;
using Image = Bliss.CSharp.Images.Image;
using Rectangle = Bliss.CSharp.Transformations.Rectangle;

namespace Bliss.Test;

public class Game : Disposable
{
    private readonly GameSettings _settings;
    private GraphicsDevice _graphicsDevice;
    private CommandList _commandList;
    private FullScreenRenderPass _fullScreenRenderPass;
    private RenderTexture2D _fullScreenTexture;

    private ImmediateRenderer _immediateRenderer;
    private SpriteBatch _spriteBatch;
    //private PrimitiveBatch _primitiveBatch;
    private AnimatedImage _animatedImage;
    private Texture2D _gif;
    private Font _font;
    private Texture2D _logoTexture;
    private Cubemap _cubemap;
    private Texture2D _cubemapTexture;
    private Texture2D _button;
    private RenderTexture2D _finalOutputTexture;

    private Cam3D _cam3D;
    private Model _playerModel;
    private Model _planeModel;
    private Model _treeModel;
    private Model _cyberCarModel;
    private Model _skullModel;
    private Texture2D _cyberTexture;

    private Texture2D _customMeshTexture;
    private Mesh _customPoly;
    private Mesh _customCube;
    private Mesh _customSphere;
    private Mesh _customHemishpere;
    private Mesh _customCylinder;
    private Mesh _customCapsule;
    private Mesh _customCone;
    private Mesh _customTorus;
    private Mesh _customKnot;
    private Mesh _customHeighmap;
    //private Mesh _customCubemap;

    private int _frameCount;
    private bool _playingAnim;

    private string _textInput;

    private Texture? _stagingTexture;
    private readonly Control _host;
    private SKBitmap? _skBitmap;
    private DummyWindow _dummyWindow;

    private double _fixedFrameRate;

    private readonly double _fixedUpdateTimeStep;
    private double _fixedUpdateTimer;

    private readonly Lock _frameLock = new();


    public Game(GameSettings settings, Control host)
    {
        _settings = settings;
        _fixedUpdateTimeStep = settings.FixedTimeStep;
        _host = host;
    }

    public void Tick()
    {
        Time.Update();

        Input.Begin();

        AudioContext.Update();
        Update();
        AfterUpdate();

        _fixedUpdateTimer += Time.Delta;
        while (_fixedUpdateTimer >= _fixedUpdateTimeStep)
        {
            FixedUpdate();
            _fixedUpdateTimer -= _fixedUpdateTimeStep;
        }

        Draw(_graphicsDevice, _commandList);
    }

    protected virtual void Init()
    {
        _fullScreenRenderPass = new FullScreenRenderPass(_graphicsDevice);
        _fullScreenTexture = new RenderTexture2D(_graphicsDevice, (uint)_settings.Width, (uint)_settings.Height, _settings.SampleCount);
        _finalOutputTexture = new RenderTexture2D(_graphicsDevice, (uint)_settings.Width, (uint)_settings.Height, _settings.SampleCount);
        _stagingTexture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            _finalOutputTexture.Width,
            _finalOutputTexture.Height,
            mipLevels: 1,
            arrayLayers: 1,
            _finalOutputTexture.ColorTexture.Format,
            TextureUsage.Staging));

        _skBitmap = new SKBitmap((int)_finalOutputTexture.Width, (int)_finalOutputTexture.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        _dummyWindow = new DummyWindow();
        _dummyWindow.SetSize(_settings.Width, _settings.Height);
        _immediateRenderer = new ImmediateRenderer(_graphicsDevice);
        _spriteBatch = new SpriteBatch(_graphicsDevice, _dummyWindow);
        //_primitiveBatch = new PrimitiveBatch(GraphicsDevice, MainWindow);

        _font = new Font("content/fonts/fontoe.ttf");
        _logoTexture = new Texture2D(_graphicsDevice, "content/images/logo.png");
        _animatedImage = new AnimatedImage("content/animated.gif");
        _gif = new Texture2D(_graphicsDevice, _animatedImage.SpriteSheet);

        float aspectRatio = (float)_settings.Width / (float)_settings.Height;
        _cam3D = new Cam3D(new Vector3(0, 3, -3), new Vector3(0, 1.5F, 0), aspectRatio);
        _playerModel = Model.Load(_graphicsDevice, "content/player.glb");
        _planeModel = Model.Load(_graphicsDevice, "content/plane.glb");
        _treeModel = Model.Load(_graphicsDevice, "content/tree.glb");
        _cyberCarModel = Model.Load(_graphicsDevice, "content/cybercar.glb", false);
        _skullModel = Model.Load(_graphicsDevice, "E:\\Blender\\skull.obj", false);
        _cyberTexture = new Texture2D(_graphicsDevice, "content/cybercar.png");

        foreach (Mesh mesh in _cyberCarModel.Meshes)
        {
            mesh.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _cyberTexture);
        }

        _customMeshTexture = new Texture2D(_graphicsDevice, "content/cube.png");

        _customPoly = Mesh.GenPoly(_graphicsDevice, 40, 1);
        _customPoly.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customCube = Mesh.GenCube(_graphicsDevice, 1, 1, 1);
        _customCube.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customSphere = Mesh.GenSphere(_graphicsDevice, 1F, 40, 40);
        _customSphere.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customHemishpere = Mesh.GenHemisphere(_graphicsDevice, 1F, 40, 40);
        _customHemishpere.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customCylinder = Mesh.GenCylinder(_graphicsDevice, 1F, 1F, 40);
        _customCylinder.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customCapsule = Mesh.GenCapsule(_graphicsDevice, 1, 1, 60);
        _customCapsule.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customCone = Mesh.GenCone(_graphicsDevice, 1F, 1F, 40);
        _customCone.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customTorus = Mesh.GenTorus(_graphicsDevice, 2.0F, 1F, 40, 40);
        _customTorus.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customKnot = Mesh.GenKnot(_graphicsDevice, 1F, 1F, 40, 40);
        _customKnot.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), _customMeshTexture);

        _customHeighmap = Mesh.GenHeightmap(_graphicsDevice, new Image("content/heightmap.png"), new Vector3(1, 1, 1));
        _customHeighmap.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), new Texture2D(_graphicsDevice, "content/heightmap.png"));

        _cubemap = new Cubemap(_graphicsDevice, "content/cubemap.png");
        _cubemapTexture = new Texture2D(_graphicsDevice, _cubemap.Images[5][0]);

        _button = new Texture2D(_graphicsDevice, "content/button.png");
    }

    protected virtual void Update()
    {
        if (Input.IsMouseButtonDoubleClicked(MouseButton.Left))
        {
            Logger.Error("DOUBLE CLICKED!");
        }

        _cam3D.Update((float)Time.Delta);
    }

    protected virtual void AfterUpdate() { }

    protected virtual void FixedUpdate()
    {
        if (Input.IsKeyDown(KeyboardKey.H))
        {
            _playingAnim = true;
            _frameCount++;

            if (_frameCount >= _playerModel.Animations[1].FrameCount)
            {
                _frameCount = 0;
            }
        }
    }

    protected virtual void Draw(GraphicsDevice graphicsDevice, CommandList commandList)
    {
        commandList.Begin();
        commandList.SetFramebuffer(_fullScreenTexture.Framebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());
        commandList.ClearDepthStencil(1.0F);

        // Enables relative mouse mod.
        Input.EnableRelativeMouseMode();

        // Drawing 3D.
        _cam3D.Begin();

        // ImmediateRenderer START
        //_immediateRenderer.SetTexture(_customMeshTexture);
        //_immediateRenderer.DrawCube(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(9, 0, 6) }, new Vector3(1, 1, 1));
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawCubeWires(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(11, 0, 6) }, new Vector3(1, 1, 1), Color.Green);

        //_immediateRenderer.SetTexture(_customMeshTexture);
        //_immediateRenderer.DrawSphere(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(13, 0, 6) }, 1, 40, 40);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawSphereWires(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(15, 0, 6) }, 1, 40, 40, Color.Green);

        //_immediateRenderer.SetTexture(_customMeshTexture);
        //_immediateRenderer.DrawHemisphere(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(17, 0, 6) }, 1, 40, 40);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawHemisphereWires(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(19, 0, 6) }, 1, 40, 40, Color.Green);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawLine(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Vector3(20.5F, 0, 6), new Vector3(21.5F, 0, 6), Color.Green);

        _immediateRenderer.DrawGrid(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform(), 96, 1, 16, Color.Gray);

        //_immediateRenderer.SetTexture(_customMeshTexture);
        //_immediateRenderer.DrawCylinder(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(23, 0, 6) }, 1, 1, 40);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawCylinderWires(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(25, 0, 6) }, 1, 1, 40, Color.Green);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawBoundingBox(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(28, 0, 6) }, _playerModel.BoundingBox, Color.Green);

        //_immediateRenderer.SetTexture(_customMeshTexture);
        //_immediateRenderer.DrawCapsule(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(31, 0, 6) }, 1, 1, 40);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawCapsuleWires(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(33, 0, 6) }, 1, 1, 40, Color.Green);

        //_immediateRenderer.SetTexture(_logoTexture, sourceRect: new Rectangle(0, 0, (int)_logoTexture.Width, (int)_logoTexture.Height));
        //_immediateRenderer.DrawBillboard(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Vector3(35, 0, 6));
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.SetTexture(_customMeshTexture);
        //_immediateRenderer.DrawCone(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(38, 0, 6) }, 1, 1, 40);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawConeWires(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(40, 0, 6) }, 1, 1, 40, Color.Green);

        //_immediateRenderer.SetTexture(_customMeshTexture);
        //_immediateRenderer.DrawTorus(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(42, 0, 6) }, 2, 1, 40, 40);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawTorusWires(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(44, 0, 6) }, 2, 1, 40, 40, Color.Green);

        //_immediateRenderer.SetTexture(_customMeshTexture);
        //_immediateRenderer.DrawKnot(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(46, 0, 6) }, 1, 1, 40, 40);
        //_immediateRenderer.ResetSettings();

        //_immediateRenderer.DrawKnotWires(commandList, _fullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(48, 0, 6) }, 1, 1, 40, 40, Color.Green);
        // ImmediateRenderer END

        //_customPoly.Draw(commandList, new Transform() { Translation = new Vector3(9, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customCube.Draw(commandList, new Transform() { Translation = new Vector3(11, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customSphere.Draw(commandList, new Transform() { Translation = new Vector3(13, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customHemishpere.Draw(commandList, new Transform() { Translation = new Vector3(15, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customCylinder.Draw(commandList, new Transform() { Translation = new Vector3(17, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customCapsule.Draw(commandList, new Transform() { Translation = new Vector3(19, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customCone.Draw(commandList, new Transform() { Translation = new Vector3(21, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customTorus.Draw(commandList, new Transform() { Translation = new Vector3(23, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customKnot.Draw(commandList, new Transform() { Translation = new Vector3(25, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        //_customHeighmap.Draw(commandList, new Transform() { Translation = new Vector3(27, 0, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);

        //if (_cam3D.GetFrustum().ContainsBox(_planeModel.BoundingBox))
        //{
        //    _planeModel.Draw(commandList, new Transform(), _fullScreenTexture.Framebuffer.OutputDescription);
        //}

        //_treeModel.Draw(commandList, new Transform() { Translation = new Vector3(0, 0, 20) }, _fullScreenTexture.Framebuffer.OutputDescription, rasterizerState: RasterizerStateDescription.CULL_NONE);

        _cyberCarModel.Draw(commandList, new Transform() { Translation = new Vector3(10, 0, 20) }, _fullScreenTexture.Framebuffer.OutputDescription);
        _skullModel.Draw(commandList, new Transform() { Translation = new Vector3(-1, 2, 1) }, _fullScreenTexture.Framebuffer.OutputDescription);

        if (Input.IsKeyPressed(KeyboardKey.G))
        {
            _playerModel.ResetAnimationBones(commandList);
            _playingAnim = false;
            Logger.Error("RESET ANIM");
        }

        if (_cam3D.GetFrustum().ContainsBox(_playerModel.BoundingBox))
        {
            if (_playingAnim)
            {
                _playerModel.UpdateAnimationBones(commandList, _playerModel.Animations[1], _frameCount);
            }
            _playerModel.Draw(commandList, new Transform() { Translation = new Vector3(0, 0.05F, 0) }, _fullScreenTexture.Framebuffer.OutputDescription);
        }

        //_playerModel.ResetAnimationBones(commandList);
        //_playerModel.Draw(commandList, new Transform() { Translation = new Vector3(4, 0.05F, 0)}, FullScreenTexture.Framebuffer.OutputDescription);

        _cam3D.End();

        // SpriteBatch Drawing.
        _spriteBatch.Begin(commandList, _fullScreenTexture.Framebuffer.OutputDescription);

        //if (Input.IsKeyPressed(KeyboardKey.O))
        //{
        //    Input.EnableTextInput();
        //}

        //if (Input.IsKeyPressed(KeyboardKey.Enter))
        //{
        //    Input.DisableTextInput();
        //}

        //if (Input.IsTextInputActive())
        //{
        //    if (Input.GetTypedText(out string text))
        //    {
        //        _textInput += text;
        //    }

        //    if (Input.IsKeyPressed(KeyboardKey.BackSpace, true))
        //    {
        //        if (_textInput.Length > 0)
        //        {
        //            _textInput = _textInput.Remove(_textInput.Length - 1, 1);
        //        }
        //    }
        //}

        //_spriteBatch.DrawText(_font, $"Text Input: {_textInput}", new Vector2(80, 80), 18);

        _spriteBatch.DrawText(_font, $"FPS: {(int)(1.0F / Time.Delta)}", new Vector2(5, 5), 18);

        //int frame = 4;
        //_animatedImage.GetFrameInfo(frame, out int width, out int height, out float duration);

        //_spriteBatch.PushRasterizerState(_spriteBatch.GetCurrentRasterizerState() with { ScissorTestEnabled = true });
        //_spriteBatch.PushScissorRect(new Rectangle(30, 30, (int)(width / 2.0F * 0.2F), (int)(_height / 2.0F * 0.2F)));
        //_spriteBatch.DrawTexture(_gif, new Vector2(30, 30), sourceRect: new Rectangle(width * frame, 0, width, height), scale: new Vector2(0.2F, 0.2F), color: new Color(255, 255, 255, 155));
        //_spriteBatch.PopScissorRect();
        //_spriteBatch.PopRasterizerState();

        //_spriteBatch.DrawTexture(_customMeshTexture, Input.GetMousePosition(), scale: new Vector2(3, 3));
        //_spriteBatch.DrawTexture(_button, new Vector2(300, 300), scale: new Vector2(3, 3));

        _spriteBatch.End();

        //_primitiveBatch.Begin(commandList, FullScreenTexture.Framebuffer.OutputDescription);

        //_primitiveBatch.PushRasterizerState(_primitiveBatch.GetCurrentRasterizerState() with { ScissorTestEnabled = true });
        //_primitiveBatch.PushScissorRect(new Rectangle(90, 90, 40, 80));
        //_primitiveBatch.DrawFilledCircle(new Vector2(130, 130), 40, 40, 0.5F, new Color(130, 130, 255, 120));
        //_primitiveBatch.PopScissorRect();
        //_primitiveBatch.PopRasterizerState();

        //_primitiveBatch.DrawFilledRectangle(new RectangleF(200, 200, 100, 100), origin: new Vector2(0, 0), rotation: _frameCount, color: Color.Green);
        //_primitiveBatch.DrawEmptyRectangle(new RectangleF(200, 200, 100, 100), 4, origin: new Vector2(0, 0), rotation: _frameCount, color: Color.Red);

        //_primitiveBatch.End();

        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.WaitForIdle();

        // Draw ScreenPass.
        commandList.Begin();

        if (_fullScreenTexture.SampleCount != TextureSampleCount.Count1)
        {
            commandList.ResolveTexture(_fullScreenTexture.ColorTexture, _fullScreenTexture.DestinationTexture);
        }

        commandList.SetFramebuffer(_finalOutputTexture.Framebuffer);
        ////commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());

        ////FullScreenRenderPass.Draw(commandList, FullScreenTexture, GraphicsDevice.SwapchainFramebuffer.OutputDescription);
        _fullScreenRenderPass.Draw(commandList, _fullScreenTexture, _finalOutputTexture.Framebuffer.OutputDescription);

        commandList.End();

        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.WaitForIdle();

        //graphicsDevice.SwapBuffers();
        SaveFrame();
    }

    protected virtual void OnResize(Rectangle rectangle)
    {
        //GraphicsDevice.MainSwapchain.Resize((uint)rectangle.Width, (uint)rectangle.Height);
        _fullScreenTexture.Resize((uint)rectangle.Width, (uint)rectangle.Height);
        _finalOutputTexture.Resize((uint)rectangle.Width, (uint)rectangle.Height);
        _cam3D.Resize((uint)rectangle.Width, (uint)rectangle.Height);

        _stagingTexture?.Dispose();
        _stagingTexture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            _fullScreenTexture.Width,
            _fullScreenTexture.Height,
            mipLevels: 1,
            arrayLayers: 1,
            _fullScreenTexture.ColorTexture.Format,
            TextureUsage.Staging));

        _dummyWindow.SetSize(rectangle.Width, rectangle.Height);
    }

    public int GetTargetFps()
    {
        return (int)(1.0F / _fixedFrameRate);
    }

    public void SetTargetFps(float fps)
    {
        _fixedFrameRate = 1.0F / fps;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _playerModel.Dispose();
            _planeModel.Dispose();
            _treeModel.Dispose();

            AudioContext.Deinitialize();
            GlobalResource.Destroy();
            Input.Destroy();
            //MainWindow.Dispose();
            _graphicsDevice.Dispose();
        }
    }

    private void SaveFrame()
    {
        _graphicsDevice.WaitForIdle();

        if (_stagingTexture == null || _stagingTexture.Width != _finalOutputTexture.Width || _stagingTexture.Height != _finalOutputTexture.Height)
        {
            _stagingTexture?.Dispose();
            _stagingTexture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                _finalOutputTexture.Width,
                _finalOutputTexture.Height,
                mipLevels: 1,
                arrayLayers: 1,
                _finalOutputTexture.ColorTexture.Format,
                TextureUsage.Staging));
        }

        _commandList.Begin();
        _commandList.CopyTexture(_finalOutputTexture.ColorTexture, _stagingTexture);
        _commandList.End();

        _graphicsDevice.SubmitCommands(_commandList);
        _graphicsDevice.WaitForIdle();

        MappedResource mapped = _graphicsDevice.Map(_stagingTexture, MapMode.Read);
        if (mapped.Data == IntPtr.Zero)
        {
            _graphicsDevice.Unmap(_stagingTexture);
            return;
        }

        lock (_frameLock)
        {
            unsafe
            {
                var srcPtr = (byte*)mapped.Data.ToPointer();
                var dstPtr = (byte*)_skBitmap!.GetPixels().ToPointer();
                var srcPitch = mapped.RowPitch;
                var dstPitch = (long)_skBitmap.Width * _skBitmap.BytesPerPixel;

                for (int y = 0; y < _skBitmap.Height; y++)
                {
                    var srcOffset = srcPtr + (y * srcPitch);
                    var dstOffset = dstPtr + (y * dstPitch);
                    Buffer.MemoryCopy(srcOffset, dstOffset, dstPitch, dstPitch);
                }
            }
        }

        _graphicsDevice.Unmap(_stagingTexture);
    }


    public void ResizeTo(int width, int height)
    {
        if (_fullScreenTexture.Width == width && _fullScreenTexture.Height == height)
        {
            return;
        }

        OnResize(new Rectangle(0, 0, width, height));

        lock (_frameLock)
        {
            _skBitmap?.Dispose();
            _skBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        }
    }

    public void Prepare()
    {
        var options = new GraphicsDeviceOptions()
        {
            Debug = false,
            SyncToVerticalBlank = _settings.VSync,
            SwapchainDepthFormat = null,
            HasMainSwapchain = false,
            PreferDepthRangeZeroToOne = true,
            ResourceBindingModel = ResourceBindingModel.Improved,
            PreferStandardClipSpaceYDirection = true,
        };

        var graphicsDevice = GraphicsDevice.CreateVulkan(options);
        _graphicsDevice = graphicsDevice;

        Time.Init();

        SetTargetFps(_settings.TargetFps);

        _commandList = graphicsDevice.ResourceFactory.CreateCommandList();

        GlobalResource.Init(graphicsDevice);

        Input.Init(new AvaloniaInputContext(_host));

        AudioContext.Initialize(44100, 2);

        Init();
    }


    public SKBitmap GetFrame()
    {
        lock (_frameLock)
        {
            return _skBitmap!;
        }
    }
}