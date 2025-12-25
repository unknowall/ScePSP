using ScePSP.Core;
using ScePSPPlatform.Library;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace ScePSPPlatform.GL.Impl.Windows
{
    public unsafe class WinGlContext : IGlContext
    {
        IntPtr _dc;

        IntPtr _context;

        IntPtr _hWnd;

        static IntPtr _sharedContext;

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern ushort RegisterClassEx(ref ExtendedWindowClass windowClass);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        [DllImport("user32.dll", EntryPoint = "AdjustWindowRectEx", CallingConvention = CallingConvention.StdCall,
             SetLastError = true), SuppressUnmanagedCodeSecurity]
        internal static extern bool AdjustWindowRectEx(ref Rect lpRect, WindowStyle dwStyle, bool bMenu,
            ExtendedWindowStyle dwExStyle);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateWindowEx(
            ExtendedWindowStyle exStyle,
            IntPtr classAtom,
            IntPtr windowName,
            WindowStyle style,
            int x, int y,
            int width, int height,
            IntPtr handleToParentWindow,
            IntPtr menu,
            IntPtr instance,
            IntPtr param
        );

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("Gdi32.dll")]
        internal static extern IntPtr GetCurrentObject(
            IntPtr hdc,
            uint uObjectType
        );

        [DllImport("Gdi32.dll")]
        internal static extern int GetObject(
            IntPtr hgdiobj,
            int cbBuffer,
            void* lpvObject
        );

        [SuppressUnmanagedCodeSecurity, DllImport("GDI32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int ChoosePixelFormat(IntPtr hDc, PixelFormatDescriptor* pPfd);

        [SuppressUnmanagedCodeSecurity, DllImport("GDI32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool SetPixelFormat(IntPtr hdc, int ipfd, PixelFormatDescriptor* ppfd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool AdjustWindowRectEx(ref Rect lpRect, int dwStyle, bool bMenu, int dwExStyle);

        const ClassStyle DefaultClassStyle = ClassStyle.OwnDc;

        private static bool _classRegistered;

        static readonly IntPtr Instance = Marshal.GetHINSTANCE(typeof(WinGlContext).Module);

        static readonly IntPtr ClassName = Marshal.StringToHGlobalAuto(Guid.NewGuid().ToString());

        const ExtendedWindowStyle ParentStyleEx = ExtendedWindowStyle.WindowEdge | ExtendedWindowStyle.ApplicationWindow;

        private static void RegisterClassOnce()
        {
            if (!_classRegistered)
            {
                ExtendedWindowClass wc = new ExtendedWindowClass();
                wc.Size = ExtendedWindowClass.SizeInBytes;
                wc.Style = DefaultClassStyle;
                wc.Instance = Instance;
                wc.WndProc = WindowProcedure;
                wc.ClassName = ClassName;
                wc.Icon = IntPtr.Zero;
                wc.IconSm = IntPtr.Zero;
                wc.Cursor = LoadCursor(IntPtr.Zero, (IntPtr)CursorName.Arrow);
                ushort atom = RegisterClassEx(ref wc);

                if (atom == 0)
                    throw new Exception($"Failed to register window class. Error: {Marshal.GetLastWin32Error()}");

                _classRegistered = true;
            }
        }

        static IntPtr WindowProcedure(IntPtr handle, WindowMessage message, IntPtr wParam, IntPtr lParam)
        {
            return DefWindowProc(handle, message, wParam, lParam);
        }

        public static WinGlContext FromWindowHandle(IntPtr windowHandle)
        {
            return new WinGlContext(windowHandle);
        }

        private WinGlContext(IntPtr winHandle)
        {
            _hWnd = winHandle;
            RegisterClassOnce();
            var width = 512;
            var height = 512;

            if (winHandle == IntPtr.Zero)
            {
                var style = WindowStyle.OverlappedWindow | WindowStyle.ClipChildren | WindowStyle.ClipSiblings;// | WindowStyle.Visible;
                var exStyle = ParentStyleEx;

                var rect = new Rect
                {
                    Left = 0,
                    Top = 0,
                    Right = width,
                    Bottom = height
                };
                AdjustWindowRectEx(ref rect, style, false, exStyle);

                var windowName = Marshal.StringToHGlobalAuto("Title");
                _hWnd = CreateWindowEx(
                    exStyle, ClassName, windowName, style,
                    rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top,
                    IntPtr.Zero, IntPtr.Zero, Instance, IntPtr.Zero
                );

                if (_hWnd == IntPtr.Zero)
                    throw new Exception($"Failed to create window. Error: {Marshal.GetLastWin32Error()}");

                //ShowWindow(_hWnd, 1); // 1=SW_SHOWNORMAL
                //UpdateWindow(_hWnd);
            }

            _dc = GetDC(_hWnd);
            if (_dc == 0)
            {
                Console.WriteLine($"Error GetDC HWND {_hWnd}");
            }

            var pfd = new PixelFormatDescriptor();
            pfd.Size = (short)sizeof(PixelFormatDescriptor);
            pfd.Version = 1;
            pfd.Flags = PixelFormatDescriptorFlags.DrawToWindow 
                | PixelFormatDescriptorFlags.SupportOpengl 
                | PixelFormatDescriptorFlags.Doublebuffer;
            pfd.LayerType = 0;
            pfd.PixelType = PixelType.RGBA; // PFD_TYPE_RGBA
            pfd.ColorBits = 32;
            pfd.DepthBits = 24;
            pfd.StencilBits = 8;

            var pf = ChoosePixelFormat(_dc, &pfd);

            if (!SetPixelFormat(_dc, pf, &pfd))
            {
                Console.WriteLine("Error SetPixelFormat failed.");
            }

            _context = Wgl.wglCreateContext(_dc);

            if (_sharedContext != IntPtr.Zero)
            {
            RetryShareLists:
                //Console.WriteLine("SharedContext!"); Console.ReadKey();
                if (!Wgl.wglShareLists(_sharedContext, _context))
                {
                    var lastError = Platform.InternalWindows.GetLastError();
                    Console.WriteLine($"Can't share lists {lastError}");
                    if (lastError == 170) // BUSY
                    {
                        Console.WriteLine($"-- Due to Busy. RETRY.");
                        Thread.Sleep(10);
                        goto RetryShareLists;
                    }
                    Debugger.Break();
                    throw new InvalidOperationException($"Can't share lists {lastError}");
                }
            }

            MakeCurrent();

            //Console.Out.WriteLineColored(ConsoleColor.Yellow, "OpenGL Version:{0}.{1}", GL.glGetInteger(GL.GL_MAJOR_VERSION), GL.glGetInteger(GL.GL_MINOR_VERSION));

            DynamicLibraryFactory.MapLibraryToType<Extension>(new DynamicLibraryGl());

            //GL.LoadAllOnce();

#if false
			if (Extension.wglCreateContextAttribsARB != null)
			{
				ReleaseCurrent();

				WGL.wglDeleteContext(this.Context);

				fixed (int* AttribListPtr = new int[] { (int)ArbCreateContext.MajorVersion, 3, (int)ArbCreateContext.MinorVersion, 1, 0, 0 })
				{
					this.Context = Extension.wglCreateContextAttribsARB(DC, SharedContext, AttribListPtr);
				}

				if (this.Context == IntPtr.Zero) throw(new Exception("Error creating context"));

				MakeCurrent();

				Console.WriteLine("OpenGL Version: {0}", Marshal.PtrToStringAnsi(new IntPtr(GL.glGetString(GL.GL_VERSION))));

				//Console.ReadKey();
			}
#endif

            if (_sharedContext == IntPtr.Zero)
            {
                _sharedContext = _context;
            }

            if (Extension.wglSwapIntervalEXT != null)
            {
                Extension.wglSwapIntervalEXT(0);
            }

            //RECT clientRect;
            //GetClientRect(hWnd, &clientRect);
        }

        public GlContextSize Size
        {
            get
            {
                var bitmapHeader = default(Bitmap);
                var hBitmap = GetCurrentObject(_dc, 7);
                GetObject(hBitmap, sizeof(Bitmap), &bitmapHeader);
                return new GlContextSize { Width = (int)bitmapHeader.BmWidth, Height = (int)bitmapHeader.BmHeight };
            }
        }

        public override string ToString() => $"WinOpenglContext({_dc}, {_context}, {_sharedContext}, {Size})";

        public enum ArbCreateContext
        {
            DebugBit = 0x0001,
            ForwardCompatibleBit = 0x0002,
            MajorVersion = 0x2091,
            MinorVersion = 0x2092,
            LayerPlane = 0x2093,
            Flags = 0x2094,
            ErrorInvalidVersion = 0x2095
        }


        public class Extension
        {
            public static wglCreateContextAttribsARB wglCreateContextAttribsARB;
            public static wglSwapIntervalEXT wglSwapIntervalEXT;
            public static wglGetSwapIntervalEXT wglGetSwapIntervalEXT;
        }

        public delegate IntPtr wglCreateContextAttribsARB(IntPtr hDc, IntPtr hShareContext, int* attribList);

        public delegate bool wglSwapIntervalEXT(int interval);

        public delegate int wglGetSwapIntervalEXT();

        public IGlContext MakeCurrent()
        {
            if (GlContextFactory.Current != this)
            {
                if (!Wgl.wglMakeCurrent(_dc, _context))
                {
                    Console.WriteLine($"Can't MakeCurrent DC {_dc} context {_context}");
                }
                GlContextFactory.Current = this;
            }
            //Console.WriteLine($"WinGlContext MakeCurrent DC {_dc} context {_context}");
            return this;
        }

        public IGlContext ReleaseCurrent()
        {
            if (GlContextFactory.Current != null)
            {
                if (!Wgl.wglMakeCurrent(_dc, IntPtr.Zero))
                {
                    Console.WriteLine($"Can't MakeCurrent DC {_dc} context {_context}");
                }
                GlContextFactory.Current = null;
            }
            //Console.WriteLine($"WinGlContext ReleaseCurrent DC {_dc} context {_context}");
            return this;
        }

        public IGlContext SwapBuffers()
        {
            Wgl.wglSwapBuffers(_dc);
            return this;
        }

        public void Dispose()
        {
            Wgl.wglDeleteContext(_context);
            _context = IntPtr.Zero;
        }
    }
}