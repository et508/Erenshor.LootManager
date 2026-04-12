using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using ImGuiNET;
using UnityEngine;
using UnityEngine.Rendering;

namespace LootManager
{
    public sealed class ImGuiRenderer : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public Action OnLayout         { get; set; }
        public ImFontPtr BoldFont      { get; private set; }
        public bool WantCaptureMouse   { get; private set; }
        public bool WantTextInput      { get; private set; }

        public float UiScale
        {
            get => _uiScale;
            set => _uiScale = value;
        }
        public float CurrentScale => _uiScale;

        public void SetScale(float scale) { _pendingScale = scale; }
        public void ClearWindowState()    { ImGui.LoadIniSettingsFromMemory(""); }

        internal void ClearCaptureState()
        {
            WantCaptureMouse = false;
            WantTextInput    = false;
        }

        public ImGuiRenderer(ManualLogSource log)
        {
            _log = log;
        }

        public unsafe bool Init()
        {
            if (_context != IntPtr.Zero)
            {
                _log.LogWarning("[LootManager] ImGuiRenderer.Init() called twice — ignoring.");
                return true;
            }
            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                     ?? Paths.PluginPath;
                string nativePath  = Path.Combine(assemblyDir, "cimgui.dll");

                if (!File.Exists(nativePath))
                {
                    _log.LogError("[LootManager] cimgui.dll not found at: " + nativePath);
                    return false;
                }

                _nativeLib = LoadLibrary(nativePath);
                if (_nativeLib == IntPtr.Zero)
                {
                    _log.LogError(string.Format(
                        "[LootManager] LoadLibrary failed for {0} (Win32 error {1})",
                        nativePath, Marshal.GetLastWin32Error()));
                    return false;
                }

                _context = ImGui.CreateContext();
                ImGuiIOPtr io = ImGui.GetIO();

                io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
                io.NativePtr->IniFilename = (byte*)0;

                ImGui.StyleColorsDark();
                BuildFontAtlas();
                SaveStyleBackup();
                ImGui.GetStyle().ScaleAllSizes(_uiScale);
                CreateMaterial();

                _commandBuffer = new CommandBuffer { name = "LootManager_ImGui" };

                _log.LogInfo("[LootManager] ImGui initialised successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError("[LootManager] ImGui init failed: " + ex);
                return false;
            }
        }

        public unsafe void OnGUI()
        {
            Event current = Event.current;
            if (current == null) return;
            if (_context == IntPtr.Zero) return;

            ImGuiIOPtr io = ImGui.GetIO();

            if (current.type != EventType.Repaint) return;

            try
            {
                if (_pendingScale >= 0f)
                {
                    ApplyScale(_pendingScale);
                    _pendingScale = -1f;
                }

                io.DisplaySize = new System.Numerics.Vector2((float)Screen.width, (float)Screen.height);
                io.DeltaTime   = Time.deltaTime > 0f ? Time.deltaTime : 0.016666668f;

                UpdateInput(io);

                ImGui.NewFrame();
                OnLayout?.Invoke();
                ImGui.EndFrame();

                WantCaptureMouse = io.WantCaptureMouse;
                WantTextInput    = io.WantTextInput;

                ImGui.Render();
                RenderDrawData();
            }
            catch (Exception ex)
            {
                _log.LogError("[LootManager] ImGui render error: " + ex);
            }
        }

        public void Dispose()
        {
            if (_context != IntPtr.Zero)
            {
                ImGui.DestroyContext(_context);
                _context = IntPtr.Zero;
            }
            foreach (var mesh in _meshPool)
                UnityEngine.Object.Destroy(mesh);
            _meshPool.Clear();
            if (_fontTexture != null) { UnityEngine.Object.Destroy(_fontTexture); _fontTexture = null; }
            if (_material    != null) { UnityEngine.Object.Destroy(_material);    _material    = null; }
            _commandBuffer?.Dispose();
            if (_nativeLib != IntPtr.Zero)
            {
                FreeLibrary(_nativeLib);
                _nativeLib = IntPtr.Zero;
            }
        }

