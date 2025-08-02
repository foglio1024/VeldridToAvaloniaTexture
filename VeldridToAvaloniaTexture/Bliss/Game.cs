using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Bliss.CSharp;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Rendering.Batches.Primitives;
using Bliss.CSharp.Graphics.Rendering.Batches.Sprites;
using Bliss.CSharp.Graphics.Rendering.Passes;
using Bliss.CSharp.Graphics.Rendering.Renderers;
using Bliss.CSharp.Images;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Contexts;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Textures.Cubemaps;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using MiniAudioEx;
using SkiaSharp;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Vk;
using Color = Bliss.CSharp.Colors.Color;
using Image = Bliss.CSharp.Images.Image;
using Rectangle = Bliss.CSharp.Transformations.Rectangle;
using Window = Bliss.CSharp.Windowing.Window;

namespace Bliss.Test;

public class Game : Disposable
{

    public static Game Instance { get; private set; }
    public GameSettings Settings { get; private set; }

    //public IWindow MainWindow { get; private set; }
    public GraphicsDevice GraphicsDevice { get; private set; }
    public CommandList CommandList { get; private set; }

    private double _fixedFrameRate;

    private readonly double _fixedUpdateTimeStep;
    private double _fixedUpdateTimer;

    public FullScreenRenderPass FullScreenRenderPass { get; private set; }
    public RenderTexture2D FullScreenTexture { get; private set; }

    private ImmediateRenderer _immediateRenderer;
    //private SpriteBatch _spriteBatch;
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
    private Texture2D _cynerTexture;

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

    Lock _frame = new Lock();


    public Game(GameSettings settings, Control host)
    {
        Instance = this;
        this.Settings = settings;
        this._fixedUpdateTimeStep = settings.FixedTimeStep;
        _host = host;

    }

    public void Run()
    {

        Logger.Info("Start main loops...");
        //while (this.MainWindow.Exists) {
        Task.Run(async () =>
        {
            while (true)
            {
                //if (this.GetTargetFps() != 0 && Time.Timer.Elapsed.TotalSeconds <= this._fixedFrameRate)
                //{
                //    continue;
                //}
               
                Tick();

                Draw(GraphicsDevice, CommandList);
                Input.End();

                await Task.Delay(16);
            }
        }
        );

        //Logger.Warn("Application shuts down!");
        //this.OnClose();
    }

    public void Tick()
    {
        Time.Update();

        //this.MainWindow.PumpEvents();
        Input.Begin();

        AudioContext.Update();
        this.Update();
        this.AfterUpdate();

        this._fixedUpdateTimer += Time.Delta;
        while (this._fixedUpdateTimer >= this._fixedUpdateTimeStep)
        {
            this.FixedUpdate();
            this._fixedUpdateTimer -= this._fixedUpdateTimeStep;
        }

        this.Draw(GraphicsDevice, this.CommandList);
    }


    private Texture? _stagingTexture;
    private readonly Control _host;
    private SKBitmap _skBitmap;