        private unsafe void BuildFontAtlas()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            var asm = Assembly.GetExecutingAssembly();

            using (var stream = asm.GetManifestResourceStream("Erenshor.LootManager.LootManager-Roboto.ttf"))
            {
                if (stream != null)
                {
                    byte[] fontBytes = new byte[stream.Length];
                    stream.Read(fontBytes, 0, fontBytes.Length);

                    IntPtr fontPtr = ImGui.MemAlloc((uint)fontBytes.Length);
                    Marshal.Copy(fontBytes, 0, fontPtr, fontBytes.Length);

                    var builder = new ImFontGlyphRangesBuilderPtr(
                        ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
                    builder.AddRanges(io.Fonts.GetGlyphRangesDefault());
                    ImVector ranges;
                    builder.BuildRanges(out ranges);

                    ImFontConfig* cfg = ImGuiNative.ImFontConfig_ImFontConfig();
                    cfg->OversampleH = 2;
                    cfg->OversampleV = 1;
                    io.Fonts.AddFontFromMemoryTTF(fontPtr, fontBytes.Length,
                        16f * _uiScale, cfg, ranges.Data);

                    builder.Destroy();
                }
                else
                {
                    _log.LogWarning("[LootManager] Roboto font not found in resources, using default.");
                    unsafe
                    {
                        ImFontConfig* cfg = ImGuiNative.ImFontConfig_ImFontConfig();
                        cfg->SizePixels  = 16f * _uiScale;
                        cfg->OversampleH = 1;
                        cfg->OversampleV = 1;
                        cfg->PixelSnapH  = 1;
                        io.Fonts.AddFontDefault(cfg);
                    }
                }
            }

            io.Fonts.Build();

            byte* pixels;
            int w, h, bpp;
            io.Fonts.GetTexDataAsRGBA32(out pixels, out w, out h, out bpp);

            _fontTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            byte[] texData = new byte[w * h * 4];
            Marshal.Copy((IntPtr)pixels, texData, 0, texData.Length);
            _fontTexture.LoadRawTextureData(texData);
            _fontTexture.Apply();
            io.Fonts.SetTexID(_fontTexture.GetNativeTexturePtr());
        }

        private unsafe void SaveStyleBackup()
        {
            int sz = sizeof(ImGuiStyle);
            _unscaledStyleBackup = new byte[sz];
            fixed (byte* dst = _unscaledStyleBackup)
                Buffer.MemoryCopy((void*)ImGui.GetStyle().NativePtr, dst, sz, sz);
        }

        private unsafe void RestoreStyleBackup()
        {
            if (_unscaledStyleBackup == null) return;
            fixed (byte* src = _unscaledStyleBackup)
                Buffer.MemoryCopy(src, (void*)ImGui.GetStyle().NativePtr,
                    _unscaledStyleBackup.Length, _unscaledStyleBackup.Length);
        }

        private void ApplyScale(float newScale)
        {
            _uiScale = newScale;
            if (_fontTexture != null) { UnityEngine.Object.Destroy(_fontTexture); _fontTexture = null; }
            ImGui.GetIO().Fonts.Clear();
            BuildFontAtlas();
            RestoreStyleBackup();
            ImGui.GetStyle().ScaleAllSizes(_uiScale);
            if (_material != null) _material.mainTexture = _fontTexture;
            _log.LogInfo(string.Format("[LootManager] UI scale -> {0:F2}", _uiScale));
        }

        private void CreateMaterial()
        {
            var shader = Shader.Find("UI/Default");
            _material = new Material(shader) { hideFlags = (HideFlags)61 };
            _material.SetInt("_SrcBlend", 5);
            _material.SetInt("_DstBlend", 10);
            _material.SetInt("_ZWrite",   0);
            _material.SetInt("_Cull",     0);
            _material.mainTexture = _fontTexture;
        }

        private void UpdateInput(ImGuiIOPtr io)
        {
            UnityEngine.Vector3 mouse = Input.mousePosition;
            io.AddMousePosEvent(mouse.x, (float)Screen.height - mouse.y);
            io.AddMouseButtonEvent(0, Input.GetMouseButton(0));
            io.AddMouseButtonEvent(1, Input.GetMouseButton(1));
            io.AddMouseButtonEvent(2, Input.GetMouseButton(2));

            UnityEngine.Vector2 scroll = Input.mouseScrollDelta;
            if (scroll.y != 0f || scroll.x != 0f)
                io.AddMouseWheelEvent(scroll.x, scroll.y);

            io.AddKeyEvent(ImGuiKey.ModCtrl,  Input.GetKey(KeyCode.LeftControl)  || Input.GetKey(KeyCode.RightControl));
            io.AddKeyEvent(ImGuiKey.ModShift, Input.GetKey(KeyCode.LeftShift)    || Input.GetKey(KeyCode.RightShift));
            io.AddKeyEvent(ImGuiKey.ModAlt,   Input.GetKey(KeyCode.LeftAlt)      || Input.GetKey(KeyCode.RightAlt));

            AddKey(io, ImGuiKey.Tab,         KeyCode.Tab);
            AddKey(io, ImGuiKey.LeftArrow,   KeyCode.LeftArrow);
            AddKey(io, ImGuiKey.RightArrow,  KeyCode.RightArrow);
            AddKey(io, ImGuiKey.UpArrow,     KeyCode.UpArrow);
            AddKey(io, ImGuiKey.DownArrow,   KeyCode.DownArrow);
            AddKey(io, ImGuiKey.PageUp,      KeyCode.PageUp);
            AddKey(io, ImGuiKey.PageDown,    KeyCode.PageDown);
            AddKey(io, ImGuiKey.Home,        KeyCode.Home);
            AddKey(io, ImGuiKey.End,         KeyCode.End);
            AddKey(io, ImGuiKey.Insert,      KeyCode.Insert);
            AddKey(io, ImGuiKey.Delete,      KeyCode.Delete);
            AddKey(io, ImGuiKey.Backspace,   KeyCode.Backspace);
            AddKey(io, ImGuiKey.Space,       KeyCode.Space);
            AddKey(io, ImGuiKey.Enter,       KeyCode.Return);
            AddKey(io, ImGuiKey.Escape,      KeyCode.Escape);
            AddKey(io, ImGuiKey.KeypadEnter, KeyCode.KeypadEnter);
            AddKey(io, ImGuiKey.A,           KeyCode.A);
            AddKey(io, ImGuiKey.C,           KeyCode.C);
            AddKey(io, ImGuiKey.V,           KeyCode.V);
            AddKey(io, ImGuiKey.X,           KeyCode.X);
            AddKey(io, ImGuiKey.Z,           KeyCode.Z);

            foreach (char c in Input.inputString)
                if (c >= ' ' && c != '\x7f')
                    io.AddInputCharacter((uint)c);
        }

        private static void AddKey(ImGuiIOPtr io, ImGuiKey imKey, KeyCode unityKey)
        {
            io.AddKeyEvent(imKey, Input.GetKey(unityKey));
        }