    protected virtual void Init()
    {
        this.FullScreenRenderPass = new FullScreenRenderPass(this.GraphicsDevice);
        this.FullScreenTexture = new RenderTexture2D(this.GraphicsDevice, (uint)this.Settings.Width, (uint)this.Settings.Height, this.Settings.SampleCount);
        this._finalOutputTexture = new RenderTexture2D(this.GraphicsDevice, (uint)this.Settings.Width, (uint)this.Settings.Height, this.Settings.SampleCount);
        _stagingTexture = GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            _finalOutputTexture.Width,
            _finalOutputTexture.Height,
            mipLevels: 1,
            arrayLayers: 1,
            _finalOutputTexture.ColorTexture.Format,
            TextureUsage.Staging));

        _skBitmap = new SKBitmap((int)_finalOutputTexture.Width, (int)_finalOutputTexture.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

        this._immediateRenderer = new ImmediateRenderer(this.GraphicsDevice);
        //this._spriteBatch = new SpriteBatch(this.GraphicsDevice, this.MainWindow);
        //this._primitiveBatch = new PrimitiveBatch(this.GraphicsDevice, this.MainWindow);

        this._font = new Font("content/fonts/fontoe.ttf");
        this._logoTexture = new Texture2D(this.GraphicsDevice, "content/images/logo.png");
        this._animatedImage = new AnimatedImage("content/animated.gif");
        this._gif = new Texture2D(this.GraphicsDevice, this._animatedImage.SpriteSheet);

        float aspectRatio = (float)this.Settings.Width / (float)this.Settings.Height;
        this._cam3D = new Cam3D(new Vector3(0, 3, -3), new Vector3(0, 1.5F, 0), aspectRatio);
        this._playerModel = Model.Load(this.GraphicsDevice, "content/player.glb");
        this._planeModel = Model.Load(this.GraphicsDevice, "content/plane.glb");
        this._treeModel = Model.Load(this.GraphicsDevice, "content/tree.glb");
        this._cyberCarModel = Model.Load(this.GraphicsDevice, "content/cybercar.glb", false);
        this._cynerTexture = new Texture2D(this.GraphicsDevice, "content/cybercar.png");

        foreach (Mesh mesh in _cyberCarModel.Meshes)
        {
            mesh.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._cynerTexture);
        }

        this._customMeshTexture = new Texture2D(this.GraphicsDevice, "content/cube.png");

        this._customPoly = Mesh.GenPoly(this.GraphicsDevice, 40, 1);
        this._customPoly.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customCube = Mesh.GenCube(this.GraphicsDevice, 1, 1, 1);
        this._customCube.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customSphere = Mesh.GenSphere(this.GraphicsDevice, 1F, 40, 40);
        this._customSphere.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customHemishpere = Mesh.GenHemisphere(this.GraphicsDevice, 1F, 40, 40);
        this._customHemishpere.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customCylinder = Mesh.GenCylinder(this.GraphicsDevice, 1F, 1F, 40);
        this._customCylinder.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customCapsule = Mesh.GenCapsule(this.GraphicsDevice, 1, 1, 60);
        this._customCapsule.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customCone = Mesh.GenCone(this.GraphicsDevice, 1F, 1F, 40);
        this._customCone.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customTorus = Mesh.GenTorus(this.GraphicsDevice, 2.0F, 1F, 40, 40);
        this._customTorus.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customKnot = Mesh.GenKnot(this.GraphicsDevice, 1F, 1F, 40, 40);
        this._customKnot.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), this._customMeshTexture);

        this._customHeighmap = Mesh.GenHeightmap(this.GraphicsDevice, new Image("content/heightmap.png"), new Vector3(1, 1, 1));
        this._customHeighmap.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), new Texture2D(this.GraphicsDevice, "content/heightmap.png"));

        this._cubemap = new Cubemap(this.GraphicsDevice, "content/cubemap.png");
        this._cubemapTexture = new Texture2D(this.GraphicsDevice, this._cubemap.Images[5][0]);

        this._button = new Texture2D(this.GraphicsDevice, "content/button.png");
    }

    protected virtual void Update()
    {
        if (Input.IsMouseButtonDoubleClicked(MouseButton.Left))
        {
            Logger.Error("DOUBLE CLICKED!");
        }

        this._cam3D.Update((float)Time.Delta);
    }

    protected virtual void AfterUpdate() { }

    protected virtual void FixedUpdate()
    {
        if (Input.IsKeyDown(KeyboardKey.H))
        {
            this._playingAnim = true;
            this._frameCount++;

            if (this._frameCount >= this._playerModel.Animations[1].FrameCount)
            {
                this._frameCount = 0;
            }
        }
    }

    protected virtual void Draw(GraphicsDevice graphicsDevice, CommandList commandList)
    {
        commandList.Begin();
        commandList.SetFramebuffer(this.FullScreenTexture.Framebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());
        commandList.ClearDepthStencil(1.0F);

        // Enables relative mouse mod.
        Input.EnableRelativeMouseMode();

        // Drawing 3D.
        this._cam3D.Begin();

        // ImmediateRenderer START
        this._immediateRenderer.SetTexture(this._customMeshTexture);
        this._immediateRenderer.DrawCube(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(9, 0, 6) }, new Vector3(1, 1, 1));
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawCubeWires(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(11, 0, 6) }, new Vector3(1, 1, 1), Color.Green);

        this._immediateRenderer.SetTexture(this._customMeshTexture);
        this._immediateRenderer.DrawSphere(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(13, 0, 6) }, 1, 40, 40);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawSphereWires(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(15, 0, 6) }, 1, 40, 40, Color.Green);

        this._immediateRenderer.SetTexture(this._customMeshTexture);
        this._immediateRenderer.DrawHemisphere(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(17, 0, 6) }, 1, 40, 40);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawHemisphereWires(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(19, 0, 6) }, 1, 40, 40, Color.Green);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawLine(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Vector3(20.5F, 0, 6), new Vector3(21.5F, 0, 6), Color.Green);

        this._immediateRenderer.DrawGrid(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform(), 96, 1, 16, Color.Gray);

        this._immediateRenderer.SetTexture(this._customMeshTexture);
        this._immediateRenderer.DrawCylinder(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(23, 0, 6) }, 1, 1, 40);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawCylinderWires(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(25, 0, 6) }, 1, 1, 40, Color.Green);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawBoundingBox(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(28, 0, 6) }, this._playerModel.BoundingBox, Color.Green);

        this._immediateRenderer.SetTexture(this._customMeshTexture);
        this._immediateRenderer.DrawCapsule(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(31, 0, 6) }, 1, 1, 40);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawCapsuleWires(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(33, 0, 6) }, 1, 1, 40, Color.Green);

        this._immediateRenderer.SetTexture(this._logoTexture, sourceRect: new Rectangle(0, 0, (int)this._logoTexture.Width, (int)this._logoTexture.Height));
        this._immediateRenderer.DrawBillboard(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Vector3(35, 0, 6));
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.SetTexture(this._customMeshTexture);
        this._immediateRenderer.DrawCone(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(38, 0, 6) }, 1, 1, 40);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawConeWires(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(40, 0, 6) }, 1, 1, 40, Color.Green);

        this._immediateRenderer.SetTexture(this._customMeshTexture);
        this._immediateRenderer.DrawTorus(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(42, 0, 6) }, 2, 1, 40, 40);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawTorusWires(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(44, 0, 6) }, 2, 1, 40, 40, Color.Green);

        this._immediateRenderer.SetTexture(this._customMeshTexture);
        this._immediateRenderer.DrawKnot(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(46, 0, 6) }, 1, 1, 40, 40);
        this._immediateRenderer.ResetSettings();

        this._immediateRenderer.DrawKnotWires(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(48, 0, 6) }, 1, 1, 40, 40, Color.Green);
        // ImmediateRenderer END

        this._customPoly.Draw(commandList, new Transform() { Translation = new Vector3(9, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customCube.Draw(commandList, new Transform() { Translation = new Vector3(11, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customSphere.Draw(commandList, new Transform() { Translation = new Vector3(13, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customHemishpere.Draw(commandList, new Transform() { Translation = new Vector3(15, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customCylinder.Draw(commandList, new Transform() { Translation = new Vector3(17, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customCapsule.Draw(commandList, new Transform() { Translation = new Vector3(19, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customCone.Draw(commandList, new Transform() { Translation = new Vector3(21, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customTorus.Draw(commandList, new Transform() { Translation = new Vector3(23, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customKnot.Draw(commandList, new Transform() { Translation = new Vector3(25, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._customHeighmap.Draw(commandList, new Transform() { Translation = new Vector3(27, 0, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);

        if (this._cam3D.GetFrustum().ContainsBox(this._planeModel.BoundingBox))
        {
            this._planeModel.Draw(commandList, new Transform(), this.FullScreenTexture.Framebuffer.OutputDescription);
        }

        this._treeModel.Draw(commandList, new Transform() { Translation = new Vector3(0, 0, 20) }, this.FullScreenTexture.Framebuffer.OutputDescription, rasterizerState: RasterizerStateDescription.CULL_NONE);

        this._cyberCarModel.Draw(commandList, new Transform() { Translation = new Vector3(10, 0, 20) }, this.FullScreenTexture.Framebuffer.OutputDescription);

        if (Input.IsKeyPressed(KeyboardKey.G))
        {
            this._playerModel.ResetAnimationBones(commandList);
            this._playingAnim = false;
            Logger.Error("RESET ANIM");
        }

        if (this._cam3D.GetFrustum().ContainsBox(this._playerModel.BoundingBox))
        {
            if (this._playingAnim)
            {
                this._playerModel.UpdateAnimationBones(commandList, this._playerModel.Animations[1], this._frameCount);
            }
            this._playerModel.Draw(commandList, new Transform() { Translation = new Vector3(0, 0.05F, 0) }, this.FullScreenTexture.Framebuffer.OutputDescription);
        }

        //this._playerModel.ResetAnimationBones(commandList);
        //this._playerModel.Draw(commandList, new Transform() { Translation = new Vector3(4, 0.05F, 0)}, this.FullScreenTexture.Framebuffer.OutputDescription);

        this._cam3D.End();

        // SpriteBatch Drawing.
        //this._spriteBatch.Begin(commandList, this.FullScreenTexture.Framebuffer.OutputDescription);

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
        //        this._textInput += text;
        //    }

        //    if (Input.IsKeyPressed(KeyboardKey.BackSpace, true))
        //    {
        //        if (this._textInput.Length > 0)
        //        {
        //            this._textInput = this._textInput.Remove(this._textInput.Length - 1, 1);
        //        }
        //    }
        //}

        //this._spriteBatch.DrawText(this._font, $"Text Input: {this._textInput}", new Vector2(80, 80), 18);

        //this._spriteBatch.DrawText(this._font, $"FPS: {(int)(1.0F / Time.Delta)}", new Vector2(5, 5), 18);

        //int frame = 4;
        //this._animatedImage.GetFrameInfo(frame, out int width, out int height, out float duration);

        //this._spriteBatch.PushRasterizerState(this._spriteBatch.GetCurrentRasterizerState() with { ScissorTestEnabled = true });
        //this._spriteBatch.PushScissorRect(new Rectangle(30, 30, (int)(width / 2.0F * 0.2F), (int)(height / 2.0F * 0.2F)));
        //this._spriteBatch.DrawTexture(this._gif, new Vector2(30, 30), sourceRect: new Rectangle(width * frame, 0, width, height), scale: new Vector2(0.2F, 0.2F), color: new Color(255, 255, 255, 155));
        //this._spriteBatch.PopScissorRect();
        //this._spriteBatch.PopRasterizerState();

        //this._spriteBatch.DrawTexture(this._customMeshTexture, Input.GetMousePosition(), scale: new Vector2(3, 3));
        //this._spriteBatch.DrawTexture(this._button, new Vector2(300, 300), scale: new Vector2(3, 3));

        //this._spriteBatch.End();

        //this._primitiveBatch.Begin(commandList, this.FullScreenTexture.Framebuffer.OutputDescription);

        //this._primitiveBatch.PushRasterizerState(this._primitiveBatch.GetCurrentRasterizerState() with { ScissorTestEnabled = true });
        //this._primitiveBatch.PushScissorRect(new Rectangle(90, 90, 40, 80));
        //this._primitiveBatch.DrawFilledCircle(new Vector2(130, 130), 40, 40, 0.5F, new Color(130, 130, 255, 120));
        //this._primitiveBatch.PopScissorRect();
        //this._primitiveBatch.PopRasterizerState();

        //this._primitiveBatch.DrawFilledRectangle(new RectangleF(200, 200, 100, 100), origin: new Vector2(0, 0), rotation: _frameCount, color: Color.Green);
        //this._primitiveBatch.DrawEmptyRectangle(new RectangleF(200, 200, 100, 100), 4, origin: new Vector2(0, 0), rotation: _frameCount, color: Color.Red);

        //this._primitiveBatch.End();

        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.WaitForIdle();

        // Draw ScreenPass.
        commandList.Begin();

        if (this.FullScreenTexture.SampleCount != TextureSampleCount.Count1)
        {
            commandList.ResolveTexture(this.FullScreenTexture.ColorTexture, this.FullScreenTexture.DestinationTexture);
        }

        commandList.SetFramebuffer(_finalOutputTexture.Framebuffer);
        ////commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());

        ////this.FullScreenRenderPass.Draw(commandList, this.FullScreenTexture, this.GraphicsDevice.SwapchainFramebuffer.OutputDescription);
        this.FullScreenRenderPass.Draw(commandList, this.FullScreenTexture, _finalOutputTexture.Framebuffer.OutputDescription);

        commandList.End();

        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.WaitForIdle();

        //graphicsDevice.SwapBuffers();
        this.SaveFrame();
    }

    protected virtual void OnResize(Rectangle rectangle)
    {
        //this.GraphicsDevice.MainSwapchain.Resize((uint)rectangle.Width, (uint)rectangle.Height);
        this.FullScreenTexture.Resize((uint)rectangle.Width, (uint)rectangle.Height);
        this._finalOutputTexture.Resize((uint)rectangle.Width, (uint)rectangle.Height);
        this._cam3D.Resize((uint)rectangle.Width, (uint)rectangle.Height);

        _stagingTexture?.Dispose();
        _stagingTexture = GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            FullScreenTexture.Width,
            FullScreenTexture.Height,
            mipLevels: 1,
            arrayLayers: 1,
            FullScreenTexture.ColorTexture.Format,
            TextureUsage.Staging));

    }

    protected virtual void OnClose() { }

    public int GetTargetFps()
    {
        return (int)(1.0F / this._fixedFrameRate);
    }

    public void SetTargetFps(int fps)
    {
        this._fixedFrameRate = 1.0F / fps;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._playerModel.Dispose();
            this._planeModel.Dispose();
            this._treeModel.Dispose();

            AudioContext.Deinitialize();
            GlobalResource.Destroy();
            Input.Destroy();
            //this.MainWindow.Dispose();
            this.GraphicsDevice.Dispose();
        }
    }

    public void SaveFrame()
    {
        GraphicsDevice.WaitForIdle();

        // Verifica che la texture di staging abbia le stesse dimensioni della texture di destinazione
        if (_stagingTexture == null || _stagingTexture.Width != _finalOutputTexture.Width || _stagingTexture.Height != _finalOutputTexture.Height)
        {
            // Ricrea la texture di staging se le dimensioni non corrispondono
            _stagingTexture?.Dispose();
            _stagingTexture = GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                _finalOutputTexture.Width,
                _finalOutputTexture.Height,
                mipLevels: 1,
                arrayLayers: 1,
                _finalOutputTexture.ColorTexture.Format,
                TextureUsage.Staging));
        }

        CommandList.Begin();
        CommandList.CopyTexture(_finalOutputTexture.ColorTexture, _stagingTexture);
        CommandList.End();

        GraphicsDevice.SubmitCommands(CommandList);
        GraphicsDevice.WaitForIdle();

        // Mappa la texture di staging per leggere i dati.
        var mapped = GraphicsDevice.Map(_stagingTexture, MapMode.Read);

        // Controlla se la mappatura ha successo
        if (mapped.Data == IntPtr.Zero)
        {
            // Se la mappatura fallisce, non procedere
            GraphicsDevice.Unmap(_stagingTexture);
            return;
        }
        //byte[] debugData = new byte[_stagingTexture.Width * _stagingTexture.Height* 4];

        lock (_frame)
        {
            unsafe
            {
                // Ottieni un puntatore ai dati mappati.
                void* src = mapped.Data.ToPointer();

                // Ottieni un puntatore ai pixel della SKBitmap.
                void* dst = _skBitmap.GetPixels().ToPointer();

                // Calcola la dimensione totale in byte da copiare.
                long byteCount = _skBitmap.ByteCount;
                unsafe
                {
                    var srcPtr = (byte*)mapped.Data.ToPointer();
                    var dstPtr = (byte*)_skBitmap.GetPixels().ToPointer(); // per confronto
                    var srcPitch = mapped.RowPitch;
                    var dstPitch = _skBitmap.Width * 4;

                    for (int y = 0; y < _skBitmap.Height; y++)
                    {
                        var srcOffset = srcPtr + (y * srcPitch);
                        var dstOffset = dstPtr + (y * dstPitch);

                        // Copia i dati in un array locale, non direttamente nella SKBitmap
                        //Marshal.Copy((IntPtr)srcOffset, debugData, (int)(y * srcPitch), (int)srcPitch);

                        // Copia i dati nella SKBitmap, come facevi prima
                        Buffer.MemoryCopy(srcOffset, dstOffset, dstPitch, dstPitch);
                    }

                    //using (var stream = File.OpenWrite("debug_bitmap_output.bmp"))
                    //{
                    //    stream.Write(debugData);
                    ////}

                    //var info = new SKImageInfo(
                    //    (int)_finalOutputTexture.Width,
                    //    (int)_finalOutputTexture.Height,
                    //    SKColorType.Rgba8888,
                    //    SKAlphaType.Premul);

                    //using (var bitmap = new SKBitmap(info))
                    //{
                    //    // Copia i byte nell'area dei pixel della bitmap
                    //    Marshal.Copy(debugData, 0, bitmap.GetPixels(), debugData.Length);

                    //    // Salva la bitmap come PNG
                    //    using (var stream = File.OpenWrite("raw.png"))
                    //    {
                    //        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
                    //    }
                    //}
                    //using (var stream = File.OpenWrite("debug_bitmap_output.png"))
                    //{
                    //    skBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
                    //}
                }

                // Copia i dati dalla memoria del buffer a quella della bitmap.
                // Questo è il punto critico dove l'errore potrebbe verificarsi.
                // Assumiamo che il formato dei pixel sia compatibile.
                Buffer.MemoryCopy(src, dst, byteCount, byteCount);
            }
        }

        // Rilascia la texture mappata.
        GraphicsDevice.Unmap(_stagingTexture);

        //CommandList.Begin();

        //CommandList.CopyTexture(
        //    source: FullScreenTexture.ColorTexture,
        //    destination: _stagingTexture
        //    );

        //CommandList.End();
        //GraphicsDevice.SubmitCommands(CommandList);
        //GraphicsDevice.WaitForIdle();

        //var mapped = GraphicsDevice.Map(_stagingTexture, MapMode.Read);
        //unsafe
        //{
        //    void* src = mapped.Data.ToPointer();
        //    void* dst = skBitmap.GetPixels().ToPointer();

        //    Buffer.MemoryCopy(src, dst, skBitmap.ByteCount, skBitmap.ByteCount);
        //}
        //GraphicsDevice.Unmap(_stagingTexture);
    }

    public void ResizeTo(int width, int height)
    {
        OnResize(new Rectangle(0, 0, width, height));

        _skBitmap.Dispose();
        _skBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

    }

    public void Prepare()
    {


        Logger.Info("Hello World! Bliss start...");
        Logger.Info($"\t> CPU: {SystemInfo.Cpu}");
        Logger.Info($"\t> MEMORY: {SystemInfo.MemorySize} GB");
        Logger.Info($"\t> THREADS: {SystemInfo.Threads}");
        Logger.Info($"\t> OS: {SystemInfo.Os}");

        Logger.Info("Initialize window and graphics device...");
        //GraphicsDeviceOptions options = new GraphicsDeviceOptions()
        //{
        //    Debug = false,
        //    HasMainSwapchain = true,
        //    SwapchainDepthFormat = PixelFormat.D32FloatS8UInt,
        //    SyncToVerticalBlank = this.Settings.VSync,
        //    ResourceBindingModel = ResourceBindingModel.Improved,
        //    PreferDepthRangeZeroToOne = true,
        //    PreferStandardClipSpaceYDirection = true,
        //    SwapchainSrgbFormat = false
        //};

        var options = new GraphicsDeviceOptions()
        {
            Debug = false,
            SyncToVerticalBlank = this.Settings.VSync,
            SwapchainDepthFormat = null,
            HasMainSwapchain = false,
            PreferDepthRangeZeroToOne = true,
            ResourceBindingModel = ResourceBindingModel.Improved,
            PreferStandardClipSpaceYDirection = true,
        };


        //this.MainWindow = Window.CreateWindow(WindowType.Sdl3, this.Settings.Width, this.Settings.Height, this.Settings.Title, this.Settings.WindowFlags, options, this.Settings.Backend, out GraphicsDevice graphicsDevice);
        //this.MainWindow.Resized += () => this.OnResize(new Rectangle(this.MainWindow.GetX(), this.MainWindow.GetY(), this.MainWindow.GetWidth(), this.MainWindow.GetHeight()));

        var graphicsDevice = GraphicsDevice.CreateVulkan(options);
        this.GraphicsDevice = graphicsDevice;

        Logger.Info("Loading window icon...");
        //this.MainWindow.SetIcon(this.Settings.IconPath != string.Empty ? new Image(this.Settings.IconPath) : new Image("content/images/icon.png"));

        Logger.Info("Initialize time...");
        Time.Init();

        Logger.Info($"Set target FPS to: {this.Settings.TargetFps}");
        this.SetTargetFps(this.Settings.TargetFps);

        Logger.Info("Initialize command list...");
        this.CommandList = graphicsDevice.ResourceFactory.CreateCommandList();

        Logger.Info("Initialize global resources...");
        GlobalResource.Init(graphicsDevice);

        Logger.Info("Initialize input...");
        //if (this.MainWindow is Sdl3Window)
        //{
        //    Input.Init(new Sdl3InputContext(this.MainWindow));
        //}
        //else
        //{
        //    throw new Exception("This type of window is not supported by the InputContext!");
        //}
        Input.Init(new AvaloniaInputContext(_host));

        Logger.Info("Initialize audio device...");
        AudioContext.Initialize(44100, 2);

        this.Init();

    }


    public SKBitmap GetFrame()
    {
        lock (_frame)
        {
            return _skBitmap;
        }
    }
}