        private unsafe void RenderDrawData()
        {
            if (_commandBuffer == null || _material == null) return;

            ImDrawDataPtr drawData = ImGui.GetDrawData();
            if (drawData.CmdListsCount == 0) return;

            float sw = (float)Screen.width;
            float sh = (float)Screen.height;

            var proj = UnityEngine.Matrix4x4.Ortho(0f, sw, sh, 0f, -1f, 1f);
            _commandBuffer.Clear();
            _commandBuffer.SetProjectionMatrix(proj);
            _commandBuffer.SetViewMatrix(UnityEngine.Matrix4x4.identity);

            float ox = drawData.DisplayPos.X;
            float oy = drawData.DisplayPos.Y;

            while (_meshPool.Count < drawData.CmdListsCount)
            {
                var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.MarkDynamic();
                _meshPool.Add(m);
            }

            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr dl = drawData.CmdListsRange[i];
                var vtx          = dl.VtxBuffer;
                var idx          = dl.IdxBuffer;
                var cmd          = dl.CmdBuffer;

                _verts.Clear(); _uvs.Clear(); _colors.Clear();

                for (int j = 0; j < vtx.Size; j++)
                {
                    ImDrawVertPtr v = vtx[j];
                    _verts.Add(new UnityEngine.Vector3(v.pos.X - ox, v.pos.Y - oy, 0f));
                    _uvs.Add(new UnityEngine.Vector2(v.uv.X, v.uv.Y));
                    uint c = v.col;
                    _colors.Add(new Color32(
                        (byte)( c        & 0xFF),
                        (byte)((c >>  8) & 0xFF),
                        (byte)((c >> 16) & 0xFF),
                        (byte)((c >> 24) & 0xFF)));
                }

                Mesh mesh = _meshPool[i];
                mesh.Clear();
                mesh.SetVertices(_verts);
                mesh.SetUVs(0, _uvs);
                mesh.SetColors(_colors);
                mesh.subMeshCount = cmd.Size;

                for (int k = 0; k < cmd.Size; k++)
                {
                    ImDrawCmdPtr dc = cmd[k];
                    _indices.Clear();
                    for (int l = 0; l < (int)dc.ElemCount; l++)
                        _indices.Add((int)((uint)idx[(int)(dc.IdxOffset + (uint)l)] + dc.VtxOffset));
                    mesh.SetTriangles(_indices, k);
                }

                mesh.UploadMeshData(false);

                for (int k = 0; k < cmd.Size; k++)
                {
                    ImDrawCmdPtr dc = cmd[k];
                    if (dc.ElemCount == 0) continue;

                    float cx = dc.ClipRect.X - ox;
                    float cy = dc.ClipRect.Y - oy;
                    float cw = dc.ClipRect.Z - dc.ClipRect.X;
                    float ch = dc.ClipRect.W - dc.ClipRect.Y;
                    _commandBuffer.EnableScissorRect(new Rect(cx, sh - cy - ch, cw, ch));

                    _mpb.Clear();
                    Texture tex;
                    if (_textures.TryGetValue(dc.TextureId, out tex))
                    {
                        _mpb.SetTexture("_MainTex",   tex);
                        _mpb.SetVector("_MainTex_ST", new UnityEngine.Vector4(1f, -1f, 0f, 1f));
                    }
                    else
                    {
                        _mpb.SetTexture("_MainTex",   _fontTexture);
                        _mpb.SetVector("_MainTex_ST", new UnityEngine.Vector4(1f, 1f, 0f, 0f));
                    }
                    _commandBuffer.DrawMesh(mesh, UnityEngine.Matrix4x4.identity, _material, k, 0, _mpb);
                }

                _commandBuffer.DisableScissorRect();
            }

            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }

        private readonly ManualLogSource             _log;
        private IntPtr                               _nativeLib;
        private IntPtr                               _context;
        private Texture2D                            _fontTexture;
        private Material                             _material;
        private CommandBuffer                        _commandBuffer;
        private byte[]                               _unscaledStyleBackup;
        private float                                _uiScale      = 1f;
        private float                                _pendingScale = -1f;

        private readonly Dictionary<IntPtr, Texture> _textures  = new Dictionary<IntPtr, Texture>();
        private readonly List<Mesh>                  _meshPool  = new List<Mesh>();
        private readonly List<UnityEngine.Vector3>   _verts     = new List<UnityEngine.Vector3>();
        private readonly List<UnityEngine.Vector2>   _uvs       = new List<UnityEngine.Vector2>();
        private readonly List<Color32>               _colors    = new List<Color32>();
        private readonly List<int>                   _indices   = new List<int>();
        private readonly MaterialPropertyBlock       _mpb       = new MaterialPropertyBlock();
    }
